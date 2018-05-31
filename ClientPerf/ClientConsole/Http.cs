using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    /// <summary>
    /// Based on https://docs.microsoft.com/en-us/rest/api/eventhub/Send-event
    /// </summary>
    public class Http : Scenario
    {
        public Http(string connectionString) : base(connectionString)
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
            var builder = new EventHubsConnectionStringBuilder(ConnectionString);
            var nameSpace = builder.Endpoint.Authority.Split('.')[0];
            var client = new HttpClient
            {
                BaseAddress = new Uri($"https://{nameSpace}.servicebus.windows.net/")
            };
            var url = $"{builder.EntityPath}/messages";
            var content = new StringContent(
                GetDummyEventString(),
                Encoding.UTF8,
                "application/json");

            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                $"SharedAccessSignature sr={builder.SasKeyName}");

            var response = await client.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
        }
    }
}