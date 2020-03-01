using System;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    internal class HubFeeder
    {
        public static Task RunAsync()
        {
            Console.WriteLine("Hub Feeder");

            return Task.CompletedTask;
        }
    }
}