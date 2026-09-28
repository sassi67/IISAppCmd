using System;
using IISAppCmd.CommandLine;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Fills a <see cref="ServerConfig"/> from the command line, one section
    /// per <c>appcmd set config</c> the options stand in for.
    /// </summary>
    public static class ServerConfigFactory
    {
        /// <summary>
        /// The sections named on the command line, or null when none of
        /// --caching, --requestfiltering and --directorybrowse was given, in
        /// which case the run leaves every server-wide section as it was.
        /// </summary>
        public static ServerConfig FromCommandLine(CommandLineOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var config = new ServerConfig();

            if (options.CachingEnabled != null || options.CachingEnableKernelCache != null)
            {
                config.Caching = new Caching
                {
                    Enabled = options.CachingEnabled,
                    EnableKernelCache = options.CachingEnableKernelCache,
                };
            }

            if (options.RequestFilteringRemoveServerHeader != null)
            {
                config.RequestFiltering = new RequestFiltering
                {
                    RemoveServerHeader = options.RequestFilteringRemoveServerHeader,
                };
            }

            if (options.DirectoryBrowseEnabled != null)
            {
                config.DirectoryBrowse = new DirectoryBrowse
                {
                    Enabled = options.DirectoryBrowseEnabled,
                };
            }

            if (config.Caching == null && config.RequestFiltering == null && config.DirectoryBrowse == null)
            {
                return null;
            }

            return config;
        }
    }
}
