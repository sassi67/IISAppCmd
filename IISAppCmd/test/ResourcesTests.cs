using System;
using System.IO;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class ResourcesTests
    {
        [Test]
        public void ApplicationHostConfig_ExistsInResources()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "applicationHost.config");
            Assert.That(File.Exists(path), Is.True, $"Expected resource file at '{path}'");
        }

        [Test]
        public void IISAgentConfigSchema_ExistsInResources()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "IISAgentConfigSchema.xml");
            Assert.That(File.Exists(path), Is.True, $"Expected resource file at '{path}'");
        }
    }
}
