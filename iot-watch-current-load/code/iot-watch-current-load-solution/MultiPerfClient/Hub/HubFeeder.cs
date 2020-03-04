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
        private const int CONCURRENT_CALLS = 100;
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
                var deviceIds = await RegisterDevicesAsync();

                Console.WriteLine("Looping for messages...");

                await LoopMessagesAsync(deviceIds);
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

        private async Task LoopMessagesAsync(string[] deviceIds)
        {
            var client = DeviceClient.CreateFromConnectionString(_configuration.ConnectionString);
            var watch = new Stopwatch();
            var metricMessageCount = 0;
            var context = new Dictionary<string, string>()
            {
                { "batchesPerHour", _configuration.MessagesPerMinute.ToString() },
                { "deviceCount", _configuration.DeviceCount.ToString() },
                { "messageSize", _configuration.MessageSize.ToString() }
            };

            watch.Start();
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var messageCount = await SendingMessagesAsync(client, deviceIds);

                metricMessageCount += messageCount;
                if (metricMessageCount >= _configuration.MessagesPerMinute)
                {
                    var pause = TimeSpan.FromSeconds(60 - DateTime.Now.Second);

                    if (pause > TimeSpan.Zero && pause < TimeSpan.FromSeconds(59))
                    {
                        Console.WriteLine($"Pausing before next minute:  {pause}...");
                        await Task.Delay(pause);
                    }
                    Console.WriteLine("Writing metrics");
                    _telemetryClient.TrackMetric(
                        "pause-length-in-seconds",
                        pause.TotalSeconds,
                        context);
                    _telemetryClient.TrackMetric(
                        "message-throughput-per-minute",
                        metricMessageCount / watch.Elapsed.TotalSeconds * 60,
                        context);
                    //  Reset metrics
                    watch.Restart();
                    metricMessageCount = 0;
                }
            }
        }

        private async Task<int> SendingMessagesAsync(DeviceClient client, string[] deviceIds)
        {
            Console.WriteLine($"Sending 1 message to each {deviceIds.Length} devices...");

            var tasks = from id in deviceIds
                        select SendMessageToOneClientAsync(client, id);
            //  Avoid having timeout for great quantity of devices
            var results = await TaskRunner.RunAsync(tasks, CONCURRENT_CALLS);
            var messageCount = results.Sum();

            return messageCount;
        }

        private async Task<int> SendMessageToOneClientAsync(DeviceClient client, string id)
        {
            using (var message = new Microsoft.Azure.Devices.Client.Message(
                CreateMessagePayload(id)))
            {
                try
                {
                    await client.SendEventAsync(message);

                    return 1;
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackException(ex);

                    return 0;
                }
            }
        }

        private byte[] CreateMessagePayload(string id)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                var payload = from i in Enumerable.Range(0, _configuration.MessageSize * 1024)
                              select (char)(_random.Next((int)'A', (int)'Z'));

                writer.Write($"{{'deviceId':'{id}','payload':'");
                writer.Write(payload.ToArray());
                writer.Write("'}");

                writer.Flush();

                return stream.ToArray();
            }
        }

        private async Task<string[]> RegisterDevicesAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(
                _configuration.ConnectionString);
            var uniqueCode = Guid.NewGuid().GetHashCode().ToString("x8");
            var ids = (from i in Enumerable.Range(0, _configuration.DeviceCount)
                       select $"{Environment.MachineName}.{uniqueCode}.{i}").ToArray();
            var idSegments = TaskRunner.Segment(ids, 100);
            //  Register devices in batches
            var tasks = from segment in idSegments
                        select RegisterBatchDeviceAsync(registryManager, segment);

            //  Avoid having timeout for great quantity of devices
            await TaskRunner.RunAsync(tasks, CONCURRENT_CALLS);

            return ids;
        }

        private async Task<string[]> RegisterBatchDeviceAsync(
            RegistryManager registryManager,
            string[] ids)
        {
            var devices = from id in ids
                          select new Device(id)
                          {
                              Authentication = new AuthenticationMechanism
                              {
                                  SymmetricKey = new SymmetricKey()
                              }
                          };

            try
            {
                var result = await registryManager.AddDevices2Async(
                    devices,
                    _cancellationTokenSource.Token);

                if (result.IsSuccessful)
                {
                    return ids;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{result.Errors.Length} errors detected while registering devices");
                }
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);

                throw;
            }
        }
    }
}