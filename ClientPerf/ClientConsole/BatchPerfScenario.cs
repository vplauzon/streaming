using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BatchPerfScenario : ScenarioBase
    {
        private const int BATCH_SIZE = 50;

        public BatchPerfScenario(string connectionString, bool isAmqp) :
            base(connectionString, isAmqp)
        {
        }

        public override async Task RunAsync()
        {
            for (int i = 0; i != 10; ++i)
            {
                var elasped = await TimeFunctionAsync(SendBatchEventAsync);

                Console.WriteLine($"{BATCH_SIZE} events:  {elasped}");
            }
        }

        private async Task SendBatchEventAsync()
        {
            var client = CreateEventHubClient();
            var batch = from i in Enumerable.Range(0, BATCH_SIZE)
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