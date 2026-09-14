namespace IISAppCmd.IIS
{
    public class SiteVirtualDirectory
    {
        public string Path { get; set; }

        public string PhysicalPath { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public LogonMethod LogonMethod { get; set; } = LogonMethod.ClearText;

        public bool AllowSubDirConfig { get; set; } = true;
    }
}
