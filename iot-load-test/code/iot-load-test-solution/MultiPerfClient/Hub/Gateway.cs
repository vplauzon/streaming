using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MultiPerfClient.Hub
{
    public class Gateway
    {
        public Gateway(string name, IEnumerable<DeviceProxy> devices)
        {
            Name = name;
            Devices = devices.ToImmutableArray();
        }

        public string Name { get; }

        public IImmutableList<DeviceProxy> Devices { get; }
    }
}