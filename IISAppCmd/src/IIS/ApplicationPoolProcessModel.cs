using System;

namespace IISAppCmd.IIS
{
    public class ApplicationPoolProcessModel
    {
        public ProcessModelIdentityType IdentityType { get; set; } = ProcessModelIdentityType.ApplicationPoolIdentity;

        public string UserName { get; set; }

        public string Password { get; set; }

        public string RequestQueueDelegatorIdentity { get; set; }

        public bool LoadUserProfile { get; set; }

        public bool SetProfileEnvironment { get; set; } = true;

        public LogonType LogonType { get; set; } = LogonType.LogonBatch;

        public bool ManualGroupMembership { get; set; }

        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(20);

        public IdleTimeoutAction IdleTimeoutAction { get; set; } = IdleTimeoutAction.Terminate;

        public uint MaxProcesses { get; set; } = 1;

        public TimeSpan ShutdownTimeLimit { get; set; } = TimeSpan.FromSeconds(90);

        public TimeSpan StartupTimeLimit { get; set; } = TimeSpan.FromSeconds(90);

        public bool PingingEnabled { get; set; } = true;

        public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan PingResponseTime { get; set; } = TimeSpan.FromSeconds(90);

        public ProcessModelLogEvents LogEventOnProcessModel { get; set; } = ProcessModelLogEvents.IdleTimeout;
    }
}
