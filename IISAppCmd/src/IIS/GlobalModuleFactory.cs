using System;
using IISAppCmd.CommandLine;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Fills a <see cref="GlobalModule"/> from the command line, the way
    /// iisexpressstarter registers the native module it is given.
    /// </summary>
    public static class GlobalModuleFactory
    {
        public const string Bitness32PreCondition = "bitness32";

        public const string Bitness64PreCondition = "bitness64";

        /// <summary>
        /// The module named on the command line, or null when --globalmodule was
        /// not given, in which case the run registers none.
        /// </summary>
        public static GlobalModule FromCommandLine(CommandLineOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrEmpty(options.GlobalModule))
            {
                return null;
            }

            return new GlobalModule
            {
                Name = options.GlobalModule,
                Image = options.GlobalModuleImage,

                // A native module is only loaded into a worker process of its own
                // bitness, so the one the run asks for supplies the condition the
                // command line left out.
                PreCondition = options.GlobalModulePreCondition ?? PreConditionFor(options.Bitness),
            };
        }

        /// <summary>The preCondition that matches the bitness of the worker process.</summary>
        public static string PreConditionFor(Bitness bitness) =>
            bitness == Bitness.X86 ? Bitness32PreCondition : Bitness64PreCondition;
    }
}
