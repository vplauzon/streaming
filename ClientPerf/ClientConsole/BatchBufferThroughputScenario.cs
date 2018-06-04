using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BatchBufferThroughputScenario : ScenarioBase
    {
        private readonly bool _isSafe;
        private readonly int _batchSize;
        private readonly int _threadCount;
        private readonly TimeSpan _samplingTime;

        public BatchBufferThroughputScenario(
            string connectionString,
            bool isAmqp,
            bool isSafe,
            int batchSize,
            int threadCount,
            TimeSpan samplingTime)
            : base(connectionString, isAmqp)
        {
            _isSafe = isSafe;
            _batchSize = batchSize;
            _threadCount = threadCount;
            _samplingTime = samplingTime;
        }

        public override async Task RunAsync()
        {
            int count = 0;
            var elapsed = await TimeFunctionAsync(async () =>
            {
                var pool = new EventHubClientPool(() => CreateEventHubClient());
                var proxyClient = _isSafe
                    ? new SafeBufferBatchEventHubClient(pool, _batchSize) as IEventHubClient
                    : new UnsafeBufferBatchEventHubClient(pool, _batchSize);

                try
                {
                    var cancellationTokenSource = new CancellationTokenSource(_samplingTime);
                    var threads = (from i in Enumerable.Range(0, _threadCount)
                                   select OneThreadAsync(proxyClient, cancellationTokenSource.Token)).ToArray();

                    await Task.WhenAll(threads);

                    var counts = from t in threads
                                 select t.Result;

                    count = counts.Sum();
                }
                finally
                {
                    await proxyClient.CloseAsync();
                }
            });

            Console.WriteLine($"Total Events:  {count}");
            Console.WriteLine($"Duration:  {elapsed}");
            Console.WriteLine($"Throughput:  {(double)count / elapsed.TotalSeconds}");
        }

        private async Task<int> OneThreadAsync(IEventHubClient client, CancellationToken cancellationToken)
        {
            int eventCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                await client.SendAsync(GetDummyEventObject());
                ++eventCount;
            }

            return eventCount;
        }
    }
}