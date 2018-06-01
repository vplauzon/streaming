using System;
using System.Threading.Tasks;

namespace ClientConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            var scenario = Environment.GetEnvironmentVariable("SCENARIO");

            Console.WriteLine($"Connection String:  {connectionString}");
            Console.WriteLine($"Scenario:  {scenario}");

            switch(scenario.ToLower())
            {
                case "amqp-isolated-perf":
                    new AmqpIsolatedPerf(connectionString).RunAsync().Wait();
                    break;
                case "http-isolated-perf":
                    new HttpIsolatedPerf(connectionString).RunAsync().Wait();
                    break;
                default:
                    Console.WriteLine($"Unsupported scenario:  {scenario}");
                    break;
            }
        }
    }
}