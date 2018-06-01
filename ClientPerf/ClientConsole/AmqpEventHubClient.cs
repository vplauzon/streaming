using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class AmqpEventHubClient : IEventHubClient
    {
        private readonly EventHubClient _client;

        private AmqpEventHubClient(EventHubClient client)
        {
            _client = client;
        }

        public static IEventHubClient CreateFromConnectionString(string connectionString)
        {
            return new AmqpEventHubClient(EventHubClient.CreateFromConnectionString(connectionString));
        }

        async Task IEventHubClient.SendAsync(object jsonPayload)
        {
            var text = JsonConvert.SerializeObject(jsonPayload);
            var binary = ASCIIEncoding.ASCII.GetBytes(text);

            await _client.SendAsync(new EventData(binary));
        }

        async Task IEventHubClient.SendBatchAsync(IEnumerable<object> batch)
        {
            var batchData = from b in batch
                            let text = JsonConvert.SerializeObject(b)
                            let binary = ASCIIEncoding.ASCII.GetBytes(text)
                            select new EventData(binary);

            await _client.SendAsync(batchData);
        }

        async Task IEventHubClient.CloseAsync()
        {
            await _client.CloseAsync();
        }
    }
}