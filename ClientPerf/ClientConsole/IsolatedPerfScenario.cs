using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class IsolatedPerfScenario : ScenarioBase
    {
        public IsolatedPerfScenario(string connectionString, bool isAmqp) : base(connectionString, isAmqp)
        {
        }

        public override async Task RunAsync()
        {
            for (int i = 0; i != 10; ++i)
            {
                var elasped = await TimeFunctionAsync(SendOneEventAsync);

                Console.WriteLine($"One Event:  {elasped}");
            }
        }

        private async Task SendOneEventAsync()
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
        }
    }
}