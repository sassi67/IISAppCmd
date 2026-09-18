using IISAppCmd.CommandLine;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class CustomConfigFactoryTests
    {
        private const string Options = "tenant=abc,loglevelcon=info";

        [Test]
        public void FromCommandLine_WithoutTheOption_WritesNoSection()
        {
            Assert.That(CustomConfigFactory.FromCommandLine(new CommandLineOptions(), "7430f109"), Is.Null);
        }

        [Test]
        public void FromCommandLine_TakesTheOptionsFromTheCommandLine()
        {
            CustomConfig config = CustomConfigFactory.FromCommandLine(OptionsWith(), "7430f109");

            Assert.That(config.Options, Is.EqualTo(Options));
        }

        [Test]
        public void FromCommandLine_WithoutAnAppPool_UsesTheOneTheRunCreates()
        {
            CustomConfig config = CustomConfigFactory.FromCommandLine(OptionsWith(), "7430f109");

            Assert.That(config.AppPool, Is.EqualTo("AppPool_7430f109"));
        }

        [Test]
        public void FromCommandLine_KeepsTheAppPoolItWasGiven()
        {
            var options = OptionsWith();
            options.CustomConfigAppPool = "DefaultAppPool";

            Assert.That(CustomConfigFactory.FromCommandLine(options, "7430f109").AppPool, Is.EqualTo("DefaultAppPool"));
        }

        [Test]
        public void FromCommandLine_RejectsMissingArguments()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => CustomConfigFactory.FromCommandLine(null, "7430f109"), Throws.ArgumentNullException);
                Assert.That(() => CustomConfigFactory.FromCommandLine(OptionsWith(), ""), Throws.ArgumentException);
            });
        }

        /// <summary>The command line of a run that asks for a custom section.</summary>
        private static CommandLineOptions OptionsWith() => new CommandLineOptions
        {
            Application = "Scratch",
            ApplicationPath = @"C:\apps\scratch",
            GlobalModule = "MyModule",
            GlobalModuleImage = @"C:\modules\my.dll",
            CustomConfigOptions = Options,
        };
    }
}
