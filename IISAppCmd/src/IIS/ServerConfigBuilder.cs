using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.Administration;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Writes a <see cref="ServerConfig"/> into the applicationHost.config a
    /// <see cref="ServerManager"/> was opened on, the equivalent of
    /// <c>appcmd set config /section:... /attribute:value</c> run without a
    /// path: every section is set at the root of the file, so it applies to all
    /// sites. Changes are left uncommitted so the caller can batch them with
    /// the rest of its edits.
    /// </summary>
    public sealed class ServerConfigBuilder
    {
        public const string CachingSection = "system.webServer/caching";

        public const string RequestFilteringSection = "system.webServer/security/requestFiltering";

        public const string DirectoryBrowseSection = "system.webServer/directoryBrowse";

        private readonly ServerConfig _config;
        private readonly ServerManager _manager;

        /// <param name="config">The sections to write.</param>
        /// <param name="manager">Opened on the working copy at <paramref name="configPath"/>.</param>
        /// <param name="configPath">The working copy of applicationHost.config being edited.</param>
        public ServerConfigBuilder(ServerConfig config, ServerManager manager, string configPath)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("The configuration path must not be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("The configuration file does not exist.", configPath);

            _config = config;
            _manager = manager;
            ConfigPath = configPath;
        }

        public string ConfigPath { get; }

        /// <summary>
        /// Sets every attribute the model holds a value for, and only those,
        /// without committing.
        /// </summary>
        public bool Build(out string error)
        {
            try
            {
                Configuration configuration = _manager.GetApplicationHostConfiguration();

                if (_config.Caching != null)
                {
                    ConfigurationSection section = configuration.GetSection(CachingSection);

                    if (!Set(section, CachingSection, "enabled", _config.Caching.Enabled, out error) ||
                        !Set(section, CachingSection, "enableKernelCache", _config.Caching.EnableKernelCache, out error))
                    {
                        return false;
                    }
                }

                if (_config.RequestFiltering != null)
                {
                    ConfigurationSection section = configuration.GetSection(RequestFilteringSection);

                    if (!Set(section, RequestFilteringSection, "removeServerHeader", _config.RequestFiltering.RemoveServerHeader, out error))
                    {
                        return false;
                    }
                }

                if (_config.DirectoryBrowse != null)
                {
                    ConfigurationSection section = configuration.GetSection(DirectoryBrowseSection);

                    if (!Set(section, DirectoryBrowseSection, "enabled", _config.DirectoryBrowse.Enabled, out error))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidOperationException || ex is ArgumentException || ex is FileNotFoundException)
            {
                error = $"could not set the server configuration of '{ConfigPath}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Writes one attribute when the model holds a value for it. Unlike the
        /// attributes of a pool or a site, which the tool fills in itself, these
        /// were asked for one by one, so an attribute the installed IIS schema
        /// does not know, such as removeServerHeader before IIS 10, fails the
        /// run instead of being skipped.
        /// </summary>
        private bool Set(ConfigurationSection section, string sectionPath, string name, bool? value, out string error)
        {
            if (value != null)
            {
                if (section.Schema.AttributeSchemas[name] == null)
                {
                    error = $"the installed IIS schema has no attribute '{name}' in '{sectionPath}', "
                        + $"so it cannot be set in '{ConfigPath}'.";
                    return false;
                }

                section[name] = value.Value;
            }

            error = string.Empty;
            return true;
        }
    }
}
