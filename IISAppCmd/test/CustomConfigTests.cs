using System.IO;
using System.Linq;
using System.Xml.Linq;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class CustomConfigTests
    {
        [Test]
        public void AllPublicProperties_HavePublicGetterAndSetter()
        {
            PropertyAssert.AllHavePublicGetterAndSetter(typeof(CustomConfig));
        }

        [Test]
        public void AllPublicProperties_ReturnAssignedValue()
        {
            PropertyAssert.AllReturnAssignedValue(typeof(CustomConfig));
        }

        [Test]
        public void CustomConfig_HasNoDefaults()
        {
            var config = new CustomConfig();

            Assert.Multiple(() =>
            {
                Assert.That(config.AppPool, Is.Null);
                Assert.That(config.Options, Is.Null);
            });
        }

        [Test]
        public void Properties_MatchAttributesDeclaredInSchema()
        {
            var schemaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Resources", "IISAgentConfigSchema.xml");
            var attributes = XDocument.Load(schemaPath)
                .Descendants("element").Single(e => (string)e.Attribute("name") == "dynatrace")
                .Element("collection")
                .Elements("attribute")
                .Select(a => (string)a.Attribute("name"))
                .ToArray();

            var properties = typeof(CustomConfig).GetProperties()
                .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1))
                .ToArray();

            Assert.That(properties, Is.EquivalentTo(attributes));
        }
    }
}
