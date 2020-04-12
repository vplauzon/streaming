using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Hub
{
    /// <summary>
    /// Largely inspired by
    /// https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet.
    /// </summary>
    internal class HubFeeder : IDaemon
    {
        private static readonly TimeSpan MESSAGE_TIMEOUT = TimeSpan.FromSeconds(5);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HubFeederConfiguration _configuration = new HubFeederConfiguration();
        private readonly TelemetryClient _telemetryClient;

        public HubFeeder(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        async Task IDaemon.RunAsync()
        {
            Console.WriteLine("Hub Feeder");
            Console.WriteLine($"Register {_configuration.TotalDeviceCount} devices...");

            try
            {
                var gateways = await RegisterDevicesAsync();

                Console.WriteLine("Looping for messages...");
                await LoopMessagesAsync(gateways);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                throw;
            }
            finally
            {
                _telemetryClient.Flush();
            }
        }

        void IDaemon.Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task LoopMessagesAsync(IEnumerable<Gateway> gateways)
        {
            var context = new MessageLoopContext(_configuration, MESSAGE_TIMEOUT);

            await context.LoopMessagesAsync(
                gateways,
                _telemetryClient,
                _cancellationTokenSource.Token);
        }

        private async Task<Gateway[]> RegisterDevicesAsync()
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
                        select RegisterBatchDeviceAsync(registryManager, s);

            await TaskRunner.TempoRunAsync(
                tasks,
                MESSAGE_TIMEOUT,
                _cancellationTokenSource.Token);

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
            string[] ids)
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
                    _cancellationTokenSource.Token);

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