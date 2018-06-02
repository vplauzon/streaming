using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BufferBatchEventHubClient : IEventHubClient
    {
        private readonly IEventHubClient _client;
        private readonly int _batchSize;
        private readonly ConcurrentQueue<(object, TaskCompletionSource<object>)> _queue =
            new ConcurrentQueue<(object, TaskCompletionSource<object>)>();

        public BufferBatchEventHubClient(IEventHubClient client, int batchSize)
        {
            _client = client;
            _batchSize = batchSize;
        }

        async Task IEventHubClient.SendAsync(object jsonPayload)
        {
            var item = (jsonPayload, taskSource : new TaskCompletionSource<object>());

            _queue.Enqueue(item);
            await item.taskSource.Task;
            //item.taskSource.SetResult(null);
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