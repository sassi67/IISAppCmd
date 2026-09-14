using System;

namespace IISAppCmd.IIS
{
    [Flags]
    public enum LogExtFileFlags
    {
        None = 0,
        Date = 1,
        Time = 2,
        ClientIP = 4,
        UserName = 8,
        SiteName = 16,
        ComputerName = 32,
        ServerIP = 64,
        Method = 128,
        UriStem = 256,
        UriQuery = 512,
        HttpStatus = 1024,
        Win32Status = 2048,
        BytesSent = 4096,
        BytesRecv = 8192,
        TimeTaken = 16384,
        ServerPort = 32768,
        UserAgent = 65536,
        Cookie = 131072,
        Referer = 262144,
        ProtocolVersion = 524288,
        Host = 1048576,
        HttpSubStatus = 2097152,
    }

    public enum LogFormat
    {
        IIS = 0,
        NCSA = 1,
        W3C = 2,
        Custom = 3,
    }

    [Flags]
    public enum LogTargetW3C
    {
        None = 0,
        File = 1,
        ETW = 2,
    }

    public enum LogPeriod
    {
        MaxSize = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Hourly = 4,
    }

    public enum CustomLogFieldSourceType
    {
        RequestHeader = 0,
        ResponseHeader = 1,
        ServerVariable = 2,
    }

    public enum LogonMethod
    {
        Interactive = 0,
        Batch = 1,
        Network = 2,
        ClearText = 3,
    }
}
