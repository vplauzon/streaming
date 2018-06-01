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

        public static async Task<long> GetTotalEventCountAsync(string connectionString)
        {
            var client = EventHubClient.CreateFromConnectionString(connectionString);
            var info = await client.GetRuntimeInformationAsync();
            var partitionInfoTasks = from id in info.PartitionIds
                                     select client.GetPartitionRuntimeInformationAsync(id);

            await Task.WhenAll(partitionInfoTasks);

            var partitionInfo = from t in partitionInfoTasks
                                select t.Result;
            var lengths = from p in partitionInfo
                          select p.LastEnqueuedSequenceNumber - p.BeginSequenceNumber;
            var total = lengths.Sum();

            return total;
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