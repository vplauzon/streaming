using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public interface IEventHubClient
    {
        Task SendAsync(object jsonPayload);

        Task SendBatchAsync(IEnumerable<object> batch);

        Task CloseAsync();
    }
}