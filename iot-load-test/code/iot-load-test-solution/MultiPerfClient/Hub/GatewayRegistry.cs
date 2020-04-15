using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
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

        public GatewayRegistry(
            HubFeederConfiguration configuration,
            TelemetryClient telemetryClient,
            TimeSpan messageTimeout)
        {
            _configuration = configuration;
            _telemetryClient = telemetryClient;
            _messageTimeout = messageTimeout;
        }

        public async Task<Gateway[]> RegisterDevicesAsync(CancellationToken cancellationToken)
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                _configuration.ConnectionString);
            var uniqueCode = Guid.NewGuid().GetHashCode().ToString("x8");
            var gatewayIds =
                (from g in Enumerable.Range(0, _configuration.GatewayCount)
                 let gatewayName = $"{uniqueCode}.{g}"
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
                        select RegisterBatchDeviceAsync(registryManager, s, cancellationToken);

            await TaskRunner.TempoRunAsync(
                tasks,
                _messageTimeout,
                cancellationToken);

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
                                             _configuration.ConnectionString,
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
    }
}