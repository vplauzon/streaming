using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BatchOneByOnePerfScenario : ScenarioBase
    {
        private const int BATCH_SIZE = 50;

        public BatchOneByOnePerfScenario(string connectionString, bool isAmqp) :
            base(connectionString, isAmqp)
        {
        }

        public override async Task RunAsync()
        {
            for (int i = 0; i != 10; ++i)
            {
                var elasped = await TimeFunctionAsync(SendBatchEventAsync);

                Console.WriteLine($"One Event:  {elasped}");
            }
        }

        private async Task SendBatchEventAsync()
        {
            var client = CreateEventHubClient();
            var batch = from i in Enumerable.Range(0, BATCH_SIZE)
                        select GetDummyEventObject();

            try
            {
                foreach (var payload in batch)
                {
                    await client.SendAsync(payload);
                }
            }
            finally
            {
                await client.CloseAsync();
            }
        }
    }
}