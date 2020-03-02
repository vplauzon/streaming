using Microsoft.Azure.Devices;
using System;
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
        public static async Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");

            var connectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            var deviceCountText = Environment.GetEnvironmentVariable("DEVICE_COUNT");
            int deviceCount;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("Env Var 'IOT_CONN_STRING' not set", "IOT_CONN_STRING");
            }
            else if (string.IsNullOrWhiteSpace(deviceCountText))
            {
                throw new ArgumentNullException("Env Var 'DEVICE_COUNT' not set", "DEVICE_COUNT");
            }
            else if (!int.TryParse(deviceCountText, out deviceCount))
            {
                throw new ArgumentException("Env Var 'DEVICE_COUNT' isn't an integer", "DEVICE_COUNT");
            }
            else
            {
                Console.WriteLine($"Register {deviceCount} devices...");

                await RegisterDevicesAsync(connectionString, deviceCount);
            }
        }

        private async static Task RegisterDevicesAsync(string connectionString, int deviceCount)
        {
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            var tasks = (from i in Enumerable.Range(0, deviceCount)
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

            await registryManager.AddDeviceAsync(device);
        }
    }
}