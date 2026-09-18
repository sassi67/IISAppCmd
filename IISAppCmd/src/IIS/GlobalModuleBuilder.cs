using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Web.Administration;
using static IISAppCmd.IIS.ConfigurationWriter;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Writes a <see cref="GlobalModule"/> into the applicationHost.config a
    /// <see cref="ServerManager"/> was opened on. The module is declared in
    /// <c>system.webServer/globalModules</c> and enabled in
    /// <c>system.webServer/modules</c>, which is what makes IIS load it; a
    /// module declared but not enabled would never run. Changes are left
    /// uncommitted so the caller can batch them with the rest of its edits.
    /// <para>
    /// The <see cref="CustomConfig"/> the module carries goes the same way when
    /// the machine knows Resources\IISAgentConfigSchema.xml. When it does not,
    /// <see cref="Build"/> leaves the section to <see cref="BuildCustomConfig"/>,
    /// which writes it as XML once those changes have been committed.
    /// </para>
    /// </summary>
    public sealed class GlobalModuleBuilder
    {
        /// <summary>Where a native module is declared, with its image.</summary>
        public const string GlobalModulesSection = "system.webServer/globalModules";

        /// <summary>Where a declared module is enabled, by name.</summary>
        public const string ModulesSection = "system.webServer/modules";

        /// <summary>The element Resources\IISAgentConfigSchema.xml adds to a module.</summary>
        public const string CustomSectionElement = "dynatrace";

        /// <summary>The entries that element holds, one per application pool.</summary>
        public const string CustomSectionEntry = "config";

        private const string IndentStep = "    ";

        private readonly GlobalModule _module;
        private readonly ServerManager _manager;
        private readonly CustomConfig _customConfig;

        /// <param name="module">The global module to write.</param>
        /// <param name="manager">Opened on the working copy at <paramref name="configPath"/>.</param>
        /// <param name="customConfig">The custom section the module carries, or null when it carries none.</param>
        /// <param name="configPath">The working copy of applicationHost.config being edited.</param>
        public GlobalModuleBuilder(GlobalModule module, ServerManager manager, CustomConfig customConfig, string configPath)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("The configuration path must not be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("The configuration file does not exist.", configPath);

            _module = module;
            _manager = manager;
            _customConfig = customConfig;
            ConfigPath = configPath;
        }

        public string ConfigPath { get; }

        /// <summary>
        /// True once <see cref="Build"/> has written the custom section through
        /// the <see cref="ServerManager"/>, which it does when the IIS
        /// configuration system knows the schema of that section. It then
        /// travels with everything else to the caller's commit, and
        /// <see cref="BuildCustomConfig"/> has nothing left to do.
        /// </summary>
        public bool CustomConfigWritten { get; private set; }

        /// <summary>
        /// Adds the module to the globalModules and modules collections, or
        /// updates both entries when a module of that name is already there,
        /// without committing.
        /// </summary>
        public bool Build(out string error)
        {
            if (!Validate(out error))
            {
                return false;
            }

            try
            {
                Configuration configuration = _manager.GetApplicationHostConfiguration();

                WriteGlobalModule(configuration);
                WriteModule(configuration);
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidOperationException || ex is ArgumentException || ex is FileNotFoundException)
            {
                error = $"could not add the global module '{_module.Name}' to '{ConfigPath}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Writes the custom section into the entry the module has in
        /// &lt;modules&gt;, leaving the rest of the file as it was. Does nothing
        /// when the module carries no custom section, or when
        /// <see cref="Build"/> has already written it through the
        /// <see cref="ServerManager"/>.
        /// <para>
        /// This is the way out for a machine where the custom schema is not
        /// installed: the IIS configuration system refuses both to write and to
        /// read an element it has no schema for, so the section is written as
        /// XML instead, and only once the caller has committed. Nothing may open
        /// the file through a <see cref="ServerManager"/> afterwards, since that
        /// would be reading a file its schema cannot parse.
        /// </para>
        /// </summary>
        public bool BuildCustomConfig(out string error)
        {
            if (_customConfig == null || CustomConfigWritten)
            {
                error = string.Empty;
                return true;
            }

            if (!ValidateCustomConfig(out error))
            {
                return false;
            }

            try
            {
                XDocument document = XDocument.Load(ConfigPath, LoadOptions.PreserveWhitespace);
                XElement entry = FindModuleEntry(document);

                if (entry == null)
                {
                    error = $"the global module '{_module.Name}' is not in <modules> of '{ConfigPath}'; "
                        + "the custom section goes into the entry the module has there.";
                    return false;
                }

                WriteCustomConfig(entry);
                Save(document);
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException)
            {
                error = $"could not write the custom section of the global module '{_module.Name}' "
                    + $"to '{ConfigPath}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks what IIS requires of a native module before anything is
        /// written, so a rejected module leaves the configuration untouched and
        /// the message names the culprit.
        /// </summary>
        private bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_module.Name))
            {
                error = $"the global module has no name; nothing was written to '{ConfigPath}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_module.Image))
            {
                error = $"the global module '{_module.Name}' has no image.";
                return false;
            }

            // Checked here as well, so a custom section that cannot be written
            // stops the run before the module itself is.
            return ValidateCustomConfig(out error);
        }

        private bool ValidateCustomConfig(out string error)
        {
            if (_customConfig != null)
            {
                if (string.IsNullOrWhiteSpace(_customConfig.AppPool))
                {
                    error = $"the custom section of the global module '{_module.Name}' has no appPool.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_customConfig.Options))
                {
                    error = $"the custom section of the global module '{_module.Name}' has no options.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Declares the module and its image in &lt;globalModules&gt;.</summary>
        private void WriteGlobalModule(Configuration configuration)
        {
            ConfigurationElementCollection modules = configuration.GetSection(GlobalModulesSection).GetCollection();
            ConfigurationElement entry = Find(modules, _module.Name) ?? Add(modules);

            Set(entry, "image", _module.Image);
            Set(entry, "preCondition", _module.PreCondition);
        }

        /// <summary>
        /// Enables the declared module in &lt;modules&gt;. A native module is
        /// referred to by name there, so only the preCondition travels with it.
        /// </summary>
        private void WriteModule(Configuration configuration)
        {
            ConfigurationElementCollection modules = configuration.GetSection(ModulesSection).GetCollection();
            ConfigurationElement entry = Find(modules, _module.Name) ?? Add(modules);

            Set(entry, "preCondition", _module.PreCondition);

            CustomConfigWritten = TryWriteCustomConfig(entry);
        }

        /// <summary>
        /// Writes the custom section into the entry of the module through the
        /// IIS configuration system, which only knows it once
        /// IISAgentConfigSchema.xml has been installed into
        /// %windir%\system32\inetsrv\config\schema, the one schema directory
        /// Microsoft.Web.Administration reads. Returns false when it does not,
        /// leaving the section to <see cref="BuildCustomConfig"/>.
        /// </summary>
        private bool TryWriteCustomConfig(ConfigurationElement entry)
        {
            if (_customConfig == null)
            {
                return false;
            }

            ConfigurationElement section = Child(entry, CustomSectionElement);

            if (section == null)
            {
                return false;
            }

            ConfigurationElementCollection entries = section.GetCollection();

            // appPool is the key of the collection, so a section for that pool is
            // updated rather than written twice.
            ConfigurationElement config = FindByAppPool(entries);

            if (config == null)
            {
                config = entries.CreateElement(CustomSectionEntry);
                config["appPool"] = _customConfig.AppPool;
                entries.Add(config);
            }

            config["options"] = _customConfig.Options;
            return true;
        }

        /// <summary>The entry of the custom section for that application pool, or null.</summary>
        private ConfigurationElement FindByAppPool(ConfigurationElementCollection collection)
        {
            foreach (ConfigurationElement element in collection)
            {
                if (!string.Equals(element.ElementTagName, CustomSectionEntry, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(element["appPool"] as string, _customConfig.AppPool, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>Appends an entry for the module, named after it.</summary>
        private ConfigurationElement Add(ConfigurationElementCollection collection)
        {
            ConfigurationElement entry = collection.CreateElement("add");
            entry["name"] = _module.Name;
            collection.Add(entry);

            return entry;
        }

        /// <summary>
        /// The entry of <paramref name="collection"/> carrying that name, or
        /// null; re-running against the same file updates it instead of adding a
        /// duplicate, which IIS would refuse.
        /// </summary>
        private static ConfigurationElement Find(ConfigurationElementCollection collection, string name)
        {
            foreach (ConfigurationElement element in collection)
            {
                // A collection may also hold <clear /> and <remove />, which
                // carry no name of their own.
                if (!string.Equals(element.ElementTagName, "add", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(element["name"] as string, name, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>The &lt;add&gt; of &lt;modules&gt; that enables the module, or null.</summary>
        private XElement FindModuleEntry(XDocument document) =>
            document.Root?
                .Element("system.webServer")?
                .Element("modules")?
                .Elements("add")
                .FirstOrDefault(e => string.Equals((string)e.Attribute("name"), _module.Name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Writes &lt;dynatrace&gt;&lt;config appPool options /&gt;&lt;/dynatrace&gt;
        /// into the entry, keeping the one entry per application pool the schema
        /// asks for.
        /// </summary>
        private void WriteCustomConfig(XElement entry)
        {
            XElement section = entry.Elements(CustomSectionElement).FirstOrDefault()
                ?? AddChild(entry, CustomSectionElement);

            // appPool is the key of the collection, so a section for that pool is
            // updated rather than written twice.
            XElement config = section.Elements(CustomSectionEntry)
                .FirstOrDefault(e => string.Equals((string)e.Attribute("appPool"), _customConfig.AppPool, StringComparison.OrdinalIgnoreCase))
                ?? AddChild(section, CustomSectionEntry);

            config.SetAttributeValue("appPool", _customConfig.AppPool);
            config.SetAttributeValue("options", _customConfig.Options);
        }

        /// <summary>
        /// Appends a child, indented one step deeper than its parent, so the
        /// section reads like the rest of the file.
        /// </summary>
        private static XElement AddChild(XElement parent, string name)
        {
            string indent = IndentOf(parent);
            var child = new XElement(name);

            // An element written by the IIS configuration system holds nothing
            // yet, so its closing tag has to be put on a line of its own.
            if (parent.LastNode is XText trailing && trailing.Value.Trim().Length == 0)
            {
                trailing.AddBeforeSelf(new XText(indent + IndentStep));
                trailing.AddBeforeSelf(child);
            }
            else
            {
                parent.Add(new XText(indent + IndentStep), child, new XText(indent));
            }

            return child;
        }

        /// <summary>
        /// The line break and indentation the element sits on, taken from the
        /// whitespace in front of it.
        /// </summary>
        private static string IndentOf(XElement element)
        {
            string whitespace = (element.PreviousNode as XText)?.Value;
            int lineBreak = whitespace?.LastIndexOf('\n') ?? -1;

            if (lineBreak < 0)
            {
                return Environment.NewLine;
            }

            if (lineBreak > 0 && whitespace[lineBreak - 1] == '\r')
            {
                lineBreak--;
            }

            return whitespace.Substring(lineBreak);
        }

        /// <summary>
        /// Writes the document back as it was read, so the only difference to
        /// the file the IIS configuration system committed is the custom
        /// section: the declaration keeps its own spelling, the whitespace of
        /// every other element is preserved, and no byte order mark is added.
        /// </summary>
        private void Save(XDocument document)
        {
            string text = document.Declaration?.ToString() + document.ToString(SaveOptions.DisableFormatting);

            File.WriteAllText(ConfigPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}
