namespace IISAppCmd
{
    // Same numbering as iisexpressstarter so scripts can treat both tools alike;
    // 2 (IIS Express not found) and 4 (start failed) have no meaning here.
    public enum ExitCode
    {
        Success = 0,
        UsageError = 1,
        ConfigurationError = 3,
    }
}
