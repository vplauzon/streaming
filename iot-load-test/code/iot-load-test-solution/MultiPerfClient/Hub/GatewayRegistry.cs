using Azure.Cosmos;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Hub
{
    internal class GatewayRegistry
    {
        #region Inner Types
        private class Leaser
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; } = "leaser";

            [JsonPropertyName("ttl")]
            public int Ttl { get; set; } = 60;
        }
        #endregion

        private readonly HubFeederConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly TimeSpan _messageTimeout;
        private readonly CancellationToken _cancellationToken;
        private readonly CosmosContainer _deviceContainer;
        private readonly string _leaserName = Guid.NewGuid().GetHashCode().ToString("x8");
        private Task? _heartBeatTask = null;

        public GatewayRegistry(
            HubFeederConfiguration configuration,
            TelemetryClient telemetryClient,
            TimeSpan messageTimeout,
            CancellationToken cancellationToken)
        {
            _configuration = configuration;
            _telemetryClient = telemetryClient;
            _messageTimeout = messageTimeout;
            _cancellationToken = cancellationToken;

            var cosmosClient = new CosmosClient(_configuration.CosmosConnectionString);

            _deviceContainer = cosmosClient.GetContainer("operationDb", "device");
        }

        public async Task StartHeartBeatAsync()
        {
            if (_heartBeatTask != null)
            {
                throw new InvalidOperationException("Heart beat already started");
            }
            await UpsertHeartBeatAsync();
            _heartBeatTask = PeriodicHeartBeatAsync();
        }

        public async Task StopHeartBeatAsync()
        {
            if (_heartBeatTask == null)
            {
                throw new InvalidOperationException("Heart beat hasn't started");
            }
            await _heartBeatTask;
            _heartBeatTask = null;
        }

        public async Task<Gateway[]> RegisterDevicesAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                _configuration.IotConnectionString);
            var gatewayIds =
                (from g in Enumerable.Range(0, _configuration.GatewayCount)
                 let gatewayName = $"{_leaserName}.{g}"
                 select new
                 {
                     GatewayName = gatewayName,
                     DeviceIds = (from d in Enumerable.Range(0, _configuration.DevicePerGateway)
                                  let deviceName = $"{gatewayName}.{d}"
                                  select deviceName).ToArray()
                 }).ToArray();
            var ids = gatewayIds.SelectMany(g => g.DeviceIds);
            //  Avoid throttling on registration
            var segments = TaskRunner.Segment(ids, _configuration.RegistrationsPerSecond);
            var tasks = from s in segments
                        select RegisterBatchDeviceAsync(registryManager, s, _cancellationToken);

            await TaskRunner.TempoRunAsync(
                tasks,
                _messageTimeout,
                _cancellationToken);

            var setting = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only)
            {
                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                {
                    Pooling = true
                },
                IdleTimeout = TimeSpan.FromMinutes(60)
            };
            var settings = new ITransportSettings[] { setting };
            var gateways = from g in gatewayIds
                           let devices = from d in g.DeviceIds
                                         let client = DeviceClient.CreateFromConnectionString(
                                             _configuration.IotConnectionString,
                                             d,
                                             settings)
                                         select new DeviceProxy(d, client)
                           select new Gateway(g.GatewayName, devices);

            return gateways.ToArray();
        }

        private async Task<string[]> RegisterBatchDeviceAsync(
            RegistryManager registryManager,
            string[] ids,
            CancellationToken cancellationToken)
        {
            var devices = from id in ids
                          select new Device(id)
                          {
                              Authentication = new AuthenticationMechanism
                              {
                                  SymmetricKey = new SymmetricKey()
                              }
                          };

            try
            {
                var result = await registryManager.AddDevices2Async(
                    devices,
                    cancellationToken);

                if (result.IsSuccessful)
                {
                    _telemetryClient.TrackMetric(
                        "registration-count",
                        ids.Length);

                    return ids;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{result.Errors.Length} errors detected while registering devices");
                }
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);

                throw;
            }
        }

        private async Task PeriodicHeartBeatAsync()
        {
            var period = TimeSpan.FromSeconds(30);

            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, _cancellationToken);
                await UpsertHeartBeatAsync();
            }
        }

        private async Task UpsertHeartBeatAsync()
        {
            var leaser = new Leaser { Id = _leaserName };

            await _deviceContainer.UpsertItemAsync(leaser);
        }
    }
}