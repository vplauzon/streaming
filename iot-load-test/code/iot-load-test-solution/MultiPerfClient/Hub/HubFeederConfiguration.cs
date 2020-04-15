using System;
using System.Collections.Generic;

namespace MultiPerfClient.Hub
{
    internal class HubFeederConfiguration
    {
        public HubFeederConfiguration()
        {
            var cosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_CONN_STRING");
            var iotConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            var gatewayCountText = Environment.GetEnvironmentVariable("GATEWAY_COUNT");
            int gatewayCount;
            var devicePerGatewayText = Environment.GetEnvironmentVariable("DEVICE_PER_GATEWAY");
            int devicePerGateway;
            var registrationsPerSecondText = Environment.GetEnvironmentVariable("REGISTRATIONS_PER_SECOND");
            int registrationsPerSecond;
            var concurrentMessagesCountText = Environment.GetEnvironmentVariable("CONCURRENT_MESSAGES_COUNT");
            int concurrentMessagesCount;
            var messageSizeText = Environment.GetEnvironmentVariable("MESSAGE_SIZE_IN_BYTE");
            int messageSize;

            if (string.IsNullOrWhiteSpace(cosmosConnectionString))
            {
                throw new ArgumentNullException("Environment variable missing", "COSMOS_CONN_STRING");
            }
            else if (string.IsNullOrWhiteSpace(iotConnectionString))
            {
                throw new ArgumentNullException("Environment variable missing", "IOT_CONN_STRING");
            }
            else if (string.IsNullOrWhiteSpace(gatewayCountText))
            {
                throw new ArgumentNullException("Environment variable missing", "GATEWAY_COUNT");
            }
            else if (!int.TryParse(gatewayCountText, out gatewayCount))
            {
                throw new ArgumentException("Env Var isn't an integer", "GATEWAY_COUNT");
            }
            else if (string.IsNullOrWhiteSpace(devicePerGatewayText))
            {
                throw new ArgumentNullException("Environment variable missing", "DEVICE_PER_GATEWAY");
            }
            else if (!int.TryParse(devicePerGatewayText, out devicePerGateway))
            {
                throw new ArgumentException("Env Var isn't an integer", "DEVICE_PER_GATEWAY");
            }
            else if (string.IsNullOrWhiteSpace(registrationsPerSecondText))
            {
                throw new ArgumentNullException("Environment variable missing", "REGISTRATIONS_PER_SECOND");
            }
            else if (!int.TryParse(registrationsPerSecondText, out registrationsPerSecond))
            {
                throw new ArgumentException("Env Var isn't an integer", "REGISTRATIONS_PER_SECOND");
            }
            else if (string.IsNullOrWhiteSpace(concurrentMessagesCountText))
            {
                throw new ArgumentNullException("Environment variable missing", "CONCURRENT_MESSAGES_COUNT");
            }
            else if (!int.TryParse(concurrentMessagesCountText, out concurrentMessagesCount))
            {
                throw new ArgumentException("Env Var isn't an integer", "CONCURRENT_MESSAGES_COUNT");
            }
            else if (string.IsNullOrWhiteSpace(messageSizeText))
            {
                throw new ArgumentNullException("Environment variable missing", "MESSAGE_SIZE_IN_BYTE");
            }
            else if (!int.TryParse(messageSizeText, out messageSize))
            {
                throw new ArgumentException("Env Var isn't an integer", "MESSAGE_SIZE_IN_BYTE");
            }
            IotConnectionString = iotConnectionString;
            CosmosConnectionString = cosmosConnectionString;
            GatewayCount = gatewayCount;
            DevicePerGateway = devicePerGateway;
            RegistrationsPerSecond = registrationsPerSecond;
            ConcurrentMessagesCount = concurrentMessagesCount;
            MessageSize = messageSize;
        }

        public string CosmosConnectionString { get; }
        
        public string IotConnectionString { get; }

        public int GatewayCount { get; }

        public int DevicePerGateway { get; }

        public int TotalDeviceCount
        {
            get { return GatewayCount * DevicePerGateway; }
        }

        public int RegistrationsPerSecond { get; }

        public int ConcurrentMessagesCount { get; }

        public int MessageSize { get; }
    }
}