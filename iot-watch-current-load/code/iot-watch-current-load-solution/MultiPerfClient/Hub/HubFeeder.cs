using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
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
        private static readonly TimeSpan METRIC_WINDOW = TimeSpan.FromSeconds(60);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HubFeederConfiguration _configuration = new HubFeederConfiguration();
        private readonly TelemetryClient _telemetryClient;
        private readonly Random _random = new Random();

        public HubFeeder(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");
            Console.WriteLine($"Register {_configuration.DeviceCount} devices...");

            try
            {
                var devices = await RegisterDevicesAsync();
                var clients = (from d in devices
                               select DeviceClient.CreateFromConnectionString(
                                   _configuration.ConnectionString,
                                   d.Id)).ToArray();

                Console.WriteLine("Looping for messages...");

                await LoopMessagesAsync(clients);
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

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task LoopMessagesAsync(DeviceClient[] clients)
        {
            var betweenMessages = TimeSpan.FromMinutes(60) / _configuration.BatchesPerHour;
            var metricWatch = new Stopwatch();
            var messageCount = 0;

            metricWatch.Start();
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var delayWatch = new Stopwatch();

                delayWatch.Start();
                messageCount += await SendingMessagesAsync(clients);

                var pause = betweenMessages - delayWatch.Elapsed;

                if (pause > TimeSpan.Zero)
                {
                    Console.WriteLine($"Pausing between 2 batches:  {pause}...");

                    await Task.Delay(pause);
                }
                if (metricWatch.Elapsed >= METRIC_WINDOW)
                {
                    Console.WriteLine("Writing metrics");
                    _telemetryClient.TrackMetric(
                        "message-throughput-per-minute",
                        messageCount / metricWatch.Elapsed.TotalSeconds * 60,
                        new Dictionary<string, string>()
                        {
                            { "batchesPerHour", _configuration.BatchesPerHour.ToString() },
                            { "deviceCount", _configuration.DeviceCount.ToString() },
                            { "messageSize", _configuration.MessageSize.ToString() }
                        });
                    //  Reset metrics
                    metricWatch.Restart();
                    messageCount = 0;
                }
            }
        }

        private async Task<int> SendingMessagesAsync(DeviceClient[] clients)
        {
            var messageCount = 0;

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

                    try
                    {
                        await client.SendEventAsync(message);
                        ++messageCount;
                    }
                    catch (Exception ex)
                    {
                        _telemetryClient.TrackException(ex);
                    }
                }
            }

            return messageCount;
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