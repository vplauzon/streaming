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
        public ScenarioBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public abstract Task RunAsync();

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