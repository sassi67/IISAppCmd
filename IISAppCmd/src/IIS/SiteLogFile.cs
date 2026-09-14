using System.Collections.Generic;

namespace IISAppCmd.IIS
{
    public class SiteLogFile
    {
        public LogExtFileFlags LogExtFileFlags { get; set; } =
            LogExtFileFlags.Date | LogExtFileFlags.Time | LogExtFileFlags.ClientIP | LogExtFileFlags.UserName |
            LogExtFileFlags.ServerIP | LogExtFileFlags.Method | LogExtFileFlags.UriStem | LogExtFileFlags.UriQuery |
            LogExtFileFlags.TimeTaken | LogExtFileFlags.HttpStatus | LogExtFileFlags.Win32Status |
            LogExtFileFlags.BytesSent | LogExtFileFlags.BytesRecv | LogExtFileFlags.ServerPort |
            LogExtFileFlags.UserAgent | LogExtFileFlags.HttpSubStatus | LogExtFileFlags.Referer;

        public string CustomLogPluginClsid { get; set; } = "";

        public LogFormat LogFormat { get; set; } = LogFormat.W3C;

        public LogTargetW3C LogTargetW3C { get; set; } = LogTargetW3C.File;

        public string Directory { get; set; } = @"%SystemDrive%\inetpub\logs\LogFiles";

        public LogPeriod Period { get; set; } = LogPeriod.Daily;

        public long TruncateSize { get; set; } = 20971520;

        public bool LocalTimeRollover { get; set; }

        public bool Enabled { get; set; } = true;

        public bool LogSiteId { get; set; } = true;

        public uint FlushByEntryCountW3CLog { get; set; }

        public uint MaxLogLineLength { get; set; } = 65536;

        public SiteLogCustomFields CustomFields { get; set; } = new SiteLogCustomFields();
    }

    public class SiteLogCustomFields
    {
        public uint MaxCustomFieldLength { get; set; } = 4096;

        public List<SiteLogCustomField> Fields { get; set; } = new List<SiteLogCustomField>();
    }

    public class SiteLogCustomField
    {
        public string LogFieldName { get; set; }

        public string SourceName { get; set; }

        public CustomLogFieldSourceType SourceType { get; set; }
    }
}
