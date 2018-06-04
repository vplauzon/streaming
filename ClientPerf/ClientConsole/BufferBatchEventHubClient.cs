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
        private static readonly TimeSpan BUFFER_DELAY = TimeSpan.FromSeconds(.1);
        private static readonly int BACKOFF_RATIO = 5;

        private readonly EventHubClientPool _clientPool;
        private readonly int _batchSize;
        private readonly ConcurrentQueue<object> _queue =
            new ConcurrentQueue<object>();
        private object _currentBatchProcessId = null;

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
            _queue.Enqueue(jsonPayload);
            EnsureDelayedBatchProcess();
            if (_queue.Count > BACKOFF_RATIO * _batchSize)
            {   //  Too many elements in the queue
                //  Let's back off the producers
                await Task.Delay(BUFFER_DELAY);
            }
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
            //  Ensure queue processing is initiated immediatly
            await ProcessBatchAsync();
            await _clientPool.CloseAsync();
        }

        private void EnsureDelayedBatchProcess()
        {
            if (_currentBatchProcessId == null)
            {
                var proposedId = new object();

                if (Interlocked.CompareExchange(ref _currentBatchProcessId, proposedId, null)
                    == null)
                {   //  This thread won the id
                    //  We will process batch in the background:  no wait
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    WaitAndProcessAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
        }

        private async Task WaitAndProcessAsync()
        {
            try
            {
                await Task.Delay(BUFFER_DELAY);
                await ProcessBatchAsync();
            }
            //  It would be really important to log errors here as nothing is awaiting
            //  this task, hence errors would go in oblivion
            catch (AggregateException ex)
            {
                Console.WriteLine("AggreagateException in WaitAndProcessAsync:");
                Console.WriteLine(ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in WaitAndProcessAsync:  {ex.Message}");
            }
        }

        private async Task ProcessBatchAsync()
        {
            var sendTasks = new List<Task>();

            //  Remove delayed batch process
            Interlocked.Exchange(ref _currentBatchProcessId, null);

            //  Process ALL items in queue
            while (!_queue.IsEmpty)
            {
                var buffer = DequeueBuffer();

                sendTasks.Add(SendBufferedBatchAsync(buffer));
            }

            await Task.WhenAll(sendTasks);
        }

        private async Task SendBufferedBatchAsync(object[] buffer)
        {
            if (buffer.Length > 0)
            {
                var client = _clientPool.GetClient();

                await client.SendBatchAsync(buffer);
                _clientPool.ReleaseClient(client);
            }
        }

        private object[] DequeueBuffer()
        {
            var list = new List<object>();

            while (list.Count < _batchSize)
            {
                object item;

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