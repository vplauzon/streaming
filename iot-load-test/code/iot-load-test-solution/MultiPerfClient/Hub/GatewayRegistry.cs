using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Hub
{
    internal class GatewayRegistry
    {
        #region Inner Types
        private class LeaserDocument
        {
            public string? Id { get; set; }

            public string Type { get; set; } = "leaser";

            public int Ttl { get; set; } = 60;
        }

        private class DeviceDocument
        {
            public string? Id { get; set; }

            public string Type { get; set; } = "device";

            public string? Leaser { get; set; }
        }
        #endregion

        private readonly HubFeederConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly TimeSpan _messageTimeout;
        private readonly CancellationToken _cancellationToken;
        private readonly Container _deviceContainer;
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

            var cosmosClient = new CosmosClient(
                _configuration.CosmosConnectionString,
                new CosmosClientOptions
                {
                    AllowBulkExecution = true,
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                });

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
            var plannedSegmentQueue = ImmutableQueue.CreateRange(TaskRunner.Segment(
                plannedDeviceIds,
                _configuration.RegistrationsPerSecond));
            var ids = new List<string>(2 * _configuration.TotalDeviceCount);

            //  Compete between registering devices and recovering them from Cosmos DB
            while (ids.Count < _configuration.TotalDeviceCount)
            {
                var startTime = DateTime.Now;
                var registerTask = RegisterBatchDeviceAsync(
                    registryManager,
                    plannedSegmentQueue.Peek(),
                    _cancellationToken);
                var recoveredDevices = await RecoverDevicesAsync(
                    _configuration.TotalDeviceCount - ids.Count);
                var registeredDevices = await registerTask;

                await WriteDevicesAsync(registeredDevices);
                ids.AddRange(registeredDevices);
                ids.AddRange(recoveredDevices);

                //  Avoid trottling by waiting for the trottling time (1s) to pass
                var delay = TimeSpan.FromSeconds(1) - (DateTime.Now - startTime);

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }
                plannedSegmentQueue = plannedSegmentQueue.Dequeue();
            }
            //  If competition yield too many devices, release some
            if (ids.Count > _configuration.TotalDeviceCount)
            {
                await WriteOffDevicesAsync(ids.Skip(_configuration.TotalDeviceCount));
            }

            return ids.Take(_configuration.TotalDeviceCount).ToImmutableArray();
        }

        private async Task WriteDevicesAsync(IEnumerable<string> deviceIds)
        {
            var deviceDocuments = from id in deviceIds
                                  select new DeviceDocument
                                  {
                                      Id = id,
                                      Leaser = _leaserName
                                  };
            var writeTasks = from doc in deviceDocuments
                             select _deviceContainer.CreateItemAsync(doc);

            await Task.WhenAll(writeTasks);
        }

        private async Task<IImmutableList<string>> RecoverDevicesAsync(int deviceCount)
        {
            //  Get the list of leasers
            var leasers = await _deviceContainer.GetItemQueryIterator<LeaserDocument>(
                "SELECT * FROM c WHERE c.type='leaser'").ToListAsync();
            //  Check for devices with a leaser no longer in the list
            var leaserList = string.Join(", ", leasers.Select(l => $"'{l.Id}'"));
            var deviceQuery =
                $"SELECT TOP {deviceCount} * FROM c "
                + $"WHERE c.type='device' AND c.leaser NOT IN ({leaserList})";
            var devices = await _deviceContainer.GetItemQueryIterator<DeviceDocument>(
                deviceQuery).ToListAsync();
            //  Enlist current leaser as the devices leaser
            var leasingDeviceTasks = from d in devices
                                     let doc = new DeviceDocument
                                     {
                                         Id = d.Id,
                                         Leaser = _leaserName
                                     }
                                     select _deviceContainer.UpsertItemAsync(doc);
            //  Return device ids
            var deviceIds = from d in devices
                            select d.Id;

            //  Wait for leasing to be done
            await Task.WhenAll(leasingDeviceTasks);

            return deviceIds.ToImmutableArray();
        }

        private async Task WriteOffDevicesAsync(IEnumerable<string> deviceIds)
        {
            var writeOffTasks = from id in deviceIds
                                let doc = new DeviceDocument
                                {
                                    Id = id,
                                    Leaser = "*"
                                }
                                select _deviceContainer.UpsertItemAsync(doc);

            await Task.WhenAll(writeOffTasks);
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
            var leaser = new LeaserDocument { Id = _leaserName };

            await _deviceContainer.UpsertItemAsync(leaser);
        }
    }
}