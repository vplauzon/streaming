using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ClientConsole
{
    public class SafeBufferBatchEventHubClient : IEventHubClient
    {
        #region Inner Regions
        private class BufferedItem
        {
            private readonly TaskCompletionSource<object> _completionSource;

            public BufferedItem(object payload)
            {
                _completionSource = new TaskCompletionSource<object>();
            }

            public object Payload { get; }

            public Task WaitForCompletionAsync() => _completionSource.Task;

            public void MarkAsComplete() => _completionSource.SetResult(null);
        }
        #endregion

        private static readonly TimeSpan BUFFER_DELAY = TimeSpan.FromSeconds(.1);

        private readonly EventHubClientPool _clientPool;
        private readonly int _batchSize;
        private readonly ConcurrentQueue<BufferedItem> _queue =
            new ConcurrentQueue<BufferedItem>();
        private object _currentBatchProcessId = null;

        public SafeBufferBatchEventHubClient(EventHubClientPool pool, int batchSize)
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

            _queue.Enqueue(item);
            await EnsureDelayedBatchProcessAsync();
            await item.WaitForCompletionAsync();
        }

        Task IEventHubClient.SendBatchAsync(IEnumerable<object> batch)
        {
            throw new NotImplementedException("You shouldn't use batch in this context");
        }

        async Task IEventHubClient.CloseAsync()
        {
            //  Ensure queue processing is initiated immediatly
            await ProcessBatchAsync();
            await _clientPool.CloseAsync();
        }

        private async Task EnsureDelayedBatchProcessAsync()
        {
            if (_currentBatchProcessId == null)
            {
                var proposedId = new object();

                if (Interlocked.CompareExchange(ref _currentBatchProcessId, proposedId, null)
                    == null)
                {   //  This thread won the id
                    await WaitAndProcessAsync();
                }
            }
        }

        private async Task WaitAndProcessAsync()
        {
            await Task.Delay(BUFFER_DELAY);
            await ProcessBatchAsync();
        }

        private async Task ProcessBatchAsync()
        {
            var sendTasks = new List<Task>();

            //  Remove delayed batch process
            Interlocked.Exchange(ref _currentBatchProcessId, null);

            //  Process ALL items in queue per batch-size
            while (!_queue.IsEmpty)
            {
                var buffer = DequeueBuffer();

                sendTasks.Add(SendBufferedBatchAsync(buffer));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task SendBufferedBatchAsync(BufferedItem[] buffer)
        {
            if (buffer.Length > 0)
            {
                var client = _clientPool.AcquireClient();
                var payloads = from i in buffer
                               select i.Payload;

                await client.SendBatchAsync(payloads);
                _clientPool.ReleaseClient(client);
                foreach(var i in buffer)
                {
                    i.MarkAsComplete();
                }
            }
        }

        private BufferedItem[] DequeueBuffer()
        {
            var list = new List<BufferedItem>();

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

            return list.ToArray();
        }
    }
}