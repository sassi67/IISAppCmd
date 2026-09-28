namespace IISAppCmd.IIS
{
    // Mirrors <system.webServer/caching>; attributes follow IIS_schema.xml.
    public class Caching
    {
        public bool? Enabled { get; set; }

        public bool? EnableKernelCache { get; set; }
    }
}
