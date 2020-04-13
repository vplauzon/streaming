using System;

namespace MultiPerfClient.Cosmos
{
    internal class CosmosPingerConfiguration
    {
        public CosmosPingerConfiguration()
        {
            var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
            var concurrentCallCountText = Environment.GetEnvironmentVariable("CONCURRENT_CALL_COUNT");
            int concurrentCallCount;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("Environment variable missing", "COSMOS_CONNECTION_STRING");
            }
            else if (string.IsNullOrWhiteSpace(concurrentCallCountText))
            {
                throw new ArgumentNullException("Environment variable missing", "CONCURRENT_CALL_COUNT");
            }
            else if (!int.TryParse(concurrentCallCountText, out concurrentCallCount))
            {
                throw new ArgumentException("Env Var isn't an integer", "CONCURRENT_CALL_COUNT");
            }
            ConnectionString = connectionString;
            ConcurrentCallCount = concurrentCallCount;
        }

        public string ConnectionString { get; }

        public int ConcurrentCallCount { get; }
    }
}