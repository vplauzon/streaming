using MultiPerfClient.Hub;
using System;
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
                    var feeder = new HubFeeder();
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
    }
}