﻿using System;
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
            var batchSize = ParseInt(Environment.GetEnvironmentVariable("BATCH_SIZE"));
            var isAmqp = protocol.ToLower() == "amqp";

            Console.WriteLine($"Connection String:  {connectionString}");
            Console.WriteLine($"Scenario:  {scenario}");
            Console.WriteLine($"Protocol:  {protocol}");
            Console.WriteLine($"Batch Size:  {batchSize}");
            Console.WriteLine();

            switch (scenario.ToLower())
            {
                case "isolated-perf":
                    new IsolatedPerfScenario(connectionString, isAmqp).RunAsync().Wait();
                    break;
                case "batch-one-by-one-perf":
                    new BatchOneByOnePerfScenario(connectionString, isAmqp, batchSize).RunAsync().Wait();
                    break;
                case "batch-perf":
                    new BatchPerfScenario(connectionString, isAmqp, batchSize).RunAsync().Wait();
                    break;
                case "isolated-throughput":
                    new IsolatedThroughputScenario(connectionString, isAmqp).RunAsync().Wait();
                    break;
                case "batch-one-by-one-throughput":
                    new IsolatedThroughputScenario(connectionString, isAmqp).RunAsync().Wait();
                    break;
                default:
                    Console.WriteLine($"Unsupported scenario:  {scenario}");
                    break;
            }

            var totalEvents = AmqpEventHubClient.GetTotalEventCountAsync(connectionString).Result;

            Console.WriteLine();
            Console.WriteLine($"Total number of events:  {totalEvents}");
        }

        private static int ParseInt(string text)
        {
            int value;

            return int.TryParse(text, out value)
                ? value
                : 0;
        }
    }
}