using System;
using System.Linq;
using IISAppCmd.CommandLine;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class SiteFactoryTests
    {
        [Test]
        public void FromCommandLine_NamesSiteAfterRunId()
        {
            Site site = SiteFactory.FromCommandLine(Options(), "7430f109");

            Assert.That(site.Name, Is.EqualTo("Site_7430f109"));
        }

        [Test]
        public void FromCommandLine_MirrorsReferenceToolValues()
        {
            var options = Options();
            options.Port = 8080;

            Site site = SiteFactory.FromCommandLine(options, "abc12345");

            Assert.Multiple(() =>
            {
                Assert.That(site.ServerAutoStart, Is.True);
                Assert.That(site.Id, Is.Zero, "the site id is left to IIS");
                Assert.That(site.Bindings.Count, Is.EqualTo(1));
                Assert.That(site.Bindings[0].Protocol, Is.EqualTo("http"));
                Assert.That(site.Bindings[0].BindingInformation, Is.EqualTo("*:8080:localhost"));
                Assert.That(site.Bindings[0].SslFlags, Is.Zero);
            });
        }

        [Test]
        public void FromCommandLine_ServesTheApplicationPathFromTheRoot()
        {
            Site site = SiteFactory.FromCommandLine(Options(), "abc12345");

            Assert.That(site.Applications.Count, Is.EqualTo(1));
            SiteApplication application = site.Applications[0];

            Assert.Multiple(() =>
            {
                Assert.That(application.Path, Is.EqualTo("/"));
                Assert.That(application.ApplicationPool, Is.Null, "the pool is filled in by the builder");
                Assert.That(application.VirtualDirectories.Count, Is.EqualTo(1));
                Assert.That(application.VirtualDirectories[0].Path, Is.EqualTo("/"));
                Assert.That(application.VirtualDirectories[0].PhysicalPath, Is.EqualTo(@"C:\apps\scratch"));
            });
        }

        [Test]
        public void FromCommandLine_UsesTheDefaultPortWhenNoneWasGiven()
        {
            Site site = SiteFactory.FromCommandLine(Options(), "abc12345");

            Assert.That(site.Bindings[0].BindingInformation, Is.EqualTo("*:5001:localhost"));
        }

        [TestCase(5001, "*:5001:localhost")]
        [TestCase(8080, "*:8080:localhost")]
        public void BindingInformationFor_BindsLocalhostOnly(int port, string expected)
        {
            Assert.That(SiteFactory.BindingInformationFor(port), Is.EqualTo(expected));
        }

        [Test]
        public void FromCommandLine_RejectsMissingArguments()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => SiteFactory.FromCommandLine(null, "abc12345"), Throws.ArgumentNullException);
                Assert.That(() => SiteFactory.FromCommandLine(Options(), ""), Throws.ArgumentException);
                Assert.That(() => SiteFactory.FromCommandLine(Options(), null), Throws.ArgumentException);
            });
        }

        [Test]
        public void FromCommandLine_AddsOneApplicationPerAdditionalApplication()
        {
            var options = Options();
            options.AdditionalApplications.Add(new AdditionalApplication
            {
                Path = "/second",
                PhysicalPath = @"C:\apps\second",
                ApplicationPool = "SecondPool",
            });
            options.AdditionalApplications.Add(new AdditionalApplication
            {
                Path = "/third",
                PhysicalPath = @"C:\apps\third",
            });

            Site site = SiteFactory.FromCommandLine(options, "abc12345");

            Assert.That(site.Applications.Count, Is.EqualTo(3));

            SiteApplication second = site.Applications[1];
            SiteApplication third = site.Applications[2];

            Assert.Multiple(() =>
            {
                Assert.That(second.Path, Is.EqualTo("/second"));
                Assert.That(second.ApplicationPool, Is.EqualTo("SecondPool"));
                Assert.That(second.VirtualDirectories.Single().Path, Is.EqualTo("/second"));
                Assert.That(second.VirtualDirectories.Single().PhysicalPath, Is.EqualTo(@"C:\apps\second"));
                Assert.That(third.ApplicationPool, Is.Null, "the pool is filled in by the builder when --app names none");
                Assert.That(third.VirtualDirectories.Single().PhysicalPath, Is.EqualTo(@"C:\apps\third"));
            });
        }

        [Test]
        public void TryAttachVirtualDirectories_AttachesEachToTheApplicationItsParentPathNames()
        {
            var options = Options();
            options.AdditionalApplications.Add(new AdditionalApplication { Path = "/second", PhysicalPath = @"C:\apps\second" });
            options.AdditionalVirtualDirectories.Add(new AdditionalVirtualDirectory
            {
                ParentPath = "/",
                Path = "/jakarta",
                PhysicalPath = @"C:\apps\jakarta",
            });
            options.AdditionalVirtualDirectories.Add(new AdditionalVirtualDirectory
            {
                ParentPath = "/second",
                Path = "/second/extra",
                PhysicalPath = @"C:\apps\second\extra",
            });

            Site site = SiteFactory.FromCommandLine(options, "abc12345");

            Assert.That(SiteFactory.TryAttachVirtualDirectories(site, options, out string error), Is.True);
            Assert.That(error, Is.Empty);

            SiteApplication root = site.Applications[0];
            SiteApplication second = site.Applications[1];

            Assert.Multiple(() =>
            {
                Assert.That(root.VirtualDirectories.Select(d => d.Path), Is.EqualTo(new[] { "/", "/jakarta" }));
                Assert.That(root.VirtualDirectories.Last().PhysicalPath, Is.EqualTo(@"C:\apps\jakarta"));
                Assert.That(second.VirtualDirectories.Select(d => d.Path), Is.EqualTo(new[] { "/second", "/second/extra" }));
            });
        }

        [Test]
        public void TryAttachVirtualDirectories_FailsWhenTheParentPathNamesNoApplication()
        {
            var options = Options();
            options.AdditionalVirtualDirectories.Add(new AdditionalVirtualDirectory
            {
                ParentPath = "/no-such-app",
                Path = "/jakarta",
                PhysicalPath = @"C:\apps\jakarta",
            });

            Site site = SiteFactory.FromCommandLine(options, "abc12345");

            Assert.That(SiteFactory.TryAttachVirtualDirectories(site, options, out string error), Is.False);
            Assert.That(error, Does.Contain("/no-such-app"));
        }

        [Test]
        public void TryAttachVirtualDirectories_RejectsMissingArguments()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => SiteFactory.TryAttachVirtualDirectories(null, Options(), out _), Throws.ArgumentNullException);
                Assert.That(() => SiteFactory.TryAttachVirtualDirectories(SiteFactory.FromCommandLine(Options(), "abc12345"), null, out _), Throws.ArgumentNullException);
            });
        }

        private static CommandLineOptions Options() => new CommandLineOptions
        {
            Application = "Scratch",
            ApplicationPath = @"C:\apps\scratch",
        };
    }
}
