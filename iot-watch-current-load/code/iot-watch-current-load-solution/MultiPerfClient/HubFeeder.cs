using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    /// <summary>
    /// Largely inspired by
    /// https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet.
    /// </summary>
    internal class HubFeeder
    {
        HubFeederConfiguration Configuration { get; } = new HubFeederConfiguration();

        public async Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");
            Console.WriteLine($"Register {Configuration.DeviceCount} devices...");

            await RegisterDevicesAsync();
            //DeviceClient.CreateFromConnectionString()
        }

        private async Task RegisterDevicesAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                Configuration.ConnectionString);
            var tasks = (from i in Enumerable.Range(0, Configuration.DeviceCount)
                         select RegisterDeviceAsync(registryManager, i)).ToArray();

            await Task.WhenAll(tasks);
        }

        private async static Task RegisterDeviceAsync(RegistryManager registryManager, int index)
        {
            var device = new Device(Environment.MachineName + "." + index)
            {
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = new SymmetricKey()
                }
            };
            var result = await registryManager.AddDeviceAsync(device);
        }
    }
}