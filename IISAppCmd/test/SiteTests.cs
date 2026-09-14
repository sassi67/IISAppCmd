using System;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class SiteTests
    {
        private static readonly Type[] ModelTypes =
        {
            typeof(Site),
            typeof(SiteBinding),
            typeof(SiteLimits),
            typeof(SiteLogFile),
            typeof(SiteLogCustomFields),
            typeof(SiteLogCustomField),
            typeof(SiteTraceFailedRequestsLogging),
            typeof(SiteHsts),
            typeof(SiteApplication),
            typeof(SiteApplicationDefaults),
            typeof(SiteVirtualDirectory),
        };

        [TestCaseSource(nameof(ModelTypes))]
        public void AllPublicProperties_HavePublicGetterAndSetter(Type type)
        {
            PropertyAssert.AllHavePublicGetterAndSetter(type);
        }

        [TestCaseSource(nameof(ModelTypes))]
        public void AllPublicProperties_ReturnAssignedValue(Type type)
        {
            PropertyAssert.AllReturnAssignedValue(type);
        }

        [Test]
        public void Site_HasIisSchemaDefaults()
        {
            var site = new Site();

            Assert.Multiple(() =>
            {
                Assert.That(site.Name, Is.Null);
                Assert.That(site.Id, Is.EqualTo(0u));
                Assert.That(site.ServerAutoStart, Is.True);
                Assert.That(site.Bindings, Is.Empty);
                Assert.That(site.Limits, Is.Not.Null);
                Assert.That(site.LogFile, Is.Not.Null);
                Assert.That(site.LogFile.CustomFields, Is.Not.Null);
                Assert.That(site.TraceFailedRequestsLogging, Is.Not.Null);
                Assert.That(site.Hsts, Is.Not.Null);
                Assert.That(site.ApplicationDefaults, Is.Not.Null);
                Assert.That(site.VirtualDirectoryDefaults, Is.Not.Null);
                Assert.That(site.Applications, Is.Empty);
            });
        }

        [Test]
        public void Binding_HasIisSchemaDefaults()
        {
            var binding = new SiteBinding();

            Assert.Multiple(() =>
            {
                Assert.That(binding.Protocol, Is.Null);
                Assert.That(binding.BindingInformation, Is.Null);
                Assert.That(binding.SslFlags, Is.EqualTo(0u));
            });
        }

        [Test]
        public void Limits_HasIisSchemaDefaults()
        {
            var limits = new SiteLimits();

            Assert.Multiple(() =>
            {
                Assert.That(limits.MaxBandwidth, Is.EqualTo(4294967295u));
                Assert.That(limits.MaxConnections, Is.EqualTo(4294967295u));
                Assert.That(limits.ConnectionTimeout, Is.EqualTo(TimeSpan.Parse("00:02:00")));
                Assert.That(limits.MaxUrlSegments, Is.EqualTo(32u));
            });
        }

        [Test]
        public void LogFile_HasIisSchemaDefaults()
        {
            var logFile = new SiteLogFile();

            Assert.Multiple(() =>
            {
                Assert.That(logFile.LogExtFileFlags, Is.EqualTo((LogExtFileFlags)2490319));
                Assert.That(logFile.CustomLogPluginClsid, Is.EqualTo(""));
                Assert.That(logFile.LogFormat, Is.EqualTo(LogFormat.W3C));
                Assert.That(logFile.LogTargetW3C, Is.EqualTo(LogTargetW3C.File));
                Assert.That(logFile.Directory, Is.EqualTo(@"%SystemDrive%\inetpub\logs\LogFiles"));
                Assert.That(logFile.Period, Is.EqualTo(LogPeriod.Daily));
                Assert.That(logFile.TruncateSize, Is.EqualTo(20971520L));
                Assert.That(logFile.LocalTimeRollover, Is.False);
                Assert.That(logFile.Enabled, Is.True);
                Assert.That(logFile.LogSiteId, Is.True);
                Assert.That(logFile.FlushByEntryCountW3CLog, Is.EqualTo(0u));
                Assert.That(logFile.MaxLogLineLength, Is.EqualTo(65536u));
                Assert.That(logFile.CustomFields.MaxCustomFieldLength, Is.EqualTo(4096u));
                Assert.That(logFile.CustomFields.Fields, Is.Empty);
            });
        }

        [Test]
        public void TraceFailedRequestsLogging_HasIisSchemaDefaults()
        {
            var tracing = new SiteTraceFailedRequestsLogging();

            Assert.Multiple(() =>
            {
                Assert.That(tracing.Enabled, Is.False);
                Assert.That(tracing.Directory, Is.EqualTo(@"%SystemDrive%\inetpub\logs\FailedReqLogFiles"));
                Assert.That(tracing.MaxLogFiles, Is.EqualTo(50u));
                Assert.That(tracing.MaxLogFileSizeKB, Is.EqualTo(1024u));
                Assert.That(tracing.CustomActionsEnabled, Is.False);
            });
        }

        [Test]
        public void Hsts_HasIisSchemaDefaults()
        {
            var hsts = new SiteHsts();

            Assert.Multiple(() =>
            {
                Assert.That(hsts.Enabled, Is.False);
                Assert.That(hsts.MaxAge, Is.EqualTo(0u));
                Assert.That(hsts.IncludeSubDomains, Is.False);
                Assert.That(hsts.Preload, Is.False);
                Assert.That(hsts.RedirectHttpToHttps, Is.False);
            });
        }

        [Test]
        public void Application_HasIisSchemaDefaults()
        {
            var application = new SiteApplication();

            Assert.Multiple(() =>
            {
                Assert.That(application.Path, Is.Null);
                Assert.That(application.ApplicationPool, Is.Null);
                Assert.That(application.EnabledProtocols, Is.EqualTo("http"));
                Assert.That(application.ServiceAutoStartEnabled, Is.False);
                Assert.That(application.ServiceAutoStartProvider, Is.Null);
                Assert.That(application.PreloadEnabled, Is.False);
                Assert.That(application.VirtualDirectoryDefaults, Is.Not.Null);
                Assert.That(application.VirtualDirectories, Is.Empty);
            });
        }

        [Test]
        public void ApplicationDefaults_HasIisSchemaDefaults()
        {
            var defaults = new SiteApplicationDefaults();

            Assert.Multiple(() =>
            {
                Assert.That(defaults.Path, Is.Null);
                Assert.That(defaults.ApplicationPool, Is.Null);
                Assert.That(defaults.EnabledProtocols, Is.EqualTo("http"));
                Assert.That(defaults.ServiceAutoStartEnabled, Is.False);
                Assert.That(defaults.ServiceAutoStartProvider, Is.Null);
                Assert.That(defaults.PreloadEnabled, Is.False);
            });
        }

        [Test]
        public void VirtualDirectory_HasIisSchemaDefaults()
        {
            var virtualDirectory = new SiteVirtualDirectory();

            Assert.Multiple(() =>
            {
                Assert.That(virtualDirectory.Path, Is.Null);
                Assert.That(virtualDirectory.PhysicalPath, Is.Null);
                Assert.That(virtualDirectory.UserName, Is.Null);
                Assert.That(virtualDirectory.Password, Is.Null);
                Assert.That(virtualDirectory.LogonMethod, Is.EqualTo(LogonMethod.ClearText));
                Assert.That(virtualDirectory.AllowSubDirConfig, Is.True);
            });
        }
    }
}
