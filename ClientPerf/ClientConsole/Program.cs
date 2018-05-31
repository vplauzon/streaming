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
        static void Main(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            var scenario = Environment.GetEnvironmentVariable("SCENARIO");

            Console.WriteLine($"Connection String:  {connectionString}");
            Console.WriteLine($"Scenario:  {scenario}");

            switch(scenario.ToUpper())
            {
                case "ISOLATION":
                    new Isolation(connectionString).RunAsync().Wait();
                    break;
                default:
                    Console.WriteLine($"Unsupported scenario:  {scenario}");
                    break;
            }
        }
    }
}