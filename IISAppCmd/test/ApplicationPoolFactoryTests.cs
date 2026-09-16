using System;
using IISAppCmd.CommandLine;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class ApplicationPoolFactoryTests
    {
        [TestCase("net10.0", "")]
        [TestCase("net8.0", "")]
        [TestCase("net8.0-windows", "")]
        [TestCase("net5.0", "")]
        [TestCase("netcoreapp3.1", "")]
        [TestCase("netstandard2.0", "")]
        [TestCase("net48", "v4.0")]
        [TestCase("net481", "v4.0")]
        [TestCase("net472", "v4.0")]
        [TestCase("net40", "v4.0")]
        [TestCase("net35", "v2.0")]
        [TestCase("net20", "v2.0")]
        [TestCase("NET48", "v4.0")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("netxyz", "")]
        public void ManagedRuntimeVersionFor_MapsMonikerToClr(string tfm, string expected)
        {
            Assert.That(ApplicationPoolFactory.ManagedRuntimeVersionFor(tfm), Is.EqualTo(expected));
        }

        [Test]
        public void FromCommandLine_NamesPoolAfterRunId()
        {
            var pool = ApplicationPoolFactory.FromCommandLine(new CommandLineOptions(), "7430f109");

            Assert.That(pool.Name, Is.EqualTo("AppPool_7430f109"));
        }

        [Test]
        public void FromCommandLine_MirrorsReferenceToolValues()
        {
            var options = new CommandLineOptions { Bitness = Bitness.X64, Tfm = "net10.0" };

            var pool = ApplicationPoolFactory.FromCommandLine(options, "abc12345");

            Assert.Multiple(() =>
            {
                Assert.That(pool.ManagedRuntimeVersion, Is.EqualTo(""));
                Assert.That(pool.ManagedPipelineMode, Is.EqualTo(ManagedPipelineMode.Integrated));
                Assert.That(pool.Enable32BitAppOnWin64, Is.False);
                Assert.That(pool.AutoStart, Is.True);
                Assert.That(pool.CLRConfigFile, Is.EqualTo(@"%IIS_BIN%\config\templates\PersonalWebServer\aspnet.config"));
            });
        }

        [Test]
        public void FromCommandLine_Bitness32_Enables32BitPool()
        {
            var options = new CommandLineOptions { Bitness = Bitness.X86, Tfm = "net48" };

            var pool = ApplicationPoolFactory.FromCommandLine(options, "abc12345");

            Assert.Multiple(() =>
            {
                Assert.That(pool.Enable32BitAppOnWin64, Is.True);
                Assert.That(pool.ManagedRuntimeVersion, Is.EqualTo("v4.0"));
            });
        }

        [Test]
        public void FromCommandLine_LeavesOtherSettingsAtSchemaDefaults()
        {
            var pool = ApplicationPoolFactory.FromCommandLine(new CommandLineOptions(), "abc12345");
            var defaults = new ApplicationPool();

            Assert.Multiple(() =>
            {
                Assert.That(pool.QueueLength, Is.EqualTo(defaults.QueueLength));
                Assert.That(pool.StartMode, Is.EqualTo(defaults.StartMode));
                Assert.That(pool.ProcessModel.IdentityType, Is.EqualTo(defaults.ProcessModel.IdentityType));
                Assert.That(pool.Recycling.PeriodicRestart.Time, Is.EqualTo(defaults.Recycling.PeriodicRestart.Time));
                Assert.That(pool.EnvironmentVariables, Is.Empty);
            });
        }

        [Test]
        public void FromCommandLine_NullOptions_Throws()
        {
            Assert.That(() => ApplicationPoolFactory.FromCommandLine(null, "abc12345"), Throws.ArgumentNullException);
        }

        [TestCase("")]
        [TestCase(null)]
        public void FromCommandLine_EmptyId_Throws(string id)
        {
            Assert.That(() => ApplicationPoolFactory.FromCommandLine(new CommandLineOptions(), id), Throws.ArgumentException);
        }
    }
}
