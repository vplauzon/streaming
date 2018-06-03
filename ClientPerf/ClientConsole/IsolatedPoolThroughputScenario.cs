using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class IsolatedPoolThroughputScenario : ScenarioBase
    {
        private readonly int _threadCount;
        private readonly TimeSpan _samplingTime;

        public IsolatedPoolThroughputScenario(string connectionString, bool isAmqp, int threadCount, TimeSpan samplingTime)
            : base(connectionString, isAmqp)
        {
            _threadCount = threadCount;
            _samplingTime = samplingTime;
        }

        public override async Task RunAsync()
        {
            var pool = new EventHubClientPool(() => CreateEventHubClient());

            try
            {
                int count = 0;
                var elapsed = await TimeFunctionAsync(async () =>
                {
                    var cancellationTokenSource = new CancellationTokenSource(_samplingTime);
                    var threads = (from i in Enumerable.Range(0, _threadCount)
                                   select OneThreadAsync(pool, cancellationTokenSource.Token)).ToArray();

                    await Task.WhenAll(threads);

                    var counts = from t in threads
                                 select t.Result;

                    count = counts.Sum();
                });

                Console.WriteLine($"Total Events:  {count}");
                Console.WriteLine($"Duration:  {elapsed}");
                Console.WriteLine($"Throughput:  {(double)count / elapsed.TotalSeconds}");
            }
            finally
            {
                await pool.CloseAsync();
            }
        }

        private async Task<int> OneThreadAsync(EventHubClientPool pool, CancellationToken cancellationToken)
        {
            int eventCount = 0;
            var client = pool.GetClient();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await client.SendAsync(GetDummyEventObject());
                    ++eventCount;
                }

                return eventCount;
            }
            finally
            {
                pool.ReleaseClient(client);
            }
        }
    }
}