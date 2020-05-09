using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
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
            var registry = new GatewayRegistry(
                _configuration,
                _telemetryClient,
                MESSAGE_TIMEOUT,
                _cancellationTokenSource.Token);

            Console.WriteLine("Hub Feeder");
            Console.WriteLine($"Register {_configuration.TotalDeviceCount} devices...");

            try
            {
                await registry.StartHeartBeatAsync();

                var gateways = await registry.RegisterDevicesAsync();

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
                await registry.StopHeartBeatAsync();
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