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
            var protocol = Environment.GetEnvironmentVariable("PROTOCOL");
            var isAmqp = protocol.ToLower() == "amqp";

            Console.WriteLine($"Connection String:  {connectionString}");
            Console.WriteLine($"Scenario:  {scenario}");

            switch(scenario.ToLower())
            {
                case "isolated-perf":
                    new IsolatedPerfScenario(connectionString, isAmqp).RunAsync().Wait();
                    break;
                default:
                    Console.WriteLine($"Unsupported scenario:  {scenario}");
                    break;
            }
        }
    }
}