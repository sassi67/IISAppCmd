namespace IISAppCmd.CommandLine
{
    /// <summary>Process bitness requested on the command line.</summary>
    public enum Bitness
    {
        X86 = 32,
        X64 = 64,
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

        /// <summary>Port the site binds to.</summary>
        public int Port { get; set; } = DefaultPort;

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
