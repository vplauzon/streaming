using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultiPerfClient.Hub
{
    public class DeviceProxy
    {
        public DeviceProxy(string name, DeviceClient client)
        {
            Name = name;
            Client = client;
        }

        public string Name { get; }

        public DeviceClient Client { get; }
    }
}