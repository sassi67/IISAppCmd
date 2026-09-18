using IISAppCmd.CommandLine;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class GlobalModuleFactoryTests
    {
        private const string Image = @"C:\modules\my.dll";

        [Test]
        public void FromCommandLine_WithoutTheOption_RegistersNoModule()
        {
            Assert.That(GlobalModuleFactory.FromCommandLine(new CommandLineOptions()), Is.Null);
        }

        [Test]
        public void FromCommandLine_TakesNameAndImageFromTheCommandLine()
        {
            GlobalModule module = GlobalModuleFactory.FromCommandLine(Options());

            Assert.Multiple(() =>
            {
                Assert.That(module.Name, Is.EqualTo("MyModule"));
                Assert.That(module.Image, Is.EqualTo(Image));
            });
        }

        [Test]
        public void FromCommandLine_KeepsThePreConditionItWasGiven()
        {
            var options = Options();
            options.GlobalModulePreCondition = "bitness64,integratedMode";

            Assert.That(GlobalModuleFactory.FromCommandLine(options).PreCondition, Is.EqualTo("bitness64,integratedMode"));
        }

        [TestCase(Bitness.X64, "bitness64")]
        [TestCase(Bitness.X86, "bitness32")]
        public void FromCommandLine_WithoutAPreCondition_TakesItFromTheBitness(Bitness bitness, string expected)
        {
            var options = Options();
            options.Bitness = bitness;

            Assert.That(GlobalModuleFactory.FromCommandLine(options).PreCondition, Is.EqualTo(expected));
        }

        [TestCase(Bitness.X64, "bitness64")]
        [TestCase(Bitness.X86, "bitness32")]
        public void PreConditionFor_MatchesTheBitnessOfTheWorkerProcess(Bitness bitness, string expected)
        {
            Assert.That(GlobalModuleFactory.PreConditionFor(bitness), Is.EqualTo(expected));
        }

        [Test]
        public void FromCommandLine_WithoutOptions_Throws()
        {
            Assert.That(() => GlobalModuleFactory.FromCommandLine(null), Throws.ArgumentNullException);
        }

        /// <summary>The command line of a run that asks for a module.</summary>
        private static CommandLineOptions Options() => new CommandLineOptions
        {
            Application = "Scratch",
            ApplicationPath = @"C:\apps\scratch",
            GlobalModule = "MyModule",
            GlobalModuleImage = Image,
        };
    }
}
