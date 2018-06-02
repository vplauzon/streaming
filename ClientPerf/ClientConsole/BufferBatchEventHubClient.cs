using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BufferBatchEventHubClient : IEventHubClient
    {
        private readonly IEventHubClient _client;

        public BufferBatchEventHubClient(IEventHubClient client)
        {
            _client = client;
        }

        Task IEventHubClient.SendAsync(object jsonPayload)
        {
            throw new NotImplementedException();
        }

        async Task IEventHubClient.SendBatchAsync(IEnumerable<object> batch)
        {
            await _client.SendBatchAsync(batch);
        }

        async Task IEventHubClient.CloseAsync()
        {
            await _client.CloseAsync();
        }
    }
}