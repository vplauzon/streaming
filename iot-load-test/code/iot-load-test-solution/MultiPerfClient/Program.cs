using AppInsights.TelemetryInitializers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using MultiPerfClient.Cosmos;
using MultiPerfClient.Hub;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mode = Environment.GetEnvironmentVariable("MODE");

            Console.WriteLine("MultiPerfClient");

            switch (mode?.Trim())
            {
                case "hub-feeder":
                    var feeder = new HubFeeder(InitAppInsights("HUB-FEEDER"));
                    
                    await RunDaemonAsync(feeder);

                    return;

                case "cosmos-client":
                    var cosmosPinger = new CosmosPinger(InitAppInsights("HUB-FEEDER"));

                    await RunDaemonAsync(cosmosPinger);

                    return;

                default:
                    Console.WriteLine("Environment variable 'MODE' must be set to one of the following values:");
                    Console.WriteLine("* 'hub-feeder', for feeding an IoT hub");
                    Console.WriteLine();
                    if (!string.IsNullOrWhiteSpace(mode))
                    {
                        Console.WriteLine($"Mode '{mode}' isn't supported");
                    }

                    return;
            }
        }

        private static async Task RunDaemonAsync(IDaemon daemon)
        {
            var task = daemon.RunAsync();

            AppDomain.CurrentDomain.ProcessExit += async (object? sender, EventArgs e) =>
            {
                daemon.Stop();

                await task;
            };

            await task;
        }

        private static TelemetryClient InitAppInsights(string roleName)
        {
            var appInsightsKey = Environment.GetEnvironmentVariable("APP_INSIGHTS_KEY");

            if (string.IsNullOrWhiteSpace(appInsightsKey))
            {
                throw new ArgumentNullException("Environment variable missing", "APP_INSIGHTS_KEY");
            }

            //  Create configuration
            var configuration = TelemetryConfiguration.CreateDefault();

            //  Set Instrumentation Keys
            configuration.InstrumentationKey = appInsightsKey;
            //  Customize App Insights role name
            configuration.TelemetryInitializers.Add(
                new RoleNameInitializer(roleName));

            return new TelemetryClient(configuration);
        }
    }
}