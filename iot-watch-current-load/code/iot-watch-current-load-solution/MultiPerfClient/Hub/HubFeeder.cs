using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Diagnostics;
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
    internal class HubFeeder
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HubFeederConfiguration _configuration = new HubFeederConfiguration();
        private readonly Random _random = new Random();

        public async Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");
            Console.WriteLine($"Register {_configuration.DeviceCount} devices...");

            var devices = await RegisterDevicesAsync();
            var clients = (from d in devices
                           select DeviceClient.CreateFromConnectionString(
                               _configuration.ConnectionString,
                               d.Id)).ToArray();
            var betweenMessages = TimeSpan.FromMinutes(60) / _configuration.BatchesPerHour;

            Console.WriteLine("Looping for messages...");
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var watch = new Stopwatch();

                watch.Start();
                await SendingMessagesAsync(clients);

                var pause = betweenMessages - watch.Elapsed;

                if (pause > TimeSpan.Zero)
                {
                    Console.WriteLine($"Pausing between 2 batches:  {pause}...");

                    await Task.Delay(pause);
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task SendingMessagesAsync(DeviceClient[] clients)
        {
            Console.WriteLine("Sending message batch...");
            foreach (var client in clients)
            {
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    var payload = from i in Enumerable.Range(0, _configuration.MessageSize * 1024)
                                  select (char)(_random.Next((int)'A', (int)'Z'));

                    writer.Write("{'payload':'");
                    writer.Write(payload.ToArray());
                    writer.Write("'}");

                    writer.Flush();
                    stream.Position = 0;

                    var message = new Microsoft.Azure.Devices.Client.Message(writer.BaseStream);

                    await client.SendEventAsync(message);
                }
            }
        }

        private async Task<Device[]> RegisterDevicesAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                _configuration.ConnectionString);
            var tasks = (from i in Enumerable.Range(0, _configuration.DeviceCount)
                         select RegisterDeviceAsync(registryManager, i)).ToArray();

            await Task.WhenAll(tasks);

            var devices = from t in tasks
                          select t.Result;

            return devices.ToArray();
        }

        private async Task<Device> RegisterDeviceAsync(RegistryManager registryManager, int index)
        {
            var device = new Device(Environment.MachineName + "." + index)
            {
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = new SymmetricKey()
                }
            };

            device = await registryManager.AddDeviceAsync(device, _cancellationTokenSource.Token);

            return device;
        }
    }
}