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
            var registrationsPerSecondText = Environment.GetEnvironmentVariable("REGISTRATIONS_PER_SECOND");
            int registrationsPerSecond;
            var messagesPerSecondText = Environment.GetEnvironmentVariable("MESSAGES_PER_SECOND");
            int messagesPerSecond;
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
            else if (string.IsNullOrWhiteSpace(registrationsPerSecondText))
            {
                throw new ArgumentNullException("Environment variable missing", "REGISTRATIONS_PER_SECOND");
            }
            else if (!int.TryParse(registrationsPerSecondText, out registrationsPerSecond))
            {
                throw new ArgumentException("Env Var isn't an integer", "REGISTRATIONS_PER_SECOND");
            }
            else if (string.IsNullOrWhiteSpace(messagesPerSecondText))
            {
                throw new ArgumentNullException("Environment variable missing", "MESSAGES_PER_SECOND");
            }
            else if (!int.TryParse(messagesPerSecondText, out messagesPerSecond))
            {
                throw new ArgumentException("Env Var isn't an integer", "MESSAGES_PER_SECOND");
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
            RegistrationsPerSecond = registrationsPerSecond;
            MessagesPerSecond = messagesPerSecond;
            MessageSize = messageSize;
        }

        public string ConnectionString { get; }

        public int DeviceCount { get; }

        public int RegistrationsPerSecond { get; }
        
        public int MessagesPerSecond { get; }

        public int MessageSize { get; }
    }
}