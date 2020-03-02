using System;
using System.Collections.Generic;
using System.Text;

namespace MultiPerfClient
{
    internal class HubFeederConfiguration
    {
        public HubFeederConfiguration()
        {
            var deviceCountText = Environment.GetEnvironmentVariable("DEVICE_COUNT");
            int deviceCount;
            var messagesPerHourText = Environment.GetEnvironmentVariable("MESSAGES_PER_HOUR");
            int messagesPerHour;
            var messageSizeText = Environment.GetEnvironmentVariable("MESSAGE_SIZE_IN_KB");
            int messageSize;

            ConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentNullException("Env Var 'IOT_CONN_STRING' not set", "IOT_CONN_STRING");
            }
            else if (string.IsNullOrWhiteSpace(deviceCountText))
            {
                throw new ArgumentNullException("Env Var 'DEVICE_COUNT' not set", "DEVICE_COUNT");
            }
            else if (!int.TryParse(deviceCountText, out deviceCount))
            {
                throw new ArgumentException("Env Var 'DEVICE_COUNT' isn't an integer", "DEVICE_COUNT");
            }
            else if (string.IsNullOrWhiteSpace(messagesPerHourText))
            {
                throw new ArgumentNullException("Env Var 'MESSAGES_PER_HOUR' not set", "MESSAGES_PER_HOUR");
            }
            else if (!int.TryParse(messagesPerHourText, out messagesPerHour))
            {
                throw new ArgumentException("Env Var 'MESSAGES_PER_HOUR' isn't an integer", "MESSAGES_PER_HOUR");
            }
            else if (string.IsNullOrWhiteSpace(messageSizeText))
            {
                throw new ArgumentNullException("Env Var 'MESSAGE_SIZE_IN_KB' not set", "MESSAGE_SIZE_IN_KB");
            }
            else if (!int.TryParse(messageSizeText, out messageSize))
            {
                throw new ArgumentException("Env Var 'MESSAGE_SIZE_IN_KB' isn't an integer", "MESSAGE_SIZE_IN_KB");
            }
            DeviceCount = deviceCount;
            MessagesPerHour = messagesPerHour;
            MessageSize = messageSize;
        }

        public string ConnectionString { get; }

        public int DeviceCount { get; }

        public int MessagesPerHour { get; }

        public int MessageSize { get; }
    }
}