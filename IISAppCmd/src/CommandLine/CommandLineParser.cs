using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace IISAppCmd.CommandLine
{
    /// <summary>
    /// Reads the command line with the same option syntax as iisexpressstarter:
    /// short or long names, with a space or an '=' between an option and its value.
    /// </summary>
    public static class CommandLineParser
    {
        public const string HelpText =
@"IISAppCmd - writes an application pool and a site into a copy of applicationHost.config.

Usage:
  IISAppCmd -ap <json> [-b <32|64>] [-t <tfm>] [-p <port>] [-c <path>]

Options:
  -ap,  --application <json>   Application the site serves, as
                               {""name"": ""..."", ""path"": ""...""}. Required.
  -b,   --bitness     <32|64>  Bitness of the worker process. Default 64.
  -t,   --tfm         <tfm>    Framework the application targets. Default netcoreapp3.1.
  -p,   --port        <number> Port the site listens on, 1-65535. Default 5001.
  -c,   --config      <path>   Where the working copy of applicationHost.config is
                               written. Default %TEMP%\iisconfig\applicationhost-<id>.config.
  -h,   --help                 Show this help text.

Only --application is required. Options may be given in short or long form,
with a space or an '=' between the option and its value.

--application carries the same {""name"", ""path""} pair iisexpressstarter
takes: the name labels the application, and the path is served as the physical
path of the site's root virtual directory. The value is JSON, so it needs
quoting for your shell: escape the inner quotes in cmd, or wrap the whole
value in single quotes in PowerShell and bash.

--bitness decides whether the pool runs 32-bit (enable32BitAppOnWin64) or
64-bit.

--tfm sets the CLR the application pool loads: none for netcoreapp*,
netstandard* and net5.0 or later, v4.0 for net4x, v2.0 for anything older.

--port decides the binding of the site, which always listens on localhost.

The bundled Resources\applicationHost.config is copied to the working
location first, so the original is never touched. The copy must not exist
yet; an existing file is never overwritten.

Examples:
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -t net10.0
  IISAppCmd --application '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' --port 8080
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -b 32 -t net48
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -c C:\temp\a.config";

        private static readonly Regex TfmPattern = new Regex(
            @"^(net\d+\.\d+(-[a-z][a-z0-9.]*)?|net\d{2,3}|netcoreapp\d+\.\d+|netstandard\d+\.\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static ParseResult Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            string bitness = null;
            string tfm = null;
            string port = null;
            string application = null;
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
                    case "p":
                    case "port":
                        canonical = "port";
                        break;
                    case "ap":
                    case "application":
                        canonical = "application";
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
                    case "port":
                        duplicate = Assign(ref port, value);
                        break;
                    case "application":
                        duplicate = Assign(ref application, value);
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

            // The site has nowhere to point without it, so it is the one option
            // the tool cannot default.
            if (application == null)
            {
                return ParseResult.Fail("missing required option(s): --application.");
            }

            if (!ReadInlineApplication(application, out string applicationName, out string applicationPath, out string applicationError))
            {
                return ParseResult.Fail(applicationError);
            }

            int invalidCharIndex = applicationName.IndexOfAny(Path.GetInvalidFileNameChars());
            if (invalidCharIndex >= 0)
            {
                return ParseResult.Fail(
                    $"invalid application name '{applicationName}': it contains the unsupported character "
                    + $"'{applicationName[invalidCharIndex]}'.");
            }

            if (!TryMakeAbsolute(applicationPath, out string physicalPath, out string pathError))
            {
                return ParseResult.Fail($"invalid application path '{applicationPath}': {pathError}");
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

            int listeningPort = CommandLineOptions.DefaultPort;

            if (port != null)
            {
                if (!int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out listeningPort))
                {
                    return ParseResult.Fail($"invalid port '{port}': expected a whole number.");
                }

                if (listeningPort < 1 || listeningPort > 65535)
                {
                    return ParseResult.Fail($"invalid port '{port}': expected a number between 1 and 65535.");
                }
            }

            if (config != null)
            {
                if (!TryMakeAbsolute(config, out string absoluteConfig, out string configError))
                {
                    return ParseResult.Fail($"invalid config path '{config.Trim()}': {configError}");
                }

                config = absoluteConfig;
            }

            return ParseResult.Success(new CommandLineOptions
            {
                Bitness = bitness == "32" ? Bitness.X86
                    : bitness == "64" ? Bitness.X64
                    : CommandLineOptions.DefaultBitness,
                Tfm = tfm != null ? tfm.ToLowerInvariant() : CommandLineOptions.DefaultTfm,
                Application = applicationName,
                ApplicationPath = physicalPath,
                Port = listeningPort,
                ConfigPath = config,
            });
        }

        /// <summary>
        /// Reads the inline --application value, which has the same shape as one
        /// entry of the "applications" array of iisexpressstarter's configuration
        /// file.
        /// </summary>
        private static bool ReadInlineApplication(string value, out string name, out string path, out string error)
        {
            name = string.Empty;
            path = string.Empty;

            object parsed;

            try
            {
                parsed = new JavaScriptSerializer().DeserializeObject(value);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                error = $"--application is not valid JSON: {ex.Message}";
                return false;
            }

            if (!(parsed is Dictionary<string, object> members))
            {
                error = "--application expects a JSON object with a 'name' and a 'path'.";
                return false;
            }

            if (!TryReadText(members, "name", out name))
            {
                error = "--application is missing a non-empty 'name'.";
                return false;
            }

            if (!TryReadText(members, "path", out path))
            {
                error = "--application is missing a non-empty 'path'.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool TryReadText(IDictionary<string, object> members, string property, out string text)
        {
            text = string.Empty;

            if (!members.TryGetValue(property, out object value) ||
                !(value is string candidate) ||
                string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            text = candidate.Trim();
            return true;
        }

        /// <summary>
        /// The IIS configuration system only reads absolute paths with
        /// backslashes, so every path taken from the command line is normalised
        /// here once for everyone.
        /// </summary>
        private static bool TryMakeAbsolute(string path, out string absolute, out string error)
        {
            absolute = null;
            path = path.Trim();

            try
            {
                if (path.Length == 0 || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    throw new ArgumentException("The path contains invalid characters.");
                }

                absolute = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                error = ex.Message;
                return false;
            }

            error = string.Empty;
            return true;
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
