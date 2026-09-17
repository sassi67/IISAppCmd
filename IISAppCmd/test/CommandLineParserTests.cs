using System;
using System.IO;
using IISAppCmd.CommandLine;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class CommandLineParserTests
    {
        [Test]
        public void Parse_NoArguments_UsesDefaults()
        {
            var result = CommandLineParser.Parse(new string[0]);

            Assert.Multiple(() =>
            {
                Assert.That(result.HelpRequested, Is.False);
                Assert.That(result.Error, Is.Null);
                Assert.That(result.Options.Bitness, Is.EqualTo(Bitness.X64));
                Assert.That(result.Options.Tfm, Is.EqualTo("netcoreapp3.1"));
                Assert.That(result.Options.ConfigPath, Is.Null);
            });
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

        [TestCase("-b", "32", Bitness.X86)]
        [TestCase("-b", "64", Bitness.X64)]
        [TestCase("--bitness", "32", Bitness.X86)]
        [TestCase("--BITNESS", "64", Bitness.X64)]
        public void Parse_Bitness_SpaceSeparated(string option, string value, Bitness expected)
        {
            var result = CommandLineParser.Parse(new[] { option, value });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Bitness, Is.EqualTo(expected));
        }

        [TestCase("-b=32", Bitness.X86)]
        [TestCase("--bitness=64", Bitness.X64)]
        public void Parse_Bitness_EqualsSeparated(string token, Bitness expected)
        {
            var result = CommandLineParser.Parse(new[] { token });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Bitness, Is.EqualTo(expected));
        }

        [TestCase("16")]
        [TestCase("x64")]
        [TestCase("640")]
        public void Parse_InvalidBitness_Fails(string value)
        {
            var result = CommandLineParser.Parse(new[] { "-b", value });

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
            var result = CommandLineParser.Parse(new[] { "--tfm", value });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.Tfm, Is.EqualTo(expected));
        }

        [TestCase("foo")]
        [TestCase("net")]
        [TestCase("net4")]
        [TestCase("v4.0")]
        public void Parse_InvalidTfm_Fails(string value)
        {
            var result = CommandLineParser.Parse(new[] { "-t", value });

            Assert.That(result.Error, Does.Contain($"invalid target framework moniker '{value}'"));
        }

        [Test]
        public void Parse_ConfigPath_IsMadeAbsolute()
        {
            var result = CommandLineParser.Parse(new[] { "-c", "out.config" });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.ConfigPath, Is.EqualTo(Path.GetFullPath("out.config")));
            Assert.That(Path.IsPathRooted(result.Options.ConfigPath), Is.True);
        }

        [Test]
        public void Parse_ConfigPath_ForwardSlashesAreNormalised()
        {
            var result = CommandLineParser.Parse(new[] { "--config=C:/temp/sub/applicationhost.config" });

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Options.ConfigPath, Is.EqualTo(@"C:\temp\sub\applicationhost.config"));
        }

        [Test]
        public void Parse_ConfigPath_WithInvalidCharacters_Fails()
        {
            var result = CommandLineParser.Parse(new[] { "-c", "out|put.config" });

            Assert.That(result.Error, Does.StartWith("invalid config path"));
        }

        [Test]
        public void Parse_AllOptions_MixedForms()
        {
            var result = CommandLineParser.Parse(new[] { "--bitness=32", "-t", "net48", "--config", @"C:\temp\a.config" });

            Assert.That(result.Error, Is.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Options.Bitness, Is.EqualTo(Bitness.X86));
                Assert.That(result.Options.Tfm, Is.EqualTo("net48"));
                Assert.That(result.Options.ConfigPath, Is.EqualTo(@"C:\temp\a.config"));
            });
        }

        [TestCase("-b", "32", "--bitness", "64", "--bitness")]
        [TestCase("-t", "net8.0", "-t=net9.0", null, "--tfm")]
        [TestCase("-c", "a.config", "--config", "b.config", "--config")]
        public void Parse_DuplicateOption_Fails(string first, string firstValue, string second, string secondValue, string canonical)
        {
            var args = secondValue == null
                ? new[] { first, firstValue, second }
                : new[] { first, firstValue, second, secondValue };

            var result = CommandLineParser.Parse(args);

            Assert.That(result.Error, Is.EqualTo($"option '{canonical}' was specified more than once."));
        }

        [TestCase("-x")]
        [TestCase("--port")]
        [TestCase("--reference=Foo")]
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
                Assert.That(CommandLineParser.HelpText, Does.Contain("--bitness"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--tfm"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--config"));
                Assert.That(CommandLineParser.HelpText, Does.Contain("--help"));
            });
        }
    }
}
