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
                var registry = new GatewayRegistry(
                    _configuration,
                    _telemetryClient,
                    MESSAGE_TIMEOUT);
                var gateways = await registry.RegisterDevicesAsync(
                    _cancellationTokenSource.Token);

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
    }
}