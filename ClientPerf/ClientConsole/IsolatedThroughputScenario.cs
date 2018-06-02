using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class IsolatedThroughputScenario : ScenarioBase
    {
        private readonly int _threadCount;
        private readonly TimeSpan _samplingTime;

        public IsolatedThroughputScenario(string connectionString, bool isAmqp, int threadCount, TimeSpan samplingTime)
            : base(connectionString, isAmqp)
        {
            _threadCount = threadCount;
            _samplingTime = samplingTime;
        }

        public override async Task RunAsync()
        {
            int count = 0;
            var elapsed = await TimeFunctionAsync(async () =>
            {
                var cancellationTokenSource = new CancellationTokenSource(_samplingTime);
                var threads = (from i in Enumerable.Range(0, _threadCount)
                               select OneThreadAsync(cancellationTokenSource.Token)).ToArray();

                await Task.WhenAll(threads);

                var counts = from t in threads
                             select t.Result;

                count = counts.Sum();
            });

            Console.WriteLine($"Total Events:  {count}");
            Console.WriteLine($"Duration:  {elapsed}");
            Console.WriteLine($"Throughput:  {(double)count / elapsed.TotalSeconds}");
        }

        private async Task<int> OneThreadAsync(CancellationToken cancellationToken)
        {
            int eventCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = CreateEventHubClient();

                try
                {
                    await client.SendAsync(GetDummyEventObject());
                }
                finally
                {
                    await client.CloseAsync();
                }
                ++eventCount;
            }

            return eventCount;
        }
    }
}