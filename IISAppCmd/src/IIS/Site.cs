using System.Collections.Generic;

namespace IISAppCmd.IIS
{
    // Mirrors <system.applicationHost/sites/site>; defaults follow IIS_schema.xml.
    public class Site
    {
        public string Name { get; set; }

        public uint Id { get; set; }

        public bool ServerAutoStart { get; set; } = true;

        public List<SiteBinding> Bindings { get; set; } = new List<SiteBinding>();

        public SiteLimits Limits { get; set; } = new SiteLimits();

        public SiteLogFile LogFile { get; set; } = new SiteLogFile();

        public SiteTraceFailedRequestsLogging TraceFailedRequestsLogging { get; set; } = new SiteTraceFailedRequestsLogging();

        public SiteHsts Hsts { get; set; } = new SiteHsts();

        public SiteApplicationDefaults ApplicationDefaults { get; set; } = new SiteApplicationDefaults();

        public SiteVirtualDirectory VirtualDirectoryDefaults { get; set; } = new SiteVirtualDirectory();

        public List<SiteApplication> Applications { get; set; } = new List<SiteApplication>();
    }
}
