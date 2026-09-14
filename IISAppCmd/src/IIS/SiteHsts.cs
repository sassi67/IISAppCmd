namespace IISAppCmd.IIS
{
    public class SiteHsts
    {
        public bool Enabled { get; set; }

        public uint MaxAge { get; set; }

        public bool IncludeSubDomains { get; set; }

        public bool Preload { get; set; }

        public bool RedirectHttpToHttps { get; set; }
    }
}
