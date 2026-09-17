using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.Administration;
using IisSite = Microsoft.Web.Administration.Site;
using static IISAppCmd.IIS.ConfigurationWriter;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Writes a <see cref="Site"/> into the applicationHost.config a
    /// <see cref="ServerManager"/> was opened on. Every value the model holds is
    /// written explicitly, so the site never inherits from siteDefaults, and
    /// every application that names no pool runs in the one this run created.
    /// Changes are left uncommitted so the caller can batch them with the rest
    /// of its edits.
    /// </summary>
    public sealed class SiteBuilder
    {
        private readonly Site _site;
        private readonly ServerManager _manager;
        private readonly ApplicationPool _applicationPool;

        /// <param name="site">The site to write.</param>
        /// <param name="manager">Opened on the working copy at <paramref name="configPath"/>.</param>
        /// <param name="applicationPool">The pool this run created; applications that name none run in it.</param>
        /// <param name="configPath">The working copy of applicationHost.config being edited.</param>
        public SiteBuilder(Site site, ServerManager manager, ApplicationPool applicationPool, string configPath)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (applicationPool == null) throw new ArgumentNullException(nameof(applicationPool));
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("The configuration path must not be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("The configuration file does not exist.", configPath);

            _site = site;
            _manager = manager;
            _applicationPool = applicationPool;
            ConfigPath = configPath;
        }

        public string ConfigPath { get; }

        /// <summary>
        /// Adds the site to the sites collection, or updates it when a site of
        /// that name already exists, without committing.
        /// </summary>
        public bool Build(out string error)
        {
            if (!Validate(out error))
            {
                return false;
            }

            try
            {
                // Re-running against the same file should not fail on a duplicate.
                IisSite target = _manager.Sites[_site.Name] ?? Add();

                WriteSite(target);
                WriteBindings(target);
                WriteLimits(target);
                WriteLogFile(target);
                WriteTraceFailedRequestsLogging(target);
                WriteHsts(target);
                WriteApplicationDefaults(target);
                WriteVirtualDirectoryDefaults(target, _site.VirtualDirectoryDefaults);
                WriteApplications(target);
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidOperationException || ex is ArgumentException || ex is FileNotFoundException)
            {
                error = $"could not add the site '{_site.Name}' to '{ConfigPath}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks everything IIS requires before anything is written, so a
        /// rejected site leaves the configuration untouched and the message
        /// names the culprit.
        /// </summary>
        private bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_site.Name))
            {
                error = $"the site has no name; nothing was written to '{ConfigPath}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_applicationPool.Name))
            {
                error = $"the application pool of the site '{_site.Name}' has no name.";
                return false;
            }

            if (_site.Bindings == null || _site.Bindings.Count == 0)
            {
                error = $"the site '{_site.Name}' has no binding.";
                return false;
            }

            for (int i = 0; i < _site.Bindings.Count; i++)
            {
                SiteBinding binding = _site.Bindings[i];

                if (binding == null || string.IsNullOrEmpty(binding.Protocol))
                {
                    error = $"binding #{i + 1} of the site '{_site.Name}' has no protocol.";
                    return false;
                }

                if (string.IsNullOrEmpty(binding.BindingInformation))
                {
                    error = $"binding #{i + 1} of the site '{_site.Name}' has no binding information.";
                    return false;
                }
            }

            if (_site.Applications == null || _site.Applications.Count == 0)
            {
                error = $"the site '{_site.Name}' has no application.";
                return false;
            }

            for (int i = 0; i < _site.Applications.Count; i++)
            {
                SiteApplication application = _site.Applications[i];

                if (application == null || string.IsNullOrEmpty(application.Path))
                {
                    error = $"application #{i + 1} of the site '{_site.Name}' has no path.";
                    return false;
                }

                if (application.VirtualDirectories == null || application.VirtualDirectories.Count == 0)
                {
                    error = $"the application '{application.Path}' of the site '{_site.Name}' has no virtual directory.";
                    return false;
                }

                foreach (SiteVirtualDirectory directory in application.VirtualDirectories)
                {
                    if (directory == null || string.IsNullOrEmpty(directory.Path))
                    {
                        error = $"a virtual directory of the application '{application.Path}' of the site '{_site.Name}' has no path.";
                        return false;
                    }

                    if (string.IsNullOrEmpty(directory.PhysicalPath))
                    {
                        error = $"the virtual directory '{directory.Path}' of the application '{application.Path}' " +
                            $"of the site '{_site.Name}' has no physical path.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Creates the site from its first binding and its root physical path;
        /// everything else is written over it afterwards. IIS picks the site id.
        /// </summary>
        private IisSite Add()
        {
            SiteBinding binding = _site.Bindings[0];
            SiteVirtualDirectory root = _site.Applications[0].VirtualDirectories[0];

            return _manager.Sites.Add(_site.Name, binding.Protocol, binding.BindingInformation, root.PhysicalPath);
        }

        private void WriteSite(IisSite target)
        {
            // Zero is no site id, so the one IIS picked when the site was added
            // is kept.
            if (_site.Id != 0)
            {
                Set(target, "id", _site.Id);
            }

            Set(target, "serverAutoStart", _site.ServerAutoStart);
        }

        private void WriteBindings(IisSite target)
        {
            ConfigurationElementCollection bindings = target.GetCollection("bindings");

            // Clearing an empty collection would write a pointless <clear />.
            if (bindings.Count > 0)
            {
                bindings.Clear();
            }

            foreach (SiteBinding binding in _site.Bindings)
            {
                ConfigurationElement entry = bindings.CreateElement("binding");
                entry["protocol"] = binding.Protocol;
                entry["bindingInformation"] = binding.BindingInformation;
                Set(entry, "sslFlags", binding.SslFlags);
                bindings.Add(entry);
            }
        }

        private void WriteLimits(IisSite target)
        {
            SiteLimits limits = _site.Limits;
            ConfigurationElement element = Child(target, "limits");
            if (limits == null || element == null) return;

            Set(element, "maxBandwidth", limits.MaxBandwidth);
            Set(element, "maxConnections", limits.MaxConnections);
            Set(element, "connectionTimeout", limits.ConnectionTimeout);
            Set(element, "maxUrlSegments", limits.MaxUrlSegments);
        }

        private void WriteLogFile(IisSite target)
        {
            SiteLogFile logFile = _site.LogFile;
            ConfigurationElement element = Child(target, "logFile");
            if (logFile == null || element == null) return;

            Set(element, "logExtFileFlags", logFile.LogExtFileFlags);
            Set(element, "customLogPluginClsid", logFile.CustomLogPluginClsid);
            Set(element, "logFormat", logFile.LogFormat);
            Set(element, "logTargetW3C", logFile.LogTargetW3C);
            Set(element, "directory", logFile.Directory);
            Set(element, "period", logFile.Period);
            Set(element, "truncateSize", logFile.TruncateSize);
            Set(element, "localTimeRollover", logFile.LocalTimeRollover);
            Set(element, "enabled", logFile.Enabled);
            Set(element, "logSiteId", logFile.LogSiteId);
            Set(element, "flushByEntryCountW3CLog", logFile.FlushByEntryCountW3CLog);
            Set(element, "maxLogLineLength", logFile.MaxLogLineLength);

            SiteLogCustomFields customFields = logFile.CustomFields;
            ConfigurationElement fields = Child(element, "customFields");
            if (customFields == null || fields == null) return;

            Set(fields, "maxCustomFieldLength", customFields.MaxCustomFieldLength);

            if (customFields.Fields == null || customFields.Fields.Count == 0) return;

            ConfigurationElementCollection collection = fields.GetCollection();

            if (collection.Count > 0)
            {
                collection.Clear();
            }

            foreach (SiteLogCustomField field in customFields.Fields)
            {
                ConfigurationElement entry = collection.CreateElement("add");
                entry["logFieldName"] = field.LogFieldName;
                entry["sourceName"] = field.SourceName;
                Set(entry, "sourceType", field.SourceType);
                collection.Add(entry);
            }
        }

        private void WriteTraceFailedRequestsLogging(IisSite target)
        {
            SiteTraceFailedRequestsLogging tracing = _site.TraceFailedRequestsLogging;
            ConfigurationElement element = Child(target, "traceFailedRequestsLogging");
            if (tracing == null || element == null) return;

            Set(element, "enabled", tracing.Enabled);
            Set(element, "directory", tracing.Directory);
            Set(element, "maxLogFiles", tracing.MaxLogFiles);
            Set(element, "maxLogFileSizeKB", tracing.MaxLogFileSizeKB);
            Set(element, "customActionsEnabled", tracing.CustomActionsEnabled);
        }

        private void WriteHsts(IisSite target)
        {
            SiteHsts hsts = _site.Hsts;

            // hsts only exists from Windows 10 1709 on; on an older schema the
            // element is simply absent.
            ConfigurationElement element = Child(target, "hsts");
            if (hsts == null || element == null) return;

            Set(element, "enabled", hsts.Enabled);
            Set(element, "max-age", hsts.MaxAge);
            Set(element, "includeSubDomains", hsts.IncludeSubDomains);
            Set(element, "preload", hsts.Preload);
            Set(element, "redirectHttpToHttps", hsts.RedirectHttpToHttps);
        }

        private void WriteApplicationDefaults(IisSite target)
        {
            SiteApplicationDefaults defaults = _site.ApplicationDefaults;
            ConfigurationElement element = Child(target, "applicationDefaults");
            if (defaults == null || element == null) return;

            Set(element, "path", defaults.Path);
            Set(element, "applicationPool", defaults.ApplicationPool);
            Set(element, "enabledProtocols", defaults.EnabledProtocols);
            Set(element, "serviceAutoStartEnabled", defaults.ServiceAutoStartEnabled);
            Set(element, "serviceAutoStartProvider", defaults.ServiceAutoStartProvider);
            Set(element, "preloadEnabled", defaults.PreloadEnabled);
        }

        private void WriteVirtualDirectoryDefaults(ConfigurationElement parent, SiteVirtualDirectory defaults)
        {
            ConfigurationElement element = Child(parent, "virtualDirectoryDefaults");
            if (defaults == null || element == null) return;

            WriteVirtualDirectory(element, defaults);
        }

        private void WriteApplications(IisSite target)
        {
            // The applications are the site's default collection.
            ConfigurationElementCollection applications = target.GetCollection();

            foreach (SiteApplication application in _site.Applications)
            {
                ConfigurationElement entry = Find(applications, "path", application.Path);

                if (entry == null)
                {
                    entry = applications.CreateElement("application");
                    entry["path"] = application.Path;
                    applications.Add(entry);
                }

                WriteApplication(entry, application);
            }
        }

        private void WriteApplication(ConfigurationElement target, SiteApplication application)
        {
            Set(target, "applicationPool", application.ApplicationPool ?? _applicationPool.Name);
            Set(target, "enabledProtocols", application.EnabledProtocols);
            Set(target, "serviceAutoStartEnabled", application.ServiceAutoStartEnabled);
            Set(target, "serviceAutoStartProvider", application.ServiceAutoStartProvider);
            Set(target, "preloadEnabled", application.PreloadEnabled);

            WriteVirtualDirectoryDefaults(target, application.VirtualDirectoryDefaults);

            // The virtual directories are the application's default collection.
            ConfigurationElementCollection directories = target.GetCollection();

            foreach (SiteVirtualDirectory directory in application.VirtualDirectories)
            {
                ConfigurationElement entry = Find(directories, "path", directory.Path);

                if (entry == null)
                {
                    entry = directories.CreateElement("virtualDirectory");
                    entry["path"] = directory.Path;
                    directories.Add(entry);
                }

                WriteVirtualDirectory(entry, directory);
            }
        }

        private static void WriteVirtualDirectory(ConfigurationElement target, SiteVirtualDirectory directory)
        {
            Set(target, "physicalPath", directory.PhysicalPath);
            Set(target, "userName", directory.UserName);
            Set(target, "password", directory.Password);
            Set(target, "logonMethod", directory.LogonMethod);
            Set(target, "allowSubDirConfig", directory.AllowSubDirConfig);
        }

        /// <summary>The element of <paramref name="collection"/> whose <paramref name="key"/> matches, or null.</summary>
        private static ConfigurationElement Find(ConfigurationElementCollection collection, string key, string value)
        {
            foreach (ConfigurationElement element in collection)
            {
                if (string.Equals(element[key] as string, value, StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            return null;
        }
    }
}
