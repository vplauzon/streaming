using System;
using System.Collections.Generic;

namespace MultiPerfClient.Hub
{
    internal class HubFeederConfiguration
    {
        public HubFeederConfiguration()
        {
            var connectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            var deviceCountText = Environment.GetEnvironmentVariable("DEVICE_COUNT");
            int deviceCount;
            var batchesPerHourText = Environment.GetEnvironmentVariable("BATCHES_PER_HOUR");
            int batchesPerHour;
            var messageSizeText = Environment.GetEnvironmentVariable("MESSAGE_SIZE_IN_KB");
            int messageSize;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("Environment variable missing", "IOT_CONN_STRING");
            }
            else if (string.IsNullOrWhiteSpace(deviceCountText))
            {
                throw new ArgumentNullException("Environment variable missing", "DEVICE_COUNT");
            }
            else if (!int.TryParse(deviceCountText, out deviceCount))
            {
                throw new ArgumentException("Env Var isn't an integer", "DEVICE_COUNT");
            }
            else if (string.IsNullOrWhiteSpace(batchesPerHourText))
            {
                throw new ArgumentNullException("Environment variable missing", "BATCHES_PER_HOUR");
            }
            else if (!int.TryParse(batchesPerHourText, out batchesPerHour))
            {
                throw new ArgumentException("Env Var isn't an integer", "BATCHES_PER_HOUR");
            }
            else if (string.IsNullOrWhiteSpace(messageSizeText))
            {
                throw new ArgumentNullException("Environment variable missing", "MESSAGE_SIZE_IN_KB");
            }
            else if (!int.TryParse(messageSizeText, out messageSize))
            {
                throw new ArgumentException("Env Var isn't an integer", "MESSAGE_SIZE_IN_KB");
            }
            ConnectionString = connectionString;
            DeviceCount = deviceCount;
            BatchesPerHour = batchesPerHour;
            MessageSize = messageSize;
        }

        public string ConnectionString { get; }

        public int DeviceCount { get; }

        public int BatchesPerHour { get; }

        public int MessageSize { get; }
    }
}