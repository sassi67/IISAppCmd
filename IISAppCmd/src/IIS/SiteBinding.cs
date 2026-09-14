namespace IISAppCmd.IIS
{
    public class SiteBinding
    {
        public string Protocol { get; set; }

        public string BindingInformation { get; set; }

        public uint SslFlags { get; set; }
    }
}
