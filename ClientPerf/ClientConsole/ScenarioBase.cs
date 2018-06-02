using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Age = 42,
                Address = new
                {
                    Street = "Baker",
                    StreetNumber = "221B"
                },
                Skills = new[] { "Engineer", "Flight", "Programming", "Talking" },
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            return dummyEvent;
        }

        protected async static Task<TimeSpan> TimeFunctionAsync(Func<Task> asyncFunction)
        {
            var watch = new Stopwatch();

            watch.Start();
            await asyncFunction();

            return watch.Elapsed;
        }
    }
}