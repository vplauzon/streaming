using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class BufferBatchEventHubClient : IEventHubClient
    {
        private readonly EventHubClientPool _clientPool;
        private readonly int _batchSize;
        private readonly ConcurrentQueue<(object, TaskCompletionSource<object>)> _queue =
            new ConcurrentQueue<(object, TaskCompletionSource<object>)>();

        public BufferBatchEventHubClient(Func<IEventHubClient> clientFactory, int batchSize)
        {
            _clientPool = new EventHubClientPool(clientFactory);
            _batchSize = batchSize;
        }

        async Task IEventHubClient.SendAsync(object jsonPayload)
        {
            var item = (jsonPayload, taskSource: new TaskCompletionSource<object>());

            _queue.Enqueue(item);
            await item.taskSource.Task;
            //item.taskSource.SetResult(null);
            throw new NotImplementedException();
        }

        async Task IEventHubClient.SendBatchAsync(IEnumerable<object> batch)
        {
            var client = _clientPool.GetClient();

            try
            {
                await client.SendBatchAsync(batch);
            }
            finally
            {
                _clientPool.ReleaseClient(client);
            }
        }

        async Task IEventHubClient.CloseAsync()
        {
            await _clientPool.CloseAsync();
        }
    }
}