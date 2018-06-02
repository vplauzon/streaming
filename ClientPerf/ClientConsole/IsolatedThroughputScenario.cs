using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class IsolatedThroughputScenario : ScenarioBase
    {
        private const int THREAD_COUNT = 10;
        private static readonly TimeSpan SAMPLING_TIME = TimeSpan.FromSeconds(10);

        public IsolatedThroughputScenario(string connectionString, bool isAmqp)
            : base(connectionString, isAmqp)
        {
        }

        public override async Task RunAsync()
        {
            int count = 0;
            var elapsed = await TimeFunctionAsync(async () =>
            {
                var cancellationTokenSource = new CancellationTokenSource(SAMPLING_TIME);
                var threads = (from i in Enumerable.Range(0, THREAD_COUNT)
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