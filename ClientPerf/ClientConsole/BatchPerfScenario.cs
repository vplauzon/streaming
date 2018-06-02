using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BatchPerfScenario : ScenarioBase
    {
        private int _batchSize;

        public BatchPerfScenario(string connectionString, bool isAmqp, int batchSize) :
            base(connectionString, isAmqp)
        {
            _batchSize = batchSize;
        }

        public override async Task RunAsync()
        {
            for (int i = 0; i != 10; ++i)
            {
                var elasped = await TimeFunctionAsync(SendBatchEventAsync);

                Console.WriteLine($"{_batchSize} events:  {elasped}");
            }
        }

        private async Task SendBatchEventAsync()
        {
            var client = CreateEventHubClient();
            var batch = from i in Enumerable.Range(0, _batchSize)
                        select GetDummyEventObject();

            try
            {
                await client.SendBatchAsync(batch);
            }
            finally
            {
                await client.CloseAsync();
            }
        }
    }
}