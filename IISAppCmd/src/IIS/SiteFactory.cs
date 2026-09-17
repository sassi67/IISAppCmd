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

            return new Site
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
        }

        /// <summary>The site answers on localhost only, like the reference tool's.</summary>
        public static string BindingInformationFor(int port) => $"*:{port}:localhost";
    }
}
