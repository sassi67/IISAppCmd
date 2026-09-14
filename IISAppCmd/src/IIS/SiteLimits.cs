using System;

namespace IISAppCmd.IIS
{
    public class SiteLimits
    {
        public uint MaxBandwidth { get; set; } = uint.MaxValue;

        public uint MaxConnections { get; set; } = uint.MaxValue;

        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(2);

        public uint MaxUrlSegments { get; set; } = 32;
    }
}
