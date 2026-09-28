namespace IISAppCmd.IIS
{
    // Mirrors <system.webServer/security/requestFiltering>; attributes follow IIS_schema.xml.
    public class RequestFiltering
    {
        // Only IIS 10 and later know it.
        public bool? RemoveServerHeader { get; set; }
    }
}
