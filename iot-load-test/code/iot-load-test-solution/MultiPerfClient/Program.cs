using AppInsights.TelemetryInitializers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
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
                    var task = feeder.RunAsync();

                    AppDomain.CurrentDomain.ProcessExit += async (object? sender, EventArgs e) =>
                    {
                        feeder.Stop();

                        await task;
                    };

                    await task;

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