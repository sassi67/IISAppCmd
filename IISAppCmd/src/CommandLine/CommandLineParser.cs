using System;
using System.IO;
using System.Text.RegularExpressions;

namespace IISAppCmd.CommandLine
{
    /// <summary>
    /// Reads the command line with the same option syntax as iisexpressstarter:
    /// short or long names, with a space or an '=' between an option and its value.
    /// </summary>
    public static class CommandLineParser
    {
        public const string HelpText =
@"IISAppCmd - writes an application pool into a copy of applicationHost.config.

Usage:
  IISAppCmd [-b <32|64>] [-t <tfm>] [-c <path>]

Options:
  -b,   --bitness   <32|64>    Bitness of the worker process. Default 64.
  -t,   --tfm       <moniker>  Framework the application targets. Default netcoreapp3.1.
  -c,   --config    <path>     Where the working copy of applicationHost.config is
                               written. Default %TEMP%\iisconfig\applicationhost-<id>.config.
  -h,   --help                 Show this help text.

Every option is optional. Options may be given in short or long form, with a
space or an '=' between the option and its value.

--bitness decides whether the pool runs 32-bit (enable32BitAppOnWin64) or
64-bit.

--tfm sets the CLR the application pool loads: none for netcoreapp*,
netstandard* and net5.0 or later, v4.0 for net4x, v2.0 for anything older.

The bundled Resources\applicationHost.config is copied to the working
location first, so the original is never touched. The copy must not exist
yet; an existing file is never overwritten.

Examples:
  IISAppCmd -t net10.0
  IISAppCmd --bitness 32 --tfm net48
  IISAppCmd -b 64 -t net10.0 -c C:\temp\applicationhost.config
  IISAppCmd --bitness=64 --tfm=net10.0 --config=C:\temp\applicationhost.config";

        private static readonly Regex TfmPattern = new Regex(
            @"^(net\d+\.\d+(-[a-z][a-z0-9.]*)?|net\d{2,3}|netcoreapp\d+\.\d+|netstandard\d+\.\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static ParseResult Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            string bitness = null;
            string tfm = null;
            string config = null;

            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];

                if (token == "-h" || token == "--help" || token == "-?" || token == "/?")
                {
                    return ParseResult.Help();
                }

                if (!SplitOption(token, out string name, out string value))
                {
                    return ParseResult.Fail($"unexpected argument '{token}'.");
                }

                string canonical;
                switch (name.ToLowerInvariant())
                {
                    case "b":
                    case "bitness":
                        canonical = "bitness";
                        break;
                    case "t":
                    case "tfm":
                        canonical = "tfm";
                        break;
                    case "c":
                    case "config":
                        canonical = "config";
                        break;
                    default:
                        return ParseResult.Fail($"unknown option '{token}'.");
                }

                if (value == null)
                {
                    if (i + 1 >= args.Length || IsOptionLike(args[i + 1]))
                    {
                        return ParseResult.Fail($"option '{token}' requires a value.");
                    }

                    value = args[++i];
                }

                if (value.Length == 0)
                {
                    return ParseResult.Fail($"option '{token}' requires a non-empty value.");
                }

                bool duplicate;
                switch (canonical)
                {
                    case "bitness":
                        duplicate = Assign(ref bitness, value);
                        break;
                    case "tfm":
                        duplicate = Assign(ref tfm, value);
                        break;
                    default:
                        duplicate = Assign(ref config, value);
                        break;
                }

                if (duplicate)
                {
                    return ParseResult.Fail($"option '--{canonical}' was specified more than once.");
                }
            }

            if (bitness != null && bitness != "32" && bitness != "64")
            {
                return ParseResult.Fail($"invalid bitness '{bitness}': expected '32' or '64'.");
            }

            if (tfm != null && !TfmPattern.IsMatch(tfm))
            {
                return ParseResult.Fail(
                    $"invalid target framework moniker '{tfm}': expected something like 'net8.0' or 'net10.0'.");
            }

            if (config != null)
            {
                config = config.Trim();

                // The IIS configuration system only reads absolute paths with
                // backslashes, so the path is normalised here once for everyone.
                try
                {
                    if (config.Length == 0 || config.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        throw new ArgumentException("The path contains invalid characters.");
                    }

                    config = Path.GetFullPath(config);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                {
                    return ParseResult.Fail($"invalid config path '{config}': {ex.Message}");
                }
            }

            return ParseResult.Success(new CommandLineOptions
            {
                Bitness = bitness == "32" ? Bitness.X86
                    : bitness == "64" ? Bitness.X64
                    : CommandLineOptions.DefaultBitness,
                Tfm = tfm != null ? tfm.ToLowerInvariant() : CommandLineOptions.DefaultTfm,
                ConfigPath = config,
            });
        }

        /// <summary>Stores <paramref name="value"/> unless the slot is already taken; returns true on a duplicate.</summary>
        private static bool Assign(ref string slot, string value)
        {
            if (slot != null)
            {
                return true;
            }

            slot = value;
            return false;
        }

        /// <summary>
        /// Splits a token such as "-b", "--bitness", "-b=64" or "--bitness=64" into its
        /// name and, when the '=' form is used, its inline value.
        /// </summary>
        private static bool SplitOption(string token, out string name, out string value)
        {
            name = string.Empty;
            value = null;

            if (!IsOptionLike(token))
            {
                return false;
            }

            string body = token.StartsWith("--", StringComparison.Ordinal) ? token.Substring(2) : token.Substring(1);

            int separator = body.IndexOf('=');
            if (separator >= 0)
            {
                name = body.Substring(0, separator);
                value = body.Substring(separator + 1);
            }
            else
            {
                name = body;
            }

            return name.Length > 0;
        }

        private static bool IsOptionLike(string token) =>
            token.Length > 1 && token[0] == '-';
    }
}
