using System;
using System.IO;
using System.Linq;
using IISAppCmd.CommandLine;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class CommandLineParserTests
    {
        /// <summary>The one required option, so every other case can focus on its own.</summary>
        private const string ApplicationJson = @"{""name"": ""Scratch"", ""path"": ""C:/apps/scratch""}";

        private const string ApplicationPath = @"C:\apps\scratch";

        private const string GlobalModuleJson = @"{""name"": ""MyModule"", ""image"": ""C:/modules/my.dll""}";

        [Test]
        public void Parse_OnlyApplication_UsesDefaults()
        {
            var result = CommandLineParser.Parse(With());

            Assert.Multiple(() =>
            {
                Assert.That(result.HelpRequested, Is.False);
                Assert.That(result.Error, Is.Null);
                Assert.That(result.Options.Bitness, Is.EqualTo(Bitness.X64));
                Assert.That(result.Options.Tfm, Is.EqualTo("netcoreapp3.1"));
                Assert.That(result.Options.Port, Is.EqualTo(5001));
                Assert.That(result.Options.Application, Is.EqualTo("Scratch"));
                Assert.That(result.Options.ApplicationPath, Is.EqualTo(ApplicationPath));
                Assert.That(result.Options.GlobalModule, Is.Null);
                Assert.That(result.Options.GlobalModuleImage, Is.Null);
                Assert.That(result.Options.GlobalModulePreCondition, Is.Null);
                Assert.That(result.Options.ConfigPath, Is.Null);
            });
        }

        [Test]
        public void Parse_NoArguments_Fails()
        {
            var result = CommandLineParser.Parse(new string[0]);

            Assert.That(result.Error, Is.EqualTo("missing required option(s): --application."));
        }

        [Test]
        public void Parse_NullArguments_Throws()
        {
            Assert.That(() => CommandLineParser.Parse(null), Throws.ArgumentNullException);
        }

        [TestCase("-h")]
        [TestCase("--help")]
        [TestCase("-?")]
        [TestCase("/?")]
        public void Parse_HelpSwitch_RequestsHelp(string token)
        {
            var result = CommandLineParser.Parse(new[] { "-b", "32", token });

            Assert.Multiple(() =>
            {
                Assert.That(result.HelpRequested, Is.True);
                Assert.That(result.Options, Is.Null);
                Assert.That(result.Error, Is.Null);
            });
        }

        [TestCase("-ap")]
        [TestCase("--application")]
        [TestCase("--APPLICATION")]
        public void Parse_Application_ReadsNameAndPath(string option)
        {
            var result = CommandLineParser.Parse(new[] { option, ApplicationJson });

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.Application, Is.EqualTo("Scratch"));
                Assert.That(result.Options.ApplicationPath, Is.EqualTo(ApplicationPath));
            });
        }

        [Test]
        public void Parse_Application_EqualsSeparated()
        {
            var result = CommandLineParser.Parse(new[] { "-ap=" + ApplicationJson });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Application, Is.EqualTo("Scratch"));
        }

        [Test]
        public void Parse_Application_MembersMayComeInAnyOrder()
        {
            var result = CommandLineParser.Parse(new[] { "-ap", @"{""path"": ""C:/apps/scratch"", ""name"": ""Scratch""}" });

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.Application, Is.EqualTo("Scratch"));
                Assert.That(result.Options.ApplicationPath, Is.EqualTo(ApplicationPath));
            });
        }

        [Test]
        public void Parse_Application_PathIsMadeAbsolute()
        {
            var result = CommandLineParser.Parse(new[] { "-ap", @"{""name"": ""Scratch"", ""path"": ""apps/scratch""}" });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.ApplicationPath, Is.EqualTo(Path.GetFullPath("apps/scratch")));
            Assert.That(Path.IsPathRooted(result.Options.ApplicationPath), Is.True);
        }

        [TestCase("not json")]
        [TestCase(@"{""name"": ""Scratch"",}")]
        public void Parse_Application_InvalidJson_Fails(string value)
        {
            var result = CommandLineParser.Parse(new[] { "-ap", value });

            Assert.That(result.Error, Does.StartWith("--application is not valid JSON"));
        }

        [TestCase("[1, 2]")]
        [TestCase("42")]
        [TestCase(@"""Scratch""")]
        public void Parse_Application_NotAnObject_Fails(string value)
        {
            var result = CommandLineParser.Parse(new[] { "-ap", value });

            Assert.That(result.Error, Is.EqualTo("--application expects a JSON object with a 'name' and a 'path'."));
        }

        [TestCase(@"{""path"": ""C:/apps/scratch""}", "name")]
        [TestCase(@"{""name"": ""  "", ""path"": ""C:/apps/scratch""}", "name")]
        [TestCase(@"{""name"": 7, ""path"": ""C:/apps/scratch""}", "name")]
        [TestCase(@"{""name"": ""Scratch""}", "path")]
        [TestCase(@"{""name"": ""Scratch"", ""path"": """"}", "path")]
        public void Parse_Application_MissingMember_Fails(string value, string member)
        {
            var result = CommandLineParser.Parse(new[] { "-ap", value });

            Assert.That(result.Error, Is.EqualTo($"--application is missing a non-empty '{member}'."));
        }

        [Test]
        public void Parse_Application_NameWithInvalidCharacter_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-ap", @"{""name"": ""Scra/tch"", ""path"": ""C:/apps/scratch""}" });

            Assert.Multiple(() =>
            {
                Assert.That(result.Error, Does.StartWith("invalid application name 'Scra/tch'"));
                Assert.That(result.Error, Does.Contain("'/'"));
            });
        }

        [Test]
        public void Parse_Application_PathWithInvalidCharacters_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-ap", @"{""name"": ""Scratch"", ""path"": ""C:/ap|ps""}" });

            Assert.That(result.Error, Does.StartWith("invalid application path 'C:/ap|ps'"));
        }

        [TestCase("-gm")]
        [TestCase("--globalmodule")]
        [TestCase("--GLOBALMODULE")]
        public void Parse_GlobalModule_ReadsNameAndImage(string option)
        {
            var result = CommandLineParser.Parse(With(option, GlobalModuleJson));

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.GlobalModule, Is.EqualTo("MyModule"));
                Assert.That(result.Options.GlobalModuleImage, Is.EqualTo("C:/modules/my.dll"));
                Assert.That(result.Options.GlobalModulePreCondition, Is.Null, "without one the bitness of the run decides it");
            });
        }

        [Test]
        public void Parse_GlobalModule_EqualsSeparated()
        {
            var result = CommandLineParser.Parse(With("-gm=" + GlobalModuleJson));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.GlobalModule, Is.EqualTo("MyModule"));
        }

        [Test]
        public void Parse_GlobalModule_ReadsThePreCondition()
        {
            var result = CommandLineParser.Parse(With(
                "-gm", @"{""name"": ""MyModule"", ""image"": ""C:/modules/my.dll"", ""preCondition"": ""bitness64,integratedMode""}"));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.GlobalModulePreCondition, Is.EqualTo("bitness64,integratedMode"));
        }

        [Test]
        public void Parse_GlobalModule_MembersMayComeInAnyOrder()
        {
            var result = CommandLineParser.Parse(With("-gm", @"{""image"": ""C:/modules/my.dll"", ""name"": ""MyModule""}"));

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.GlobalModule, Is.EqualTo("MyModule"));
                Assert.That(result.Options.GlobalModuleImage, Is.EqualTo("C:/modules/my.dll"));
            });
        }

        [Test]
        public void Parse_GlobalModule_ImageKeepsItsEnvironmentVariables()
        {
            var result = CommandLineParser.Parse(With(
                "-gm", @"{""name"": ""MyModule"", ""image"": ""%windir%\\System32\\inetsrv\\my.dll""}"));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.GlobalModuleImage, Is.EqualTo(@"%windir%\System32\inetsrv\my.dll"));
        }

        [TestCase("not json")]
        [TestCase(@"{""name"": ""MyModule"",}")]
        public void Parse_GlobalModule_InvalidJson_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("-gm", value));

            Assert.That(result.Error, Does.StartWith("--globalmodule is not valid JSON"));
        }

        [TestCase("[1, 2]")]
        [TestCase("42")]
        [TestCase(@"""MyModule""")]
        public void Parse_GlobalModule_NotAnObject_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("-gm", value));

            Assert.That(result.Error, Is.EqualTo("--globalmodule expects a JSON object with a 'name' and an 'image'."));
        }

        [TestCase(@"{""image"": ""C:/modules/my.dll""}", "name")]
        [TestCase(@"{""name"": ""  "", ""image"": ""C:/modules/my.dll""}", "name")]
        [TestCase(@"{""name"": 7, ""image"": ""C:/modules/my.dll""}", "name")]
        [TestCase(@"{""name"": ""MyModule""}", "image")]
        [TestCase(@"{""name"": ""MyModule"", ""image"": """"}", "image")]
        public void Parse_GlobalModule_MissingMember_Fails(string value, string member)
        {
            var result = CommandLineParser.Parse(With("-gm", value));

            Assert.That(result.Error, Is.EqualTo($"--globalmodule is missing a non-empty '{member}'."));
        }

        [Test]
        public void Parse_GlobalModule_EmptyPreCondition_Fails()
        {
            var result = CommandLineParser.Parse(With(
                "-gm", @"{""name"": ""MyModule"", ""image"": ""C:/modules/my.dll"", ""preCondition"": ""  ""}"));

            Assert.That(result.Error, Is.EqualTo("--globalmodule has an empty 'preCondition'."));
        }

        [Test]
        public void Parse_GlobalModule_ImageWithInvalidCharacters_Fails()
        {
            var result = CommandLineParser.Parse(With("-gm", @"{""name"": ""MyModule"", ""image"": ""C:/mod|ules/my.dll""}"));

            Assert.Multiple(() =>
            {
                Assert.That(result.Error, Does.StartWith("invalid global module image 'C:/mod|ules/my.dll'"));
                Assert.That(result.Error, Does.Contain("'|'"));
            });
        }

        [Test]
        public void Parse_DuplicateGlobalModule_Fails()
        {
            var result = CommandLineParser.Parse(With("-gm", GlobalModuleJson, "--globalmodule", GlobalModuleJson));

            Assert.That(result.Error, Is.EqualTo("option '--globalmodule' was specified more than once."));
        }

        [TestCase("-b", "32", Bitness.X86)]
        [TestCase("-b", "64", Bitness.X64)]
        [TestCase("--bitness", "32", Bitness.X86)]
        [TestCase("--BITNESS", "64", Bitness.X64)]
        public void Parse_Bitness_SpaceSeparated(string option, string value, Bitness expected)
        {
            var result = CommandLineParser.Parse(With(option, value));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Bitness, Is.EqualTo(expected));
        }

        [TestCase("-b=32", Bitness.X86)]
        [TestCase("--bitness=64", Bitness.X64)]
        public void Parse_Bitness_EqualsSeparated(string token, Bitness expected)
        {
            var result = CommandLineParser.Parse(With(token));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Bitness, Is.EqualTo(expected));
        }

        [TestCase("16")]
        [TestCase("x64")]
        [TestCase("640")]
        public void Parse_InvalidBitness_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("-b", value));

            Assert.That(result.Error, Does.Contain($"invalid bitness '{value}'"));
        }

        [TestCase("net10.0", "net10.0")]
        [TestCase("NET8.0", "net8.0")]
        [TestCase("net8.0-windows", "net8.0-windows")]
        [TestCase("net48", "net48")]
        [TestCase("net472", "net472")]
        [TestCase("netcoreapp3.1", "netcoreapp3.1")]
        [TestCase("netstandard2.0", "netstandard2.0")]
        public void Parse_ValidTfm_IsLowerCased(string value, string expected)
        {
            var result = CommandLineParser.Parse(With("--tfm", value));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Tfm, Is.EqualTo(expected));
        }

        [TestCase("foo")]
        [TestCase("net")]
        [TestCase("net4")]
        [TestCase("v4.0")]
        public void Parse_InvalidTfm_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("-t", value));

            Assert.That(result.Error, Does.Contain($"invalid target framework moniker '{value}'"));
        }

        [TestCase("-p", "8080", 8080)]
        [TestCase("--port", "1", 1)]
        [TestCase("--PORT", "65535", 65535)]
        public void Parse_Port_SpaceSeparated(string option, string value, int expected)
        {
            var result = CommandLineParser.Parse(With(option, value));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Port, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_Port_EqualsSeparated()
        {
            var result = CommandLineParser.Parse(With("--port=8080"));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Port, Is.EqualTo(8080));
        }

        [TestCase("http")]
        [TestCase("80.5")]
        [TestCase("-1")]
        public void Parse_NonNumericPort_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("--port=" + value));

            Assert.That(result.Error, Is.EqualTo($"invalid port '{value}': expected a whole number."));
        }

        [TestCase("0")]
        [TestCase("65536")]
        public void Parse_PortOutOfRange_Fails(string value)
        {
            var result = CommandLineParser.Parse(With("-p", value));

            Assert.That(result.Error, Is.EqualTo($"invalid port '{value}': expected a number between 1 and 65535."));
        }

        [Test]
        public void Parse_ConfigPath_IsMadeAbsolute()
        {
            var result = CommandLineParser.Parse(With("-c", "out.config"));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.ConfigPath, Is.EqualTo(Path.GetFullPath("out.config")));
            Assert.That(Path.IsPathRooted(result.Options.ConfigPath), Is.True);
        }

        [Test]
        public void Parse_ConfigPath_ForwardSlashesAreNormalised()
        {
            var result = CommandLineParser.Parse(With("--config=C:/temp/sub/applicationhost.config"));

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.ConfigPath, Is.EqualTo(@"C:\temp\sub\applicationhost.config"));
        }

        [Test]
        public void Parse_ConfigPath_WithInvalidCharacters_Fails()
        {
            var result = CommandLineParser.Parse(With("-c", "out|put.config"));

            Assert.That(result.Error, Does.StartWith("invalid config path"));
        }

        [Test]
        public void Parse_AllOptions_MixedForms()
        {
            var result = CommandLineParser.Parse(new[]
            {
                "--bitness=32", "-t", "net48", "-ap", ApplicationJson, "--port", "8080",
                "-gm=" + GlobalModuleJson, "--config", @"C:\temp\a.config",
            });

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.Bitness, Is.EqualTo(Bitness.X86));
                Assert.That(result.Options.Tfm, Is.EqualTo("net48"));
                Assert.That(result.Options.Application, Is.EqualTo("Scratch"));
                Assert.That(result.Options.ApplicationPath, Is.EqualTo(ApplicationPath));
                Assert.That(result.Options.Port, Is.EqualTo(8080));
                Assert.That(result.Options.GlobalModule, Is.EqualTo("MyModule"));
                Assert.That(result.Options.GlobalModuleImage, Is.EqualTo("C:/modules/my.dll"));
                Assert.That(result.Options.ConfigPath, Is.EqualTo(@"C:\temp\a.config"));
            });
        }

        [TestCase("-b", "32", "--bitness", "64", "--bitness")]
        [TestCase("-t", "net8.0", "-t=net9.0", null, "--tfm")]
        [TestCase("-p", "8080", "--port", "8081", "--port")]
        [TestCase("-c", "a.config", "--config", "b.config", "--config")]
        public void Parse_DuplicateOption_Fails(string first, string firstValue, string second, string secondValue, string canonical)
        {
            var args = secondValue == null
                ? new[] { first, firstValue, second }
                : new[] { first, firstValue, second, secondValue };

            var result = CommandLineParser.Parse(args);

            Assert.That(result.Error, Is.EqualTo($"option '{canonical}' was specified more than once."));
        }

        [Test]
        public void Parse_DuplicateApplication_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-ap", ApplicationJson, "--application", ApplicationJson });

            Assert.That(result.Error, Is.EqualTo("option '--application' was specified more than once."));
        }

        [TestCase("-x")]
        [TestCase("--reference=Foo")]
        [TestCase("--enable-iis-agent")]
        public void Parse_UnknownOption_Fails(string token)
        {
            var result = CommandLineParser.Parse(new[] { token });

            Assert.That(result.Error, Does.StartWith("unknown option"));
        }

        [Test]
        public void Parse_PositionalArgument_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "net10.0" });

            Assert.That(result.Error, Is.EqualTo("unexpected argument 'net10.0'."));
        }

        [Test]
        public void Parse_OptionAtEndWithoutValue_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-t" });

            Assert.That(result.Error, Is.EqualTo("option '-t' requires a value."));
        }

        [Test]
        public void Parse_OptionFollowedByOption_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-b", "-t", "net8.0" });

            Assert.That(result.Error, Is.EqualTo("option '-b' requires a value."));
        }

        [Test]
        public void Parse_EmptyInlineValue_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "--tfm=" });

            Assert.That(result.Error, Is.EqualTo("option '--tfm=' requires a non-empty value."));
        }

        [Test]
        public void HelpText_MentionsEveryOption()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CommandLineParser.HelpText, Does.Contain("--application"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--globalmodule"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--bitness"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--tfm"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--port"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--config"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--help"));
            });
        }

        /// <summary>The given arguments, preceded by the required --application.</summary>
        private static string[] With(params string[] args) =>
            new[] { "-ap", ApplicationJson }.Concat(args).ToArray();
    }
}
