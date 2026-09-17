using System;
using IISAppCmd.CommandLine;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Fills an <see cref="ApplicationPool"/> from the command line, choosing the
    /// same values iisexpressstarter gives its pool.
    /// </summary>
    public static class ApplicationPoolFactory
    {
        public const string NamePrefix = "AppPool_";

        /// <summary>
        /// IIS Express' aspnet.config, relative to its installation; the same
        /// file the reference tool points its pool at.
        /// </summary>
        public const string ClrConfigFile = @"%IIS_BIN%\config\templates\PersonalWebServer\aspnet.config";

        /// <param name="id">Short identifier of the current run; the pool is named after it.</param>
        public static ApplicationPool FromCommandLine(CommandLineOptions options, string id)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("The run id must not be empty.", nameof(id));

            return new ApplicationPool
            {
                Name = NamePrefix + id,
                ManagedRuntimeVersion = ManagedRuntimeVersionFor(options.Tfm),
                ManagedPipelineMode = ManagedPipelineMode.Integrated,
                Enable32BitAppOnWin64 = options.Bitness == Bitness.X86,
                AutoStart = true,
                CLRConfigFile = ClrConfigFile,
            };
        }

        /// <summary>
        /// Maps a target framework moniker onto the CLR the pool has to load: none
        /// for .NET Core and its successors, v4.0 for .NET Framework 4.x and v2.0
        /// for anything older.
        /// </summary>
        public static string ManagedRuntimeVersionFor(string tfm)
        {
            const string NoManagedCode = "";

            if (string.IsNullOrEmpty(tfm))
            {
                return NoManagedCode;
            }

            // netcoreapp* and netstandard* never load the .NET Framework CLR.
            if (!tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) ||
                tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) ||
                tfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
            {
                return NoManagedCode;
            }

            // Drop the "net" prefix and any platform suffix such as "-windows".
            string version = tfm.Substring(3).Split('-')[0];
            string majorText = version.Split('.')[0];

            if (!int.TryParse(majorText, out int major))
            {
                return NoManagedCode;
            }

            // "net5.0" and later are .NET Core descendants and carry a dotted
            // version; "net48" style monikers are .NET Framework, where only the
            // leading digit is the major version.
            if (version.IndexOf('.') < 0)
            {
                major = majorText[0] - '0';
            }

            return major >= 5 ? NoManagedCode
                : major >= 4 ? "v4.0"
                : "v2.0";
        }
    }
}
