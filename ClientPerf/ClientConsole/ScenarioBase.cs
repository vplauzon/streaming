using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public abstract class ScenarioBase
    {
        private readonly string _connectionString;
        private readonly bool _isAmqp;

        public ScenarioBase(string connectionString, bool isAmqp)
        {
            _connectionString = connectionString;
            _isAmqp = isAmqp;
        }

        public abstract Task RunAsync();

        protected IEventHubClient CreateEventHubClient()
        {
            return _isAmqp
                ? AmqpEventHubClient.CreateFromConnectionString(_connectionString)
                : HttpEventHubClient.CreateFromConnectionString(_connectionString);
        }

        protected static object GetDummyEventObject()
        {
            var dummyEvent = new
            {
                Name = "John Smith",
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            return dummyEvent;
        }

        protected async static Task<TimeSpan> TimeFunctionAsync(Func<Task> function)
        {
            var watch = new Stopwatch();

            watch.Start();
            await function();

            return watch.Elapsed;
        }
    }
}