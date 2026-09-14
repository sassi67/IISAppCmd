using System;
using System.Collections.Generic;

namespace IISAppCmd.IIS
{
    public class ApplicationPoolRecycling
    {
        public bool DisallowOverlappingRotation { get; set; }

        public bool DisallowRotationOnConfigChange { get; set; }

        public RecyclingLogEvents LogEventOnRecycle { get; set; } =
            RecyclingLogEvents.Time | RecyclingLogEvents.Requests | RecyclingLogEvents.Schedule |
            RecyclingLogEvents.Memory | RecyclingLogEvents.IsapiUnhealthy | RecyclingLogEvents.OnDemand |
            RecyclingLogEvents.ConfigChange | RecyclingLogEvents.PrivateMemory;

        public ApplicationPoolPeriodicRestart PeriodicRestart { get; set; } = new ApplicationPoolPeriodicRestart();
    }

    public class ApplicationPoolPeriodicRestart
    {
        public uint Memory { get; set; }

        public uint PrivateMemory { get; set; }

        public uint Requests { get; set; }

        public TimeSpan Time { get; set; } = new TimeSpan(1, 5, 0, 0);

        public List<TimeSpan> Schedule { get; set; } = new List<TimeSpan>();
    }
}
