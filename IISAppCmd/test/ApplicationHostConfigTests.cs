using System;
using System.IO;
using IISAppCmd.Config;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class ApplicationHostConfigTests
    {
        private string _scratch;

        [SetUp]
        public void SetUp()
        {
            _scratch = Path.Combine(Path.GetTempPath(), "IISAppCmd.Tests", Guid.NewGuid().ToString("N"));
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
        public void BundledPath_PointsAtResourceNextToExecutable()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ApplicationHostConfig.BundledPath, Does.StartWith(AppContext.BaseDirectory));
                Assert.That(ApplicationHostConfig.BundledPath, Does.EndWith(@"Resources\applicationHost.config"));
                Assert.That(File.Exists(ApplicationHostConfig.BundledPath), Is.True);
            });
        }

        [Test]
        public void DefaultWorkingCopyPath_UsesReferenceToolLayout()
        {
            string path = ApplicationHostConfig.DefaultWorkingCopyPath("7430f109");

            Assert.That(path, Is.EqualTo(Path.Combine(Path.GetTempPath(), "iisconfig", "applicationhost-7430f109.config")));
        }

        [TestCase("")]
        [TestCase(null)]
        public void DefaultWorkingCopyPath_EmptyId_Throws(string id)
        {
            Assert.That(() => ApplicationHostConfig.DefaultWorkingCopyPath(id), Throws.ArgumentException);
        }

        [Test]
        public void CreateWorkingCopy_CopiesBundledFileIntoNewDirectory()
        {
            string destination = Path.Combine(_scratch, "nested", "applicationhost.config");

            bool ok = ApplicationHostConfig.CreateWorkingCopy(ApplicationHostConfig.BundledPath, destination, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True, error);
                Assert.That(error, Is.Empty);
                Assert.That(File.ReadAllBytes(destination), Is.EqualTo(File.ReadAllBytes(ApplicationHostConfig.BundledPath)));
                Assert.That(File.GetLastWriteTimeUtc(destination), Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            });
        }

        [Test]
        public void CreateWorkingCopy_DoesNotOverwriteExistingDestination()
        {
            Directory.CreateDirectory(_scratch);
            string destination = Path.Combine(_scratch, "applicationhost.config");
            File.WriteAllText(destination, "keep me");

            bool ok = ApplicationHostConfig.CreateWorkingCopy(ApplicationHostConfig.BundledPath, destination, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(error, Does.Contain("already exists"));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("keep me"));
            });
        }

        [Test]
        public void CreateWorkingCopy_MissingSource_Fails()
        {
            string source = Path.Combine(_scratch, "missing.config");
            string destination = Path.Combine(_scratch, "applicationhost.config");

            bool ok = ApplicationHostConfig.CreateWorkingCopy(source, destination, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(error, Does.Contain("missing"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }

        [TestCase("", "x")]
        [TestCase(null, "x")]
        [TestCase("x", "")]
        [TestCase("x", null)]
        public void CreateWorkingCopy_EmptyPath_Fails(string source, string destination)
        {
            bool ok = ApplicationHostConfig.CreateWorkingCopy(source, destination, out string error);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.False);
                Assert.That(error, Does.Contain("empty"));
            });
        }
    }
}
