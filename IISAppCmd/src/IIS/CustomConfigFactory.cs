using System;
using IISAppCmd.CommandLine;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Fills a <see cref="CustomConfig"/> from the command line, the way
    /// iisexpressstarter fills the custom section
    /// Resources\IISAgentConfigSchema.xml defines for a module.
    /// </summary>
    public static class CustomConfigFactory
    {
        /// <summary>
        /// The custom section named on the command line, or null when
        /// --customconfig was not given, in which case the module is written
        /// without one.
        /// </summary>
        /// <param name="id">Short identifier of the current run; it names the pool the section applies to.</param>
        public static CustomConfig FromCommandLine(CommandLineOptions options, string id)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("The run id must not be empty.", nameof(id));

            if (string.IsNullOrEmpty(options.CustomConfigOptions))
            {
                return null;
            }

            return new CustomConfig
            {
                // appPool is the key of the section, and the pool this run
                // creates is the one it applies to unless the command line
                // named another.
                AppPool = options.CustomConfigAppPool ?? ApplicationPoolFactory.NamePrefix + id,
                Options = options.CustomConfigOptions,
            };
        }
    }
}
