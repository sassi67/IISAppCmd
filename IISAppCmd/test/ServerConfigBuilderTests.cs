using System;
using System.IO;
using System.Xml.Linq;
using IISAppCmd.Config;
using IISAppCmd.IIS;
using NUnit.Framework;
using ServerManager = Microsoft.Web.Administration.ServerManager;

namespace IISAppCmd.Tests
{
    // Writes through Microsoft.Web.Administration into a private copy of the
    // bundled applicationHost.config and checks the resulting XML. IIS Express
    // is never started.
    public class ServerConfigBuilderTests
    {
        private string _scratch;
        private string _configPath;

        [SetUp]
        public void SetUp()
        {
            _scratch = Path.Combine(Path.GetTempPath(), "IISAppCmd.Tests", Guid.NewGuid().ToString("N"));
            _configPath = Path.Combine(_scratch, "applicationhost.config");

            Assert.That(
                ApplicationHostConfig.CreateWorkingCopy(ApplicationHostConfig.BundledPath, _configPath, out string error),
                Is.True, error);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_scratch))
            {
                Directory.Delete(_scratch, recursive: true);
            }
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            using (var manager = new ServerManager(_configPath))
            {
                Assert.That(() => new ServerConfigBuilder(null, manager, _configPath), Throws.ArgumentNullException);
            }
        }

        [Test]
        public void Constructor_MissingFile_Throws()
        {
            using (var manager = new ServerManager(_configPath))
            {
                Assert.That(
                    () => new ServerConfigBuilder(new ServerConfig(), manager, Path.Combine(_scratch, "missing.config")),
                    Throws.TypeOf<FileNotFoundException>());
            }
        }

        [Test]
        public void Build_SetsBothCachingAttributes()
        {
            BuildAndCommit(new ServerConfig { Caching = new Caching { Enabled = false, EnableKernelCache = false } });

            XElement caching = WebServer().Element("caching");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(caching, "enabled"), Is.EqualTo("false"));
                Assert.That(Attribute(caching, "enableKernelCache"), Is.EqualTo("false"));
            });
        }

        [Test]
        public void Build_CachingAttributeLeftOut_KeepsTheValueOfTheFile()
        {
            BuildAndCommit(new ServerConfig { Caching = new Caching { EnableKernelCache = false } });

            XElement caching = WebServer().Element("caching");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(caching, "enabled"), Is.EqualTo("true"), "the bundled file enables caching");
                Assert.That(Attribute(caching, "enableKernelCache"), Is.EqualTo("false"));
            });
        }

        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void Build_SetsRemoveServerHeader(bool value, string expected)
        {
            BuildAndCommit(new ServerConfig { RequestFiltering = new RequestFiltering { RemoveServerHeader = value } });

            XElement filtering = WebServer().Element("security")?.Element("requestFiltering");

            Assert.That(filtering, Is.Not.Null, "requestFiltering section is missing");
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(filtering, "removeServerHeader"), Is.EqualTo(expected));
                Assert.That(filtering.Element("hiddenSegments"), Is.Not.Null, "the rest of the section is kept");
            });
        }

        [Test]
        public void Build_EnablesDirectoryBrowsing()
        {
            BuildAndCommit(new ServerConfig { DirectoryBrowse = new DirectoryBrowse { Enabled = true } });

            Assert.That(Attribute(WebServer().Element("directoryBrowse"), "enabled"), Is.EqualTo("true"));
        }

        [Test]
        public void Build_SetsEverySectionAtOnce()
        {
            BuildAndCommit(new ServerConfig
            {
                Caching = new Caching { Enabled = false },
                RequestFiltering = new RequestFiltering { RemoveServerHeader = true },
                DirectoryBrowse = new DirectoryBrowse { Enabled = true },
            });

            XElement webServer = WebServer();

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(webServer.Element("caching"), "enabled"), Is.EqualTo("false"));
                Assert.That(Attribute(webServer.Element("security").Element("requestFiltering"), "removeServerHeader"), Is.EqualTo("true"));
                Assert.That(Attribute(webServer.Element("directoryBrowse"), "enabled"), Is.EqualTo("true"));
            });
        }

        [Test]
        public void Build_SectionLeftAlone_IsNotTouched()
        {
            BuildAndCommit(new ServerConfig { DirectoryBrowse = new DirectoryBrowse { Enabled = true } });

            XElement webServer = WebServer();

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(webServer.Element("caching"), "enabled"), Is.EqualTo("true"));
                Assert.That(Attribute(webServer.Element("caching"), "enableKernelCache"), Is.EqualTo("true"));
                Assert.That(webServer.Element("security").Element("requestFiltering").Attribute("removeServerHeader"), Is.Null);
            });
        }

        [Test]
        public void Build_IsWrittenAtTheRootOfTheFile()
        {
            BuildAndCommit(new ServerConfig { DirectoryBrowse = new DirectoryBrowse { Enabled = true } });

            // appcmd set config without a path writes to the server level, so
            // the setting applies to every site instead of a <location> of one.
            foreach (XElement location in XDocument.Load(_configPath).Root.Elements("location"))
            {
                Assert.That(location.Element("system.webServer")?.Element("directoryBrowse"), Is.Null);
            }
        }

        [Test]
        public void Build_RunTwice_KeepsTheLastValue()
        {
            BuildAndCommit(new ServerConfig { DirectoryBrowse = new DirectoryBrowse { Enabled = true } });
            BuildAndCommit(new ServerConfig { DirectoryBrowse = new DirectoryBrowse { Enabled = false } });

            Assert.That(Attribute(WebServer().Element("directoryBrowse"), "enabled"), Is.EqualTo("false"));
        }

        private void BuildAndCommit(ServerConfig config)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ServerConfigBuilder(config, manager, _configPath);

                Assert.That(builder.Build(out string error), Is.True, error);
                manager.CommitChanges();
            }
        }

        private XElement WebServer()
        {
            XElement webServer = XDocument.Load(_configPath).Root?.Element("system.webServer");

            Assert.That(webServer, Is.Not.Null, "system.webServer is missing");
            return webServer;
        }

        private static string Attribute(XElement element, string name)
        {
            Assert.That(element, Is.Not.Null, "the section is missing");

            XAttribute attribute = element.Attribute(name);

            Assert.That(attribute, Is.Not.Null, $"<{element.Name}> has no '{name}' attribute");
            return attribute.Value;
        }
    }
}
