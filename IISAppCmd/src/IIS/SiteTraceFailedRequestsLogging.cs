namespace IISAppCmd.IIS
{
    public class SiteTraceFailedRequestsLogging
    {
        public bool Enabled { get; set; }

        public string Directory { get; set; } = @"%SystemDrive%\inetpub\logs\FailedReqLogFiles";

        public uint MaxLogFiles { get; set; } = 50;

        public uint MaxLogFileSizeKB { get; set; } = 1024;

        public bool CustomActionsEnabled { get; set; }
    }
}
