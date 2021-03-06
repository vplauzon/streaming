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
            var isAmqp = protocol.ToLower() == "amqp";
            var batchSize = ParseInt(Environment.GetEnvironmentVariable("BATCH_SIZE"));
            var threadCount = ParseInt(Environment.GetEnvironmentVariable("THREAD_COUNT"));
            var samplingTime = ParseTimeSpan(Environment.GetEnvironmentVariable("SAMPLING_TIME"));
            var delayStart = ParseTimeSpan(Environment.GetEnvironmentVariable("DELAY_START"));

            Console.WriteLine($"Connection String:  {connectionString}");
            Console.WriteLine($"Scenario:  {scenario}");
            Console.WriteLine($"Protocol:  {protocol}");
            Console.WriteLine($"Batch Size:  {batchSize}");
            Console.WriteLine($"Thread Count:  {threadCount}");
            Console.WriteLine($"Sampling Time:  {samplingTime}");
            Console.WriteLine($"Delay Start:  {delayStart}");
            Console.WriteLine();

            Task.Delay(delayStart).Wait();
            Console.WriteLine("Starting");
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
                    new IsolatedThroughputScenario(connectionString, isAmqp, threadCount, samplingTime).RunAsync().Wait();
                    break;
                case "pool-late-release-throughput":
                    new PoolThroughputScenario(connectionString, isAmqp, threadCount, samplingTime, false).RunAsync().Wait();
                    break;
                case "pool-early-release-throughput":
                    new PoolThroughputScenario(connectionString, isAmqp, threadCount, samplingTime, true).RunAsync().Wait();
                    break;
                case "safe-batch-buffer-throughput":
                    new BatchBufferThroughputScenario(connectionString, isAmqp, true, batchSize, threadCount, samplingTime).RunAsync().Wait();
                    break;
                case "unsafe-batch-buffer-throughput":
                    new BatchBufferThroughputScenario(connectionString, isAmqp, false, batchSize, threadCount, samplingTime).RunAsync().Wait();
                    break;
                default:
                    Console.WriteLine($"Unsupported scenario:  {scenario}");
                    break;
            }

            var totalEvents = AmqpEventHubClient.GetTotalEventCountAsync(connectionString).Result;

            Console.WriteLine();
            Console.WriteLine($"Total number of events:  {totalEvents}");
        }

        private static TimeSpan ParseTimeSpan(string text)
        {
            TimeSpan value;

            return TimeSpan.TryParse(text, out value)
                ? value
                : TimeSpan.Zero;
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