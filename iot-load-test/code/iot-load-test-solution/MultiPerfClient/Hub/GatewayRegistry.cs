using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Hub
{
    internal class GatewayRegistry
    {
        private readonly HubFeederConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly TimeSpan _messageTimeout;
        private readonly CancellationToken _cancellationToken;
        private readonly string _leaserName = Guid.NewGuid().GetHashCode().ToString("x8");

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
        }

        public async Task<IImmutableList<Gateway>> RegisterDevicesAsync()
        {
            var ids = await FindDevicesAsync();
            var setting = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only)
            {
                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                {
                    Pooling = true
                },
                IdleTimeout = TimeSpan.FromMinutes(60)
            };
            var settings = new ITransportSettings[] { setting };
            var gateways =
                from g in Enumerable.Range(0, _configuration.GatewayCount)
                let gatewayName = $"gate.{_leaserName}.{g}"
                let devices = from d in Enumerable.Range(0, _configuration.DevicePerGateway)
                              let deviceId = ids[_configuration.DevicePerGateway * g + d]
                              let client = DeviceClient.CreateFromConnectionString(
                                  _configuration.IotConnectionString,
                                  deviceId,
                                  settings)
                              select new DeviceProxy(deviceId, client)
                select new Gateway(gatewayName, devices);

            return gateways.ToImmutableArray();
        }

        private async Task<IImmutableList<string>> FindDevicesAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                _configuration.IotConnectionString);
            var plannedDeviceIds = Enumerable.Range(0, _configuration.TotalDeviceCount)
                .Select(i => $"dev.{_leaserName}.{i}")
                .ToArray();
            //  Avoid throttling on registration by segmenting registration
            var tasks = from batch in TaskRunner.Segment(
                plannedDeviceIds,
                _configuration.RegistrationsPerSecond)
                        select RegisterBatchDeviceAsync(
                            registryManager,
                            batch,
                            _cancellationToken);
            var results = await TaskRunner.TempoRunAsync(
                tasks,
                TimeSpan.FromSeconds(1),
                _cancellationToken);
            var ids = results.Values.SelectMany(i => i).ToImmutableArray();

            return ids;
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
    }
}