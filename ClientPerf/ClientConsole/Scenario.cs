using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    public abstract class Scenario
    {
        public Scenario(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public abstract Task RunAsync();

        protected static byte[] GetDummyEventBinary()
        {
            var dummyEvent = new
            {
                Name = "John Smith",
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            var serializer = new JsonSerializer();
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(jsonWriter, dummyEvent);
            streamWriter.Flush();

            return stream.GetBuffer();
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