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
        #region Inner Types
        private class MessageLoopContext
        {
            private readonly HubFeederConfiguration _configuration = new HubFeederConfiguration();
            private readonly IDictionary<string, string> _telemetryContext;
            private readonly Random _random = new Random(42);
            private volatile int _clientIndex;
            private volatile int _messageCount = 0;
            private volatile int _errorCount = 0;

            public MessageLoopContext(HubFeederConfiguration configuration)
            {
                _telemetryContext = new Dictionary<string, string>()
                {
                    { "deviceCount", _configuration.DeviceCount.ToString() },
                    { "messagePerSecond", _configuration.ConcurrentMessagesCount.ToString() },
                    { "messageSize", _configuration.MessageSize.ToString() }
                };
                //  Start at the end so we can really start at zero (not one)
                _clientIndex = _configuration.DeviceCount;
            }

            public async Task LoopMessagesAsync(
                DeviceClient[] allClients,
                TelemetryClient telemetryClient,
                CancellationToken cancellationToken)
            {
                var messageTasks = from i in Enumerable.Range(0, _configuration.ConcurrentMessagesCount)
                                   select PushMessagesAsync(
                                       allClients,
                                       telemetryClient,
                                       cancellationToken);
                var telemetryTask = SendTelemetryAsync(telemetryClient, cancellationToken);

                await Task.WhenAll(messageTasks.Prepend(telemetryTask));
            }

            private async Task PushMessagesAsync(
                DeviceClient[] allClients,
                TelemetryClient telemetryClient,
                CancellationToken cancellationToken)
            {
                var payload = CreateMessagePayload();

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var message = new Microsoft.Azure.Devices.Client.Message(payload))
                    {
                        var client = NextClient(allClients);
                        var timeoutSource = new CancellationTokenSource(MESSAGE_TIMEOUT);
                        var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken,
                            timeoutSource.Token);

                        message.ContentEncoding = "utf-8";
                        message.ContentType = "application/json";
                        try
                        {
                            var sendTask = client.SendEventAsync(message, combinedSource.Token);

                            //  Prepare next payload in parallel
                            payload = CreateMessagePayload();
                            await sendTask;
                            Interlocked.Increment(ref _messageCount);
                        }
                        catch (Exception ex)
                        {
                            telemetryClient.TrackException(ex);
                            Interlocked.Increment(ref _errorCount);
                        }
                    }
                }
            }

            private async Task SendTelemetryAsync(
                TelemetryClient telemetryClient,
                CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TELEMETRY_INTERVAL, cancellationToken);

                    var messageCount = Interlocked.Exchange(ref _messageCount, 0);
                    var errorCount = Interlocked.Exchange(ref _errorCount, 0);

                    telemetryClient.TrackMetric(
                        "message-count",
                        messageCount,
                        _telemetryContext);
                    telemetryClient.TrackMetric(
                        "error-count",
                        errorCount,
                        _telemetryContext);
                }
            }

            private byte[] CreateMessagePayload()
            {
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    var payload = from i in Enumerable.Range(0, _configuration.MessageSize)
                                  select (char)(_random.Next((int)'A', (int)'Z'));

                    writer.Write("{'payload':'");
                    writer.Write(payload.ToArray());
                    writer.Write("', 'recordedAt': '");
                    writer.Write(DateTime.Now.ToUniversalTime());
                    writer.Write("'}");

                    writer.Flush();

                    return stream.ToArray();
                }
            }

            private DeviceClient NextClient(DeviceClient[] allClients)
            {
                var nextIndex = Interlocked.Increment(ref _clientIndex) % _configuration.DeviceCount;

                return allClients[nextIndex];
            }
        }
        #endregion

        private static readonly TimeSpan TELEMETRY_INTERVAL = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan MESSAGE_TIMEOUT = TimeSpan.FromSeconds(5);

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly HubFeederConfiguration _configuration = new HubFeederConfiguration();
        private readonly TelemetryClient _telemetryClient;

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
            var context = new MessageLoopContext(_configuration);

            await context.LoopMessagesAsync(
                allClients,
                _telemetryClient,
                _cancellationTokenSource.Token);
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
                MESSAGE_TIMEOUT,
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
                    _telemetryClient.TrackMetric(
                        "registration-count",
                        ids.Length);

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