using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Cosmos
{
    public class CosmosPinger : IDaemon
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CosmosPingerConfiguration _configuration = new CosmosPingerConfiguration();
        private readonly TelemetryClient _telemetryClient;

        public CosmosPinger(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        async Task IDaemon.RunAsync()
        {
            Console.WriteLine("Cosmos Pinger");

            try
            {
                Console.WriteLine("Looping for pings...");

                await Task.Delay(TimeSpan.FromDays(1), _cancellationTokenSource.Token);
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
    }
}