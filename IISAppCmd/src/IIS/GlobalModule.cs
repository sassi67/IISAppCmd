namespace IISAppCmd.IIS
{
    // Mirrors <system.webServer/globalModules/add>; attributes follow IIS_schema.xml.
    public class GlobalModule
    {
        public string Name { get; set; }

        public string Image { get; set; }

        // Comma-separated, e.g. "bitness64,integratedMode".
        public string PreCondition { get; set; }
    }
}
