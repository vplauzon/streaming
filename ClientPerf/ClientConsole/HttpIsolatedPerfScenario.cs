using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClientConsole
{
    /// <summary>
    /// Based on https://docs.microsoft.com/en-us/rest/api/eventhub/Send-event
    /// </summary>
    public class HttpIsolatedPerfScenario : ScenarioBase
    {
        public HttpIsolatedPerfScenario(string connectionString) : base(connectionString)
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
            var client = HttpEventHubClient.CreateFromConnectionString(ConnectionString);

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