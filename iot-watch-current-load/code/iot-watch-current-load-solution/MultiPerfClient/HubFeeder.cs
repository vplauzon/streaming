using System;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    /// <summary>
    /// Largely inspired by
    /// https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet.
    /// </summary>
    internal class HubFeeder
    {
        public static Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");

            return Task.CompletedTask;
        }
    }
}