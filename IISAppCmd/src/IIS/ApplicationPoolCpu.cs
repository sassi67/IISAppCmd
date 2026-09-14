using System;

namespace IISAppCmd.IIS
{
    public class ApplicationPoolCpu
    {
        public uint Limit { get; set; }

        public CpuAction Action { get; set; } = CpuAction.NoAction;

        public TimeSpan ResetInterval { get; set; } = TimeSpan.FromMinutes(5);

        public bool SmpAffinitized { get; set; }

        public uint SmpProcessorAffinityMask { get; set; } = uint.MaxValue;

        public uint SmpProcessorAffinityMask2 { get; set; } = uint.MaxValue;

        public int ProcessorGroup { get; set; }

        public NumaNodeAssignment NumaNodeAssignment { get; set; } = NumaNodeAssignment.MostAvailableMemory;

        public NumaNodeAffinityMode NumaNodeAffinityMode { get; set; } = NumaNodeAffinityMode.Soft;
    }
}
