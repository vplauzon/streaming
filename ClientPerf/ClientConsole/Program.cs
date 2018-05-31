using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ClientConsole
{
    class Program
    {
        const string CONNECTION_STRING = "Endpoint=sb://eh-rovdymiz3vorc.servicebus.windows.net/;SharedAccessKeyName=sendRule;SharedAccessKey=AJYFHvxEEtg/+b+o8pI6divfqf/Jshqjk1//kOakso0=;EntityPath=test-hub";

        static void Main(string[] args)
        {
            for (int i = 0; i != 10; ++i)
            {
                Console.WriteLine($"One Event:  {TimeFunctionAsync(SendOneEventAsync).Result}");
            }
        }

        private async static Task SendOneEventAsync()
        {
            var client = EventHubClient.CreateFromConnectionString(CONNECTION_STRING);

            try
            {
                await client.SendAsync(new EventData(GetDummyEventBinary()));
            }
            finally
            {
                await client.CloseAsync();
            }
        }

        private static byte[] GetDummyEventBinary()
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

        private async static Task<TimeSpan> TimeFunctionAsync(Func<Task> function)
        {
            var watch = new Stopwatch();

            watch.Start();
            await function();

            return watch.Elapsed;
        }
    }
}