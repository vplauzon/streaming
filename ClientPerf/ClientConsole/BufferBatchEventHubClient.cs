using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ClientConsole
{
    public class BufferBatchEventHubClient : IEventHubClient
    {
        #region Inner Types
        private class BufferedItem
        {
            private readonly TaskCompletionSource<object> _completionSource =
                new TaskCompletionSource<object>();

            public BufferedItem(object payload)
            {
                Payload = payload;
            }

            public object Payload { get; }

            public Task WaitForCompletionAsync() => _completionSource.Task;

            public void Complete() => _completionSource.SetResult(null);
        }
        #endregion

        private static readonly TimeSpan BUFFER_DELAY = TimeSpan.FromSeconds(.1);

        private readonly EventHubClientPool _clientPool;
        private readonly int _batchSize;
        private readonly ConcurrentQueue<BufferedItem> _queue =
            new ConcurrentQueue<BufferedItem>();
        private CancellationTokenSource _currentBufferingTokenSource;

        public BufferBatchEventHubClient(EventHubClientPool pool, int batchSize)
        {
            _clientPool = pool ?? throw new ArgumentNullException(nameof(pool));
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Must be greater than zero");
            }
            _batchSize = batchSize;
        }

        async Task IEventHubClient.SendAsync(object jsonPayload)
        {
            var item = new BufferedItem(jsonPayload);
            Task delayTask = null;

            lock (_queue)
            {
                _queue.Enqueue(item);
                if (_currentBufferingTokenSource == null)
                {
                    _currentBufferingTokenSource = new CancellationTokenSource();
                    delayTask = Task.Delay(BUFFER_DELAY, _currentBufferingTokenSource.Token);
                }
                if (_queue.Count >= _batchSize)
                {   //  Force the buffering to stop and the batch send to occur "now"
                    _currentBufferingTokenSource.Cancel();
                    _currentBufferingTokenSource = null;
                }
            }
            if (delayTask != null)
            {   //  This thread won the actual send process
                await delayTask;
                await SendBufferedBatchAsync();
            }
            await item.WaitForCompletionAsync();
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
            var allItemWaitTask = from i in _queue.ToArray()
                                  select i.WaitForCompletionAsync();

            await Task.WhenAll(allItemWaitTask);
            await _clientPool.CloseAsync();
        }

        private async Task SendBufferedBatchAsync()
        {
            var buffer = DequeueBuffer();

            if (buffer.Length > 0)
            {
                var payloads = (from item in buffer
                                select item.Payload).ToArray();
                var client = _clientPool.GetClient();

                await client.SendBatchAsync(payloads);
                _clientPool.ReleaseClient(client);

                foreach (var item in buffer)
                {
                    item.Complete();
                }
            }
        }

        private BufferedItem[] DequeueBuffer()
        {
            var list = new List<BufferedItem>();

            lock (_queue)
            {
                while (list.Count < _batchSize)
                {
                    BufferedItem item;

                    if (_queue.TryDequeue(out item))
                    {
                        list.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
                _currentBufferingTokenSource = null;
            }

            return list.ToArray();
        }
    }
}