namespace IISAppCmd.IIS
{
    // Mirrors <system.webServer/modules/add/dynatrace/config>; attributes follow IISAgentConfigSchema.xml.
    public class CustomConfig
    {
        public string AppPool { get; set; }

        // Comma-separated key=value pairs, e.g. "tenant=abc,loglevelcon=info".
        public string Options { get; set; }
    }
}
