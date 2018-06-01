using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class Isolation : Scenario
    {
        public Isolation(string connectionString) : base(connectionString)
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
            var client = EventHubClient.CreateFromConnectionString(ConnectionString);

            try
            {
                await client.SendAsync(new EventData(GetDummyEventBinary()));
            }
            finally
            {
                await client.CloseAsync();
            }
        }
    }
}