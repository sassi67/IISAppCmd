using IISAppCmd.CommandLine;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class ServerConfigFactoryTests
    {
        [Test]
        public void FromCommandLine_WithoutTheOptions_SetsNothing()
        {
            Assert.That(ServerConfigFactory.FromCommandLine(new CommandLineOptions()), Is.Null);
        }

        [Test]
        public void FromCommandLine_NullOptions_Throws()
        {
            Assert.That(() => ServerConfigFactory.FromCommandLine(null), Throws.ArgumentNullException);
        }

        [Test]
        public void FromCommandLine_TakesCachingFromTheCommandLine()
        {
            ServerConfig config = ServerConfigFactory.FromCommandLine(new CommandLineOptions
            {
                CachingEnabled = false,
                CachingEnableKernelCache = true,
            });

            Assert.Multiple(() =>
            {
                Assert.That(config.Caching.Enabled, Is.False);
                Assert.That(config.Caching.EnableKernelCache, Is.True);
                Assert.That(config.RequestFiltering, Is.Null, "a section the command line left alone is not written");
                Assert.That(config.DirectoryBrowse, Is.Null, "a section the command line left alone is not written");
            });
        }

        [Test]
        public void FromCommandLine_CachingWithOneAttribute_LeavesTheOtherUnset()
        {
            ServerConfig config = ServerConfigFactory.FromCommandLine(new CommandLineOptions { CachingEnableKernelCache = false });

            Assert.Multiple(() =>
            {
                Assert.That(config.Caching.Enabled, Is.Null);
                Assert.That(config.Caching.EnableKernelCache, Is.False);
            });
        }

        [Test]
        public void FromCommandLine_TakesRequestFilteringFromTheCommandLine()
        {
            ServerConfig config = ServerConfigFactory.FromCommandLine(new CommandLineOptions { RequestFilteringRemoveServerHeader = true });

            Assert.Multiple(() =>
            {
                Assert.That(config.RequestFiltering.RemoveServerHeader, Is.True);
                Assert.That(config.Caching, Is.Null);
                Assert.That(config.DirectoryBrowse, Is.Null);
            });
        }

        [Test]
        public void FromCommandLine_TakesDirectoryBrowseFromTheCommandLine()
        {
            ServerConfig config = ServerConfigFactory.FromCommandLine(new CommandLineOptions { DirectoryBrowseEnabled = false });

            Assert.Multiple(() =>
            {
                Assert.That(config.DirectoryBrowse.Enabled, Is.False);
                Assert.That(config.Caching, Is.Null);
                Assert.That(config.RequestFiltering, Is.Null);
            });
        }
    }
}
