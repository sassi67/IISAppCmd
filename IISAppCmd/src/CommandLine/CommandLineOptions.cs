using System.Collections.Generic;

namespace IISAppCmd.CommandLine
{
    /// <summary>Process bitness requested on the command line.</summary>
    public enum Bitness
    {
        X86 = 32,
        X64 = 64,
    }

    /// <summary>One entry of --app: an additional application beyond --application.</summary>
    public sealed class AdditionalApplication
    {
        public string Path { get; set; }

        public string PhysicalPath { get; set; }

        /// <summary>Null when the value named none, in which case the pool this run creates is the one it runs in.</summary>
        public string ApplicationPool { get; set; }
    }

    /// <summary>One entry of --vd: an additional virtual directory of an application added by --application or --app.</summary>
    public sealed class AdditionalVirtualDirectory
    {
        /// <summary>Path of the application it is added to.</summary>
        public string ParentPath { get; set; }

        public string Path { get; set; }

        public string PhysicalPath { get; set; }
    }

    /// <summary>The validated set of options the tool was invoked with.</summary>
    public sealed class CommandLineOptions
    {
        /// <summary>Used when --bitness is not given; the only definition of it.</summary>
        public const Bitness DefaultBitness = Bitness.X64;

        /// <summary>Used when --tfm is not given; the only definition of it.</summary>
        public const string DefaultTfm = "netcoreapp3.1";

        /// <summary>Used when --port is not given; the only definition of it.</summary>
        public const int DefaultPort = 5001;

        public Bitness Bitness { get; set; } = DefaultBitness;

        /// <summary>
        /// Framework the application targets, lower-cased. Decides the
        /// application pool's managed runtime version.
        /// </summary>
        public string Tfm { get; set; } = DefaultTfm;

        /// <summary>Name of the application the site serves, taken from --application.</summary>
        public string Application { get; set; }

        /// <summary>
        /// Physical path the site serves, taken from --application and made
        /// absolute.
        /// </summary>
        public string ApplicationPath { get; set; }

        /// <summary>
        /// Additional applications beyond the required one, taken from --app;
        /// empty when the option was not given.
        /// </summary>
        public List<AdditionalApplication> AdditionalApplications { get; set; } = new List<AdditionalApplication>();

        /// <summary>
        /// Additional virtual directories to attach to an application added by
        /// --application or --app, taken from --vd; empty when the option was
        /// not given.
        /// </summary>
        public List<AdditionalVirtualDirectory> AdditionalVirtualDirectories { get; set; } = new List<AdditionalVirtualDirectory>();

        /// <summary>Port the site binds to.</summary>
        public int Port { get; set; } = DefaultPort;

        /// <summary>
        /// Name of the native module to register, taken from --globalmodule.
        /// Null when the option was not given, in which case the run registers
        /// no module.
        /// </summary>
        public string GlobalModule { get; set; }

        /// <summary>
        /// Path of the DLL the global module lives in, taken from
        /// --globalmodule. Left as written, so it may still carry environment
        /// variables such as %windir%.
        /// </summary>
        public string GlobalModuleImage { get; set; }

        /// <summary>
        /// Condition under which the global module is loaded, taken from
        /// --globalmodule. Null when the value carried none, in which case the
        /// bitness of the run decides it.
        /// </summary>
        public string GlobalModulePreCondition { get; set; }

        /// <summary>
        /// Settings of the custom section the global module carries, taken from
        /// --customconfig. Null when the option was not given, in which case the
        /// module is written without one.
        /// </summary>
        public string CustomConfigOptions { get; set; }

        /// <summary>
        /// Application pool the custom section applies to, taken from
        /// --customconfig. Null when the value named none, in which case the
        /// pool this run creates is the one it applies to.
        /// </summary>
        public string CustomConfigAppPool { get; set; }

        /// <summary>
        /// The enabled attribute of system.webServer/caching, taken from
        /// --caching. Null when the value did not set it, in which case the
        /// attribute is left as it was.
        /// </summary>
        public bool? CachingEnabled { get; set; }

        /// <summary>
        /// The enableKernelCache attribute of system.webServer/caching, taken
        /// from --caching. Null when the value did not set it.
        /// </summary>
        public bool? CachingEnableKernelCache { get; set; }

        /// <summary>
        /// The removeServerHeader attribute of
        /// system.webServer/security/requestFiltering, taken from
        /// --requestfiltering. Null when the option was not given.
        /// </summary>
        public bool? RequestFilteringRemoveServerHeader { get; set; }

        /// <summary>
        /// The enabled attribute of system.webServer/directoryBrowse, taken from
        /// --directorybrowse. Null when the option was not given.
        /// </summary>
        public bool? DirectoryBrowseEnabled { get; set; }

        /// <summary>
        /// The applicationHost.config copied to the working location, taken
        /// from --source and made absolute. Null when the option was not given,
        /// in which case the bundled Resources\applicationHost.config is used.
        /// </summary>
        public string SourceConfigPath { get; set; }

        /// <summary>
        /// Where the working copy of applicationHost.config is written. Null
        /// when --config was not given, in which case the default scratch
        /// location is used.
        /// </summary>
        public string ConfigPath { get; set; }
    }

    /// <summary>Outcome of parsing: exactly one of help, error, or options.</summary>
    public sealed class ParseResult
    {
        private ParseResult(CommandLineOptions options, string error, bool helpRequested)
        {
            Options = options;
            Error = error;
            HelpRequested = helpRequested;
        }

        public CommandLineOptions Options { get; }

        public string Error { get; }

        public bool HelpRequested { get; }

        public static ParseResult Success(CommandLineOptions options) => new ParseResult(options, null, false);

        public static ParseResult Fail(string error) => new ParseResult(null, error, false);

        public static ParseResult Help() => new ParseResult(null, null, true);
    }
}
