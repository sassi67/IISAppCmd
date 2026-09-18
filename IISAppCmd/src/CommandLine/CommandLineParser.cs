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
@"IISAppCmd - writes an application pool, a site and an optional global module
into a copy of applicationHost.config.

Usage:
  IISAppCmd -ap <json> [-gm <json>] [-cc <json>] [-b <32|64>] [-t <tfm>] [-p <port>] [-c <path>]

Options:
  -ap,  --application  <json>   Application the site serves, as
                                {""name"": ""..."", ""path"": ""...""}. Required.
  -gm,  --globalmodule <json>   Native module to register, as {""name"": ""..."",
                                ""image"": ""..."", ""preCondition"": ""...""}.
  -cc,  --customconfig <json>   Custom section of that module, as
                                {""appPool"": ""..."", ""options"": ""...""}.
                                Needs --globalmodule.
  -b,   --bitness      <32|64>  Bitness of the worker process. Default 64.
  -t,   --tfm          <tfm>    Framework the application targets. Default netcoreapp3.1.
  -p,   --port         <number> Port the site listens on, 1-65535. Default 5001.
  -c,   --config       <path>   Where the working copy of applicationHost.config is
                                written. Default %TEMP%\iisconfig\applicationhost-<id>.config.
  -h,   --help                  Show this help text.

Only --application is required. Options may be given in short or long form,
with a space or an '=' between the option and its value.

--application carries the same {""name"", ""path""} pair iisexpressstarter
takes: the name labels the application, and the path is served as the physical
path of the site's root virtual directory. The value is JSON, so it needs
quoting for your shell: escape the inner quotes in cmd, or wrap the whole
value in single quotes in PowerShell and bash.

--globalmodule carries the same {""name"", ""image"", ""preCondition""} triple
iisexpressstarter takes: the module is written to <globalModules> and enabled
in <modules>, so it loads for every application of the site. Only the name and
the image are required; without a preCondition the bitness of the run supplies
one. The image is kept as written, so it may name its DLL through an
environment variable such as %windir%.

--customconfig carries the custom section Resources\IISAgentConfigSchema.xml
defines for a module: the options the agent is given, and the application pool
they apply to. Only the options are required; without an appPool the pool this
run creates is the one they apply to. The section is written inside the entry
the module has in <modules>, so it only makes sense next to --globalmodule.

--bitness decides whether the pool runs 32-bit (enable32BitAppOnWin64) or
64-bit, and which preCondition a global module without one is given,
bitness32 or bitness64.

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
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -c C:\temp\a.config
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -gm '{""name"": ""MyModule"", ""image"": ""C:/modules/my.dll""}'
  IISAppCmd -ap '{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}' -gm '{""name"": ""MyModule"", ""image"": ""C:/modules/my.dll""}' -cc '{""options"": ""tenant=abc,loglevelcon=info""}'";

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
            string globalModule = null;
            string customConfig = null;
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
                    case "gm":
                    case "globalmodule":
                        canonical = "globalmodule";
                        break;
                    case "cc":
                    case "customconfig":
                        canonical = "customconfig";
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
                    case "globalmodule":
                        duplicate = Assign(ref globalModule, value);
                        break;
                    case "customconfig":
                        duplicate = Assign(ref customConfig, value);
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

            string moduleName = null;
            string moduleImage = null;
            string modulePreCondition = null;

            if (globalModule != null &&
                !ReadInlineGlobalModule(globalModule, out moduleName, out moduleImage, out modulePreCondition, out string moduleError))
            {
                return ParseResult.Fail(moduleError);
            }

            string customOptions = null;
            string customAppPool = null;

            if (customConfig != null)
            {
                // The section lives inside the entry the module has in
                // <modules>, so without a module there is nowhere to put it.
                if (globalModule == null)
                {
                    return ParseResult.Fail("--customconfig needs --globalmodule: the section is written into that module.");
                }

                if (!ReadInlineCustomConfig(customConfig, out customOptions, out customAppPool, out string customError))
                {
                    return ParseResult.Fail(customError);
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
                GlobalModule = moduleName,
                GlobalModuleImage = moduleImage,
                GlobalModulePreCondition = modulePreCondition,
                CustomConfigOptions = customOptions,
                CustomConfigAppPool = customAppPool,
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

        /// <summary>
        /// Reads the inline --globalmodule value, which has the same shape as one
        /// entry of the "globalModules" array of iisexpressstarter's configuration
        /// file: a required name and image, and an optional preCondition.
        /// </summary>
        private static bool ReadInlineGlobalModule(string value, out string name, out string image, out string preCondition, out string error)
        {
            name = null;
            image = null;
            preCondition = null;

            object parsed;

            try
            {
                parsed = new JavaScriptSerializer().DeserializeObject(value);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                error = $"--globalmodule is not valid JSON: {ex.Message}";
                return false;
            }

            if (!(parsed is Dictionary<string, object> members))
            {
                error = "--globalmodule expects a JSON object with a 'name' and an 'image'.";
                return false;
            }

            if (!TryReadText(members, "name", out name))
            {
                error = "--globalmodule is missing a non-empty 'name'.";
                return false;
            }

            if (!TryReadText(members, "image", out image))
            {
                error = "--globalmodule is missing a non-empty 'image'.";
                return false;
            }

            // The image is left as written, because IIS expands the environment
            // variables such a path usually carries; only what no path may hold
            // is refused here.
            int invalidCharIndex = image.IndexOfAny(Path.GetInvalidPathChars());
            if (invalidCharIndex >= 0)
            {
                error = $"invalid global module image '{image}': it contains the unsupported character "
                    + $"'{image[invalidCharIndex]}'.";
                return false;
            }

            // The preCondition is optional: without one the bitness of the run
            // decides it, so only a present-but-empty value is a mistake.
            if (members.ContainsKey("preCondition") && !TryReadText(members, "preCondition", out preCondition))
            {
                error = "--globalmodule has an empty 'preCondition'.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Reads the inline --customconfig value, whose members are the two
        /// attributes Resources\IISAgentConfigSchema.xml gives the custom
        /// section: a required options, and an optional appPool.
        /// </summary>
        private static bool ReadInlineCustomConfig(string value, out string options, out string appPool, out string error)
        {
            options = null;
            appPool = null;

            object parsed;

            try
            {
                parsed = new JavaScriptSerializer().DeserializeObject(value);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                error = $"--customconfig is not valid JSON: {ex.Message}";
                return false;
            }

            if (!(parsed is Dictionary<string, object> members))
            {
                error = "--customconfig expects a JSON object with an 'options'.";
                return false;
            }

            if (!TryReadText(members, "options", out options))
            {
                error = "--customconfig is missing a non-empty 'options'.";
                return false;
            }

            // The appPool is optional: without one the pool this run creates is
            // the one the section applies to, so only a present-but-empty value
            // is a mistake.
            if (members.ContainsKey("appPool") && !TryReadText(members, "appPool", out appPool))
            {
                error = "--customconfig has an empty 'appPool'.";
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
