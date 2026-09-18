using System;
using System.IO;
using System.Runtime.InteropServices;
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
    /// </summary>
    public sealed class GlobalModuleBuilder
    {
        /// <summary>Where a native module is declared, with its image.</summary>
        public const string GlobalModulesSection = "system.webServer/globalModules";

        /// <summary>Where a declared module is enabled, by name.</summary>
        public const string ModulesSection = "system.webServer/modules";

        private readonly GlobalModule _module;
        private readonly ServerManager _manager;

        /// <param name="module">The global module to write.</param>
        /// <param name="manager">Opened on the working copy at <paramref name="configPath"/>.</param>
        /// <param name="configPath">The working copy of applicationHost.config being edited.</param>
        public GlobalModuleBuilder(GlobalModule module, ServerManager manager, string configPath)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("The configuration path must not be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("The configuration file does not exist.", configPath);

            _module = module;
            _manager = manager;
            ConfigPath = configPath;
        }

        public string ConfigPath { get; }

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
    }
}
