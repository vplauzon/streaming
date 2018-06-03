using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class EventHubClientPool
    {
        private readonly IProducerConsumerCollection<IEventHubClient> _queue =
            new ConcurrentQueue<IEventHubClient>();
        private readonly Func<IEventHubClient> _clientFactory;

        public EventHubClientPool(Func<IEventHubClient> clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public IEventHubClient GetClient()
        {
            IEventHubClient client;

            if (!_queue.TryTake(out client))
            {
                client = _clientFactory();
            }

            return client;
        }

        public void ReleaseClient(IEventHubClient client)
        {
            _queue.TryAdd(client);
        }

        public async Task CloseAsync()
        {
            var tasks = from client in _queue.ToArray()
                        select client.CloseAsync();

            await Task.WhenAll(tasks);
        }
    }
}