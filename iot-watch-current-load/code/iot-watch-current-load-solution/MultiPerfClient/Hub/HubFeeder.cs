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
                var setting = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        Pooling = true
                    }
                };
                var settings = new ITransportSettings[] { setting };
                var clients = (from id in deviceIds
                               select DeviceClient.CreateFromConnectionString(
                                   _configuration.ConnectionString,
                                   id,
                                   settings)).ToArray();

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

        private async Task LoopMessagesAsync(DeviceClient[] allClients)
        {
            var context = new Dictionary<string, string>()
            {
                { "deviceCount", _configuration.DeviceCount.ToString() },
                { "messagePerSecond", _configuration.MessagesPerSecond.ToString() },
                { "messageSize", _configuration.MessageSize.ToString() }
            };

            while (!_cancellationTokenSource.IsCancellationRequested)
            {   //  Represent a loop in all clients
                var loopWatch = new Stopwatch();
                var clientGroups =
                    TaskRunner.Segment(allClients, _configuration.MessagesPerSecond);
                var clientGroupTasks = from c in clientGroups
                                       let tasks = from client in c
                                                       //  Send a message to each client
                                                   select SendMessageToOneClientAsync(client)
                                       select Task.WhenAll(tasks);

                Console.WriteLine($"Sending 1 message to each {allClients.Length} devices...");
                loopWatch.Start();

                var result = await TaskRunner.TempoRunAsync(
                    clientGroupTasks,
                    TimeSpan.FromSeconds(1),
                    _cancellationTokenSource.Token);
                var metricMessageCount = result.Values.SelectMany(i => i).Sum();

                Console.WriteLine("Writing metrics");
                WriteMetrics(
                    allClients,
                    context,
                    metricMessageCount,
                    result.PauseCount,
                    result.IterationCount,
                    loopWatch.Elapsed.TotalSeconds);
            }
        }

        private void WriteMetrics(
            DeviceClient[] allClients,
            Dictionary<string, string> context,
            int metricMessageCount,
            int pauseCount,
            int iterations,
            double duration)
        {
            _telemetryClient.TrackMetric(
                "message-count",
                metricMessageCount,
                context);
            _telemetryClient.TrackMetric(
                "message-throughput-per-second",
                metricMessageCount / duration,
                context);
            _telemetryClient.TrackMetric(
                "pause-ratio",
                (double)pauseCount / iterations,
                context);
            _telemetryClient.TrackMetric(
                "sent-ratio",
                (double)metricMessageCount / allClients.Length,
                context);
            _telemetryClient.Flush();
        }

        private async Task<int> SendMessageToOneClientAsync(DeviceClient client)
        {
            var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token,
                timeoutSource.Token);

            using (var message = new Microsoft.Azure.Devices.Client.Message(
                CreateMessagePayload()))
            {
                try
                {
                    await client.SendEventAsync(message, combinedSource.Token);

                    return 1;
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackException(ex);

                    return 0;
                }
            }
        }

        private byte[] CreateMessagePayload()
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
            //  Avoid throttling on registration
            var segments = TaskRunner.Segment(ids, _configuration.RegistrationsPerSecond);
            var tasks = from s in segments
                        select RegisterBatchDeviceAsync(registryManager, s);

            await TaskRunner.TempoRunAsync(
                tasks,
                TimeSpan.FromSeconds(1),
                _cancellationTokenSource.Token);

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