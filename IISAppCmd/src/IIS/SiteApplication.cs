using System.Collections.Generic;

namespace IISAppCmd.IIS
{
    public class SiteApplication
    {
        public string Path { get; set; }

        public string ApplicationPool { get; set; }

        public string EnabledProtocols { get; set; } = "http";

        public bool ServiceAutoStartEnabled { get; set; }

        public string ServiceAutoStartProvider { get; set; }

        public bool PreloadEnabled { get; set; }

        public SiteVirtualDirectory VirtualDirectoryDefaults { get; set; } = new SiteVirtualDirectory();

        public List<SiteVirtualDirectory> VirtualDirectories { get; set; } = new List<SiteVirtualDirectory>();
    }

    public class SiteApplicationDefaults
    {
        public string Path { get; set; }

        public string ApplicationPool { get; set; }

        public string EnabledProtocols { get; set; } = "http";

        public bool ServiceAutoStartEnabled { get; set; }

        public string ServiceAutoStartProvider { get; set; }

        public bool PreloadEnabled { get; set; }
    }
}
