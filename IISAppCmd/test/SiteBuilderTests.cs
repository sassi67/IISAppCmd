using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IISAppCmd.CommandLine;
using IISAppCmd.Config;
using IISAppCmd.IIS;
using NUnit.Framework;
using ServerManager = Microsoft.Web.Administration.ServerManager;

namespace IISAppCmd.Tests
{
    // Writes through Microsoft.Web.Administration into a private copy of the
    // bundled applicationHost.config and checks the resulting XML. IIS Express
    // is never started.
    public class SiteBuilderTests
    {
        private const string PhysicalPath = @"C:\apps\scratch";

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
        public void Build_WritesSiteFromCommandLine()
        {
            var options = new CommandLineOptions
            {
                Application = "Scratch",
                ApplicationPath = PhysicalPath,
                Port = 8080,
            };

            ApplicationPool pool = ApplicationPoolFactory.FromCommandLine(options, "7430f109");
            Site site = SiteFactory.FromCommandLine(options, "7430f109");

            BuildAndCommit(site, pool);

            XElement written = FindSite("Site_7430f109");
            XElement binding = written.Element("bindings").Elements("binding").Single();
            XElement application = written.Elements("application").Single();
            XElement directory = application.Elements("virtualDirectory").Single();

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(written, "serverAutoStart"), Is.EqualTo("true"));
                Assert.That(Attribute(binding, "protocol"), Is.EqualTo("http"));
                Assert.That(Attribute(binding, "bindingInformation"), Is.EqualTo("*:8080:localhost"));
                Assert.That(Attribute(application, "path"), Is.EqualTo("/"));
                Assert.That(Attribute(application, "applicationPool"), Is.EqualTo("AppPool_7430f109"));
                Assert.That(Attribute(directory, "path"), Is.EqualTo("/"));
                Assert.That(Attribute(directory, "physicalPath"), Is.EqualTo(PhysicalPath));
            });
        }

        [Test]
        public void Build_WithoutAnId_LetsIisPickOne()
        {
            BuildAndCommit(NewSite("Site_auto"));

            Assert.That(Attribute(FindSite("Site_auto"), "id"), Is.Not.EqualTo("1"), "the Default Web Site already holds id 1");
        }

        [Test]
        public void Build_WithAnId_WritesIt()
        {
            Site site = NewSite("Site_id");
            site.Id = 42;
            site.ServerAutoStart = false;

            BuildAndCommit(site);

            XElement written = FindSite("Site_id");
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(written, "id"), Is.EqualTo("42"));
                Assert.That(Attribute(written, "serverAutoStart"), Is.EqualTo("false"));
            });
        }

        [Test]
        public void Build_WritesEveryBinding()
        {
            Site site = NewSite("Site_bindings");
            site.Bindings.Clear();
            site.Bindings.Add(new SiteBinding { Protocol = "http", BindingInformation = "*:5001:localhost" });
            site.Bindings.Add(new SiteBinding { Protocol = "https", BindingInformation = "*:5002:localhost", SslFlags = 1 });

            BuildAndCommit(site);

            var bindings = FindSite("Site_bindings").Element("bindings").Elements("binding")
                .Select(e => new
                {
                    Protocol = Attribute(e, "protocol"),
                    Information = Attribute(e, "bindingInformation"),
                    SslFlags = Attribute(e, "sslFlags"),
                })
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(bindings.Count, Is.EqualTo(2));
                Assert.That(bindings[0].Protocol, Is.EqualTo("http"));
                Assert.That(bindings[0].Information, Is.EqualTo("*:5001:localhost"));
                Assert.That(bindings[0].SslFlags, Is.EqualTo("0"));
                Assert.That(bindings[1].Protocol, Is.EqualTo("https"));
                Assert.That(bindings[1].Information, Is.EqualTo("*:5002:localhost"));
                Assert.That(bindings[1].SslFlags, Is.EqualTo("1"));
            });
        }

        [Test]
        public void Build_WritesLimits()
        {
            Site site = NewSite("Site_limits");
            site.Limits.MaxBandwidth = 1048576;
            site.Limits.MaxConnections = 4096;
            site.Limits.ConnectionTimeout = TimeSpan.FromSeconds(90);
            site.Limits.MaxUrlSegments = 64;

            BuildAndCommit(site);

            XElement limits = FindSite("Site_limits").Element("limits");
            Assert.That(limits, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(limits, "maxBandwidth"), Is.EqualTo("1048576"));
                Assert.That(Attribute(limits, "maxConnections"), Is.EqualTo("4096"));
                Assert.That(Attribute(limits, "connectionTimeout"), Is.EqualTo("00:01:30"));
                Assert.That(Attribute(limits, "maxUrlSegments"), Is.EqualTo("64"));
            });
        }

        [Test]
        public void Build_WritesLogFileAndItsCustomFields()
        {
            Site site = NewSite("Site_log");
            site.LogFile.LogExtFileFlags = LogExtFileFlags.Date | LogExtFileFlags.Time;
            site.LogFile.CustomLogPluginClsid = "";
            site.LogFile.LogFormat = LogFormat.Custom;
            site.LogFile.LogTargetW3C = LogTargetW3C.File | LogTargetW3C.ETW;
            site.LogFile.Directory = @"C:\logs";
            site.LogFile.Period = LogPeriod.Hourly;
            site.LogFile.TruncateSize = 10485760;
            site.LogFile.LocalTimeRollover = true;
            site.LogFile.Enabled = false;
            site.LogFile.LogSiteId = false;
            site.LogFile.FlushByEntryCountW3CLog = 10;
            site.LogFile.MaxLogLineLength = 4096;
            site.LogFile.CustomFields.MaxCustomFieldLength = 2048;
            site.LogFile.CustomFields.Fields.Add(new SiteLogCustomField
            {
                LogFieldName = "Referrer",
                SourceName = "Referer",
                SourceType = CustomLogFieldSourceType.RequestHeader,
            });

            BuildAndCommit(site);

            XElement logFile = FindSite("Site_log").Element("logFile");
            Assert.That(logFile, Is.Not.Null);
            XElement customFields = logFile.Element("customFields");
            Assert.That(customFields, Is.Not.Null);
            XElement field = customFields.Elements("add").Single();

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(logFile, "logExtFileFlags"), Is.EqualTo("Date, Time"));
                Assert.That(Attribute(logFile, "customLogPluginClsid"), Is.EqualTo(""));
                Assert.That(Attribute(logFile, "logFormat"), Is.EqualTo("Custom"));
                Assert.That(Attribute(logFile, "logTargetW3C"), Is.EqualTo("File, ETW"));
                Assert.That(Attribute(logFile, "directory"), Is.EqualTo(@"C:\logs"));
                Assert.That(Attribute(logFile, "period"), Is.EqualTo("Hourly"));
                Assert.That(Attribute(logFile, "truncateSize"), Is.EqualTo("10485760"));
                Assert.That(Attribute(logFile, "localTimeRollover"), Is.EqualTo("true"));
                Assert.That(Attribute(logFile, "enabled"), Is.EqualTo("false"));
                Assert.That(Attribute(logFile, "logSiteId"), Is.EqualTo("false"));
                Assert.That(Attribute(logFile, "flushByEntryCountW3CLog"), Is.EqualTo("10"));
                Assert.That(Attribute(logFile, "maxLogLineLength"), Is.EqualTo("4096"));
                Assert.That(Attribute(customFields, "maxCustomFieldLength"), Is.EqualTo("2048"));
                Assert.That(Attribute(field, "logFieldName"), Is.EqualTo("Referrer"));
                Assert.That(Attribute(field, "sourceName"), Is.EqualTo("Referer"));
                Assert.That(Attribute(field, "sourceType"), Is.EqualTo("RequestHeader"));
            });
        }

        [Test]
        public void Build_WritesTraceFailedRequestsLogging()
        {
            Site site = NewSite("Site_trace");
            site.TraceFailedRequestsLogging.Enabled = true;
            site.TraceFailedRequestsLogging.Directory = @"C:\traces";
            site.TraceFailedRequestsLogging.MaxLogFiles = 10;
            site.TraceFailedRequestsLogging.MaxLogFileSizeKB = 2048;
            site.TraceFailedRequestsLogging.CustomActionsEnabled = true;

            BuildAndCommit(site);

            XElement tracing = FindSite("Site_trace").Element("traceFailedRequestsLogging");
            Assert.That(tracing, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(tracing, "enabled"), Is.EqualTo("true"));
                Assert.That(Attribute(tracing, "directory"), Is.EqualTo(@"C:\traces"));
                Assert.That(Attribute(tracing, "maxLogFiles"), Is.EqualTo("10"));
                Assert.That(Attribute(tracing, "maxLogFileSizeKB"), Is.EqualTo("2048"));
                Assert.That(Attribute(tracing, "customActionsEnabled"), Is.EqualTo("true"));
            });
        }

        [Test]
        public void Build_WritesHsts()
        {
            Site site = NewSite("Site_hsts");
            site.Hsts.Enabled = true;
            site.Hsts.MaxAge = 31536000;
            site.Hsts.IncludeSubDomains = true;
            site.Hsts.Preload = true;
            site.Hsts.RedirectHttpToHttps = true;

            BuildAndCommit(site);

            XElement hsts = FindSite("Site_hsts").Element("hsts");
            Assert.That(hsts, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(hsts, "enabled"), Is.EqualTo("true"));
                Assert.That(Attribute(hsts, "max-age"), Is.EqualTo("31536000"));
                Assert.That(Attribute(hsts, "includeSubDomains"), Is.EqualTo("true"));
                Assert.That(Attribute(hsts, "preload"), Is.EqualTo("true"));
                Assert.That(Attribute(hsts, "redirectHttpToHttps"), Is.EqualTo("true"));
            });
        }

        [Test]
        public void Build_WritesTheDefaultsOfTheSite()
        {
            Site site = NewSite("Site_defaults");
            site.ApplicationDefaults.ApplicationPool = "DefaultAppPool";
            site.ApplicationDefaults.EnabledProtocols = "http,https";
            site.ApplicationDefaults.ServiceAutoStartEnabled = true;
            site.ApplicationDefaults.ServiceAutoStartProvider = "Warmup";
            site.ApplicationDefaults.PreloadEnabled = true;
            site.VirtualDirectoryDefaults.PhysicalPath = @"C:\apps";
            site.VirtualDirectoryDefaults.UserName = @"DOMAIN\user";
            site.VirtualDirectoryDefaults.LogonMethod = LogonMethod.Network;
            site.VirtualDirectoryDefaults.AllowSubDirConfig = false;

            BuildAndCommit(site);

            XElement written = FindSite("Site_defaults");
            XElement applicationDefaults = written.Element("applicationDefaults");
            XElement directoryDefaults = written.Element("virtualDirectoryDefaults");
            Assert.That(applicationDefaults, Is.Not.Null);
            Assert.That(directoryDefaults, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(applicationDefaults, "applicationPool"), Is.EqualTo("DefaultAppPool"));
                Assert.That(Attribute(applicationDefaults, "enabledProtocols"), Is.EqualTo("http,https"));
                Assert.That(Attribute(applicationDefaults, "serviceAutoStartEnabled"), Is.EqualTo("true"));
                Assert.That(Attribute(applicationDefaults, "serviceAutoStartProvider"), Is.EqualTo("Warmup"));
                Assert.That(Attribute(applicationDefaults, "preloadEnabled"), Is.EqualTo("true"));
                Assert.That(Attribute(directoryDefaults, "physicalPath"), Is.EqualTo(@"C:\apps"));
                Assert.That(Attribute(directoryDefaults, "userName"), Is.EqualTo(@"DOMAIN\user"));
                Assert.That(Attribute(directoryDefaults, "logonMethod"), Is.EqualTo("Network"));
                Assert.That(Attribute(directoryDefaults, "allowSubDirConfig"), Is.EqualTo("false"));
            });
        }

        [Test]
        public void Build_WritesEveryApplicationAndVirtualDirectory()
        {
            Site site = NewSite("Site_apps");
            site.Applications[0].EnabledProtocols = "http";
            site.Applications[0].VirtualDirectories.Add(new SiteVirtualDirectory
            {
                Path = "/assets",
                PhysicalPath = @"C:\apps\assets",
                AllowSubDirConfig = false,
            });
            site.Applications.Add(new SiteApplication
            {
                Path = "/api",
                ApplicationPool = "DefaultAppPool",
                PreloadEnabled = true,
                VirtualDirectories =
                {
                    new SiteVirtualDirectory { Path = "/", PhysicalPath = @"C:\apps\api" },
                },
            });

            BuildAndCommit(site);

            XElement written = FindSite("Site_apps");
            XElement root = FindApplication(written, "/");
            XElement api = FindApplication(written, "/api");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(root, "applicationPool"), Is.EqualTo("AppPool_tests"), "an application naming no pool runs in the one this run created");
                Assert.That(Attribute(root, "enabledProtocols"), Is.EqualTo("http"));
                Assert.That(root.Elements("virtualDirectory").Select(e => Attribute(e, "path")), Is.EqualTo(new[] { "/", "/assets" }));
                Assert.That(Attribute(root.Elements("virtualDirectory").Last(), "physicalPath"), Is.EqualTo(@"C:\apps\assets"));
                Assert.That(Attribute(root.Elements("virtualDirectory").Last(), "allowSubDirConfig"), Is.EqualTo("false"));
                Assert.That(Attribute(api, "applicationPool"), Is.EqualTo("DefaultAppPool"), "an application naming a pool keeps it");
                Assert.That(Attribute(api, "preloadEnabled"), Is.EqualTo("true"));
                Assert.That(Attribute(api.Elements("virtualDirectory").Single(), "physicalPath"), Is.EqualTo(@"C:\apps\api"));
            });
        }

        [Test]
        public void Build_KeepsExistingSitesAndDefaults()
        {
            BuildAndCommit(NewSite("Site_keep"));

            XElement sites = Sites();
            Assert.Multiple(() =>
            {
                Assert.That(sites.Elements("site").Select(e => Attribute(e, "name")), Is.EquivalentTo(new[] { "Default Web Site", "Site_keep" }));
                Assert.That(Attribute(sites.Element("applicationDefaults"), "applicationPool"), Is.EqualTo("DefaultAppPool"));
                Assert.That(sites.Element("siteDefaults"), Is.Not.Null);
            });
        }

        [Test]
        public void Build_Twice_UpdatesTheSameSite()
        {
            BuildAndCommit(NewSite("Site_twice"));

            Site updated = NewSite("Site_twice");
            updated.Bindings[0].BindingInformation = "*:9000:localhost";
            updated.Applications[0].VirtualDirectories[0].PhysicalPath = @"C:\apps\other";
            BuildAndCommit(updated);

            var matches = Sites().Elements("site").Where(e => Attribute(e, "name") == "Site_twice").ToList();
            Assert.Multiple(() =>
            {
                Assert.That(matches.Count, Is.EqualTo(1));
                Assert.That(Attribute(matches[0].Element("bindings").Elements("binding").Single(), "bindingInformation"), Is.EqualTo("*:9000:localhost"));
                Assert.That(Attribute(matches[0].Elements("application").Single().Elements("virtualDirectory").Single(), "physicalPath"), Is.EqualTo(@"C:\apps\other"));
            });
        }

        [Test]
        public void Build_UncommittedChanges_DoNotReachTheFile()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new SiteBuilder(NewSite("Site_uncommitted"), manager, Pool(), _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
            }

            Assert.That(Sites().Elements("site").Select(e => Attribute(e, "name")), Is.EqualTo(new[] { "Default Web Site" }));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public void Build_SiteWithoutName_Fails(string name)
        {
            string error = AssertFails(NewSite(name), "the site has no name");

            Assert.That(error, Does.Contain(_configPath));
        }

        [Test]
        public void Build_PoolWithoutName_Fails()
        {
            AssertFails(NewSite("Site_nopool"), "application pool of the site 'Site_nopool' has no name", new ApplicationPool());
        }

        [Test]
        public void Build_SiteWithoutBinding_Fails()
        {
            Site site = NewSite("Site_nobinding");
            site.Bindings.Clear();

            AssertFails(site, "the site 'Site_nobinding' has no binding.");
        }

        [Test]
        public void Build_BindingWithoutProtocol_Fails()
        {
            Site site = NewSite("Site_badbinding");
            site.Bindings[0].Protocol = "";

            AssertFails(site, "binding #1 of the site 'Site_badbinding' has no protocol.");
        }

        [Test]
        public void Build_BindingWithoutInformation_Fails()
        {
            Site site = NewSite("Site_badbinding");
            site.Bindings[0].BindingInformation = null;

            AssertFails(site, "binding #1 of the site 'Site_badbinding' has no binding information.");
        }

        [Test]
        public void Build_SiteWithoutApplication_Fails()
        {
            Site site = NewSite("Site_noapp");
            site.Applications.Clear();

            AssertFails(site, "the site 'Site_noapp' has no application.");
        }

        [Test]
        public void Build_ApplicationWithoutPath_Fails()
        {
            Site site = NewSite("Site_badapp");
            site.Applications[0].Path = null;

            AssertFails(site, "application #1 of the site 'Site_badapp' has no path.");
        }

        [Test]
        public void Build_ApplicationWithoutVirtualDirectory_Fails()
        {
            Site site = NewSite("Site_novdir");
            site.Applications[0].VirtualDirectories.Clear();

            AssertFails(site, "the application '/' of the site 'Site_novdir' has no virtual directory.");
        }

        [Test]
        public void Build_VirtualDirectoryWithoutPath_Fails()
        {
            Site site = NewSite("Site_badvdir");
            site.Applications[0].VirtualDirectories[0].Path = "";

            AssertFails(site, "a virtual directory of the application '/' of the site 'Site_badvdir' has no path.");
        }

        [Test]
        public void Build_VirtualDirectoryWithoutPhysicalPath_Fails()
        {
            Site site = NewSite("Site_badvdir");
            site.Applications[0].VirtualDirectories[0].PhysicalPath = null;

            AssertFails(site, "the virtual directory '/' of the application '/' of the site 'Site_badvdir' has no physical path.");
        }

        [Test]
        public void Constructor_ExposesConfigPath()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new SiteBuilder(NewSite("Site_x"), manager, Pool(), _configPath);

                Assert.That(builder.ConfigPath, Is.EqualTo(_configPath));
            }
        }

        [Test]
        public void Constructor_RejectsMissingArguments()
        {
            using (var manager = new ServerManager(_configPath))
            {
                Assert.Multiple(() =>
                {
                    Assert.That(() => new SiteBuilder(null, manager, Pool(), _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new SiteBuilder(new Site(), null, Pool(), _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new SiteBuilder(new Site(), manager, null, _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new SiteBuilder(new Site(), manager, Pool(), ""), Throws.ArgumentException);
                    Assert.That(() => new SiteBuilder(new Site(), manager, Pool(), Path.Combine(_scratch, "missing.config")), Throws.TypeOf<FileNotFoundException>());
                });
            }
        }

        /// <summary>The smallest site the builder accepts.</summary>
        private static Site NewSite(string name) => new Site
        {
            Name = name,
            Bindings =
            {
                new SiteBinding { Protocol = "http", BindingInformation = "*:5001:localhost" },
            },
            Applications =
            {
                new SiteApplication
                {
                    Path = "/",
                    VirtualDirectories =
                    {
                        new SiteVirtualDirectory { Path = "/", PhysicalPath = PhysicalPath },
                    },
                },
            },
        };

        private static ApplicationPool Pool() => new ApplicationPool { Name = "AppPool_tests" };

        private void BuildAndCommit(Site site, ApplicationPool pool = null)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new SiteBuilder(site, manager, pool ?? Pool(), _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
                manager.CommitChanges();
            }
        }

        /// <summary>Builds and expects a refusal that names the culprit; returns the message.</summary>
        private string AssertFails(Site site, string expected, ApplicationPool pool = null)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new SiteBuilder(site, manager, pool ?? Pool(), _configPath);

                bool ok = builder.Build(out string error);

                Assert.Multiple(() =>
                {
                    Assert.That(ok, Is.False);
                    Assert.That(error, Does.Contain(expected));
                    Assert.That(manager.Sites.Count, Is.EqualTo(1), "nothing is added when validation fails");
                });

                return error;
            }
        }

        private XElement Sites()
        {
            XElement sites = XDocument.Load(_configPath).Root?
                .Element("system.applicationHost")?
                .Element("sites");

            Assert.That(sites, Is.Not.Null, "sites section is missing");
            return sites;
        }

        private XElement FindSite(string name)
        {
            XElement site = Sites().Elements("site").SingleOrDefault(e => Attribute(e, "name") == name);

            Assert.That(site, Is.Not.Null, $"site '{name}' was not written");
            return site;
        }

        private static XElement FindApplication(XElement site, string path)
        {
            XElement application = site.Elements("application").SingleOrDefault(e => Attribute(e, "path") == path);

            Assert.That(application, Is.Not.Null, $"application '{path}' was not written");
            return application;
        }

        private static string Attribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);

            Assert.That(attribute, Is.Not.Null, $"<{element.Name}> has no '{name}' attribute");
            return attribute.Value;
        }
    }
}
