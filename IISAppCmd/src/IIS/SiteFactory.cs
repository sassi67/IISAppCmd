using System;
using IISAppCmd.CommandLine;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Fills a <see cref="Site"/> from the command line, binding and rooting it
    /// the way iisexpressstarter does the site it serves.
    /// </summary>
    public static class SiteFactory
    {
        public const string NamePrefix = "Site_";

        public const string DefaultBindingProtocol = "http";

        /// <summary>Path of the root application and of its root virtual directory.</summary>
        public const string RootPath = "/";

        /// <param name="id">Short identifier of the current run; the site is named after it.</param>
        public static Site FromCommandLine(CommandLineOptions options, string id)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("The run id must not be empty.", nameof(id));

            var site = new Site
            {
                Name = NamePrefix + id,
                ServerAutoStart = true,
                Bindings =
                {
                    new SiteBinding
                    {
                        Protocol = DefaultBindingProtocol,
                        BindingInformation = BindingInformationFor(options.Port),
                    },
                },
                Applications =
                {
                    // The application pool is left to the builder, which is given
                    // the pool this run created.
                    new SiteApplication
                    {
                        Path = RootPath,
                        VirtualDirectories =
                        {
                            new SiteVirtualDirectory
                            {
                                Path = RootPath,
                                PhysicalPath = options.ApplicationPath,
                            },
                        },
                    },
                },
            };

            // Each --app is another application of the same site, at its own
            // URL path, with its own root virtual directory at that path.
            foreach (AdditionalApplication application in options.AdditionalApplications)
            {
                site.Applications.Add(new SiteApplication
                {
                    Path = application.Path,
                    ApplicationPool = application.ApplicationPool,
                    VirtualDirectories =
                    {
                        new SiteVirtualDirectory
                        {
                            Path = application.Path,
                            PhysicalPath = application.PhysicalPath,
                        },
                    },
                });
            }

            return site;
        }

        /// <summary>
        /// Attaches every --vd to the application its parentPath names, added
        /// to <paramref name="site"/> by --application or --app. Kept apart
        /// from <see cref="FromCommandLine"/> so a parentPath naming no such
        /// application is a configuration error, the way a rejected build is
        /// elsewhere, rather than failing the site's construction itself.
        /// </summary>
        public static bool TryAttachVirtualDirectories(Site site, CommandLineOptions options, out string error)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (options == null) throw new ArgumentNullException(nameof(options));

            foreach (AdditionalVirtualDirectory directory in options.AdditionalVirtualDirectories)
            {
                SiteApplication parent = site.Applications.Find(application =>
                    string.Equals(application.Path, directory.ParentPath, StringComparison.OrdinalIgnoreCase));

                if (parent == null)
                {
                    error = $"--vd names the application '{directory.ParentPath}', which the site does not have; "
                        + "add it with --application or --app first.";
                    return false;
                }

                parent.VirtualDirectories.Add(new SiteVirtualDirectory
                {
                    Path = directory.Path,
                    PhysicalPath = directory.PhysicalPath,
                });
            }

            error = string.Empty;
            return true;
        }

        /// <summary>The site answers on localhost only, like the reference tool's.</summary>
        public static string BindingInformationFor(int port) => $"*:{port}:localhost";
    }
}
