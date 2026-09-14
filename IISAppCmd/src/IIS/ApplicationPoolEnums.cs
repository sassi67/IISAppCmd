using System;

namespace IISAppCmd.IIS
{
    public enum ManagedPipelineMode
    {
        Integrated = 0,
        Classic = 1,
    }

    public enum StartMode
    {
        OnDemand = 0,
        AlwaysRunning = 1,
    }

    public enum ProcessModelIdentityType
    {
        LocalSystem = 0,
        LocalService = 1,
        NetworkService = 2,
        SpecificUser = 3,
        ApplicationPoolIdentity = 4,
    }

    public enum LogonType
    {
        LogonBatch = 0,
        LogonService = 1,
    }

    public enum IdleTimeoutAction
    {
        Terminate = 0,
        Suspend = 1,
    }

    [Flags]
    public enum ProcessModelLogEvents
    {
        None = 0,
        IdleTimeout = 1,
    }

    [Flags]
    public enum RecyclingLogEvents
    {
        None = 0,
        Time = 1,
        Requests = 2,
        Schedule = 4,
        Memory = 8,
        IsapiUnhealthy = 16,
        OnDemand = 32,
        ConfigChange = 64,
        PrivateMemory = 128,
    }

    public enum LoadBalancerCapabilities
    {
        TcpLevel = 1,
        HttpLevel = 2,
    }

    public enum CpuAction
    {
        NoAction = 0,
        KillW3wp = 1,
        Throttle = 2,
        ThrottleUnderLoad = 3,
    }

    public enum NumaNodeAssignment
    {
        MostAvailableMemory = 0,
        WindowsScheduling = 1,
    }

    public enum NumaNodeAffinityMode
    {
        Soft = 0,
        Hard = 1,
    }
}
