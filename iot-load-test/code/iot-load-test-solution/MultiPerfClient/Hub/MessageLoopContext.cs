using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MultiPerfClient.Hub
{
    internal class MessageLoopContext
    {
        private static readonly DateTime TIMESTAMP_ORIGIN = new DateTime(1970,1,1);
        private static readonly TimeSpan TELEMETRY_INTERVAL = TimeSpan.FromSeconds(20);

        private readonly HubFeederConfiguration _configuration;
        private readonly TimeSpan _messageTimeout;
        private readonly IDictionary<string, string> _telemetryContext;
        private volatile int _clientIndex;
        private volatile int _messageCount = 0;
        private volatile int _errorCount = 0;

        public MessageLoopContext(
            HubFeederConfiguration configuration,
            TimeSpan messageTimeout)
        {
            _configuration = configuration;
            _messageTimeout = messageTimeout;
            _telemetryContext = new Dictionary<string, string>()
            {
                { "gatewayCount", _configuration.GatewayCount.ToString() },
                { "devicePerGateway", _configuration.DevicePerGateway.ToString() },
                { "messagePerSecond", _configuration.ConcurrentMessagesCount.ToString() },
                { "messageSize", _configuration.MessageSize.ToString() }
            };
            //  Start at the end so we can really start at zero (not one)
            _clientIndex = _configuration.TotalDeviceCount;
        }

        public async Task LoopMessagesAsync(
            IEnumerable<Gateway> gateways,
            TelemetryClient telemetryClient,
            CancellationToken cancellationToken)
        {
            var readonlyGateways = gateways.ToImmutableArray();
            var messageTasks = from i in Enumerable.Range(0, _configuration.ConcurrentMessagesCount)
                               select PushMessagesAsync(
                                   readonlyGateways,
                                   telemetryClient,
                                   cancellationToken);
            var telemetryTask = SendTelemetryAsync(telemetryClient, cancellationToken);

            await Task.WhenAll(messageTasks.Prepend(telemetryTask));
        }

        private async Task PushMessagesAsync(
            IImmutableList<Gateway> gateways,
            TelemetryClient telemetryClient,
            CancellationToken cancellationToken)
        {
            var devices = (from g in gateways
                           from d in g.Devices
                           select new
                           {
                               Gateway = g,
                               Device = d
                           }).ToArray();

            while (!cancellationToken.IsCancellationRequested)
            {
                var deviceComposite = devices[NextDeviceIndex()];
                var payload = CreateMessagePayload(
                    deviceComposite.Gateway.Name,
                    deviceComposite.Device.Name);

                using (var message = new Microsoft.Azure.Devices.Client.Message(payload))
                {
                    var timeoutSource = new CancellationTokenSource(_messageTimeout);
                    var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutSource.Token);

                    message.ContentEncoding = "utf-8";
                    message.ContentType = "application/json";
                    try
                    {
                        await deviceComposite.Device.Client.SendEventAsync(
                            message,
                            combinedSource.Token);

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

        private byte[] CreateMessagePayload(string gatewayName, string deviceName)
        {
            var random = new Random();
            var payload = from i in Enumerable.Range(0, _configuration.MessageSize)
                          select (char)(random.Next((int)'A', (int)'Z'));
            var currentTime = DateTime.Now.ToUniversalTime();
            var currentTimeString = DateTime.Now.ToUniversalTime().ToString(
                "o",
                CultureInfo.CreateSpecificCulture("en-US"));
            var currentTimeTs = currentTime.Subtract(TIMESTAMP_ORIGIN).TotalMilliseconds;
            var message = new
            {
                gatewayId = gatewayName,
                deviceId = deviceName,
                filling = new string(payload.ToArray()),
                recordedAt = currentTimeString,
                recordedAtTs = currentTimeTs
            };
            var binary = JsonSerializer.SerializeToUtf8Bytes(message);

            return binary;
        }

        private int NextDeviceIndex()
        {
            var nextIndex = Interlocked.Increment(ref _clientIndex)
                % _configuration.TotalDeviceCount;

            return nextIndex;
        }
    }
}