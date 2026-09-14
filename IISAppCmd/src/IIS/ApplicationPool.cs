using System.Collections.Generic;

namespace IISAppCmd.IIS
{
    // Mirrors <system.applicationHost/applicationPools/add>; defaults follow IIS_schema.xml.
    public class ApplicationPool
    {
        public string Name { get; set; }

        public uint QueueLength { get; set; } = 1000;

        public bool AutoStart { get; set; } = true;

        public bool Enable32BitAppOnWin64 { get; set; }

        public bool EnableEmulationOnWinArm64 { get; set; } = true;

        public string ManagedRuntimeVersion { get; set; } = "";

        public string ManagedRuntimeLoader { get; set; } = "webengine4.dll";

        public bool EnableConfigurationOverride { get; set; } = true;

        public ManagedPipelineMode ManagedPipelineMode { get; set; } = ManagedPipelineMode.Integrated;

        public string CLRConfigFile { get; set; } = "";

        public bool PassAnonymousToken { get; set; } = true;

        public StartMode StartMode { get; set; } = StartMode.OnDemand;

        public ApplicationPoolProcessModel ProcessModel { get; set; } = new ApplicationPoolProcessModel();

        public ApplicationPoolRecycling Recycling { get; set; } = new ApplicationPoolRecycling();

        public ApplicationPoolFailure Failure { get; set; } = new ApplicationPoolFailure();

        public ApplicationPoolCpu Cpu { get; set; } = new ApplicationPoolCpu();

        public List<ApplicationPoolEnvironmentVariable> EnvironmentVariables { get; set; } = new List<ApplicationPoolEnvironmentVariable>();
    }
}
