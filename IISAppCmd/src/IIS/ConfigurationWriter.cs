using System;
using Microsoft.Web.Administration;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// The handful of conversions every builder needs when it hands a value of
    /// the model over to the IIS configuration system.
    /// </summary>
    internal static class ConfigurationWriter
    {
        /// <summary>
        /// Writes one attribute, converting the model's CLR type to what the IIS
        /// configuration system accepts. Null strings are left unset, and an
        /// attribute the installed IIS schema does not define is skipped rather
        /// than failing the whole element on an older Windows.
        /// </summary>
        public static void Set(ConfigurationElement element, string name, object value)
        {
            if (value == null) return;
            if (element.Schema.AttributeSchemas[name] == null) return;

            if (value is Enum)
            {
                value = Convert.ToInt32(value);
            }
            else if (value is uint unsigned)
            {
                value = (long)unsigned;
            }

            element[name] = value;
        }

        /// <summary>
        /// The child element of <paramref name="element"/>, or null when the
        /// installed IIS schema does not define it, which is how an element
        /// added by a later Windows is skipped instead of throwing.
        /// </summary>
        public static ConfigurationElement Child(ConfigurationElement element, string name)
        {
            // An element the schema gives no children at all carries no
            // collection of child element schemas, not an empty one.
            ConfigurationElementSchemaCollection children = element.Schema?.ChildElementSchemas;

            if (children == null)
            {
                return null;
            }

            foreach (ConfigurationElementSchema schema in children)
            {
                if (string.Equals(schema.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return element.GetChildElement(name);
                }
            }

            return null;
        }
    }
}
