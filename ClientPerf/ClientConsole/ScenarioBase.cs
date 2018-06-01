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
                Name = "John Smith-A",
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

        protected async static Task<IEnumerable<T>> LoopUntilElapseAsync<T>(Func<Task<T>> asyncFunction, TimeSpan elapse)
        {
            var watch = new Stopwatch();
            var list = new List<T>();

            watch.Start();

            do
            {
                var result = await asyncFunction();

                list.Add(result);
            }
            while (watch.Elapsed < elapse);

            return list;
        }

        //protected async static Task<IEnumerable<T>> ParallelizeAsync<T>(Func<Task<T>> asyncFunction, TimeSpan elapse)
        //{
        //}
    }
}