using System;

namespace IISAppCmd.IIS
{
    public class ApplicationPoolFailure
    {
        public LoadBalancerCapabilities LoadBalancerCapabilities { get; set; } = LoadBalancerCapabilities.HttpLevel;

        public bool OrphanWorkerProcess { get; set; }

        public string OrphanActionExe { get; set; }

        public string OrphanActionParams { get; set; }

        public bool RapidFailProtection { get; set; } = true;

        public TimeSpan RapidFailProtectionInterval { get; set; } = TimeSpan.FromMinutes(5);

        public uint RapidFailProtectionMaxCrashes { get; set; } = 5;

        public string AutoShutdownExe { get; set; }

        public string AutoShutdownParams { get; set; }
    }
}
