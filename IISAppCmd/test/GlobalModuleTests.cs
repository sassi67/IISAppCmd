using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class GlobalModuleTests
    {
        [Test]
        public void AllPublicProperties_HavePublicGetterAndSetter()
        {
            PropertyAssert.AllHavePublicGetterAndSetter(typeof(GlobalModule));
        }

        [Test]
        public void AllPublicProperties_ReturnAssignedValue()
        {
            PropertyAssert.AllReturnAssignedValue(typeof(GlobalModule));
        }

        [Test]
        public void GlobalModule_HasIisSchemaDefaults()
        {
            var module = new GlobalModule();

            Assert.Multiple(() =>
            {
                Assert.That(module.Name, Is.Null);
                Assert.That(module.Image, Is.Null);
                Assert.That(module.PreCondition, Is.Null);
            });
        }

        [Test]
        public void GlobalModule_HoldsValuesFromApplicationHostConfig()
        {
            var module = new GlobalModule
            {
                Name = "IISNativeModule",
                Image = @"E:\Cpp\Release\IISNativeModule.dll",
                PreCondition = "bitness64",
            };

            Assert.Multiple(() =>
            {
                Assert.That(module.Name, Is.EqualTo("IISNativeModule"));
                Assert.That(module.Image, Is.EqualTo(@"E:\Cpp\Release\IISNativeModule.dll"));
                Assert.That(module.PreCondition, Is.EqualTo("bitness64"));
            });
        }
    }
}
