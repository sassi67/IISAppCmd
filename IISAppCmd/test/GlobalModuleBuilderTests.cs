using System;
using System.IO;
using System.Linq;
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
    public class GlobalModuleBuilderTests
    {
        private const string Image = @"C:\modules\my.dll";

        /// <summary>A module the bundled configuration already declares.</summary>
        private const string BundledModule = "IISNativeModule";

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
        public void Build_DeclaresTheModuleInGlobalModules()
        {
            BuildAndCommit(NewModule("MyModule"));

            XElement declared = FindIn(GlobalModules(), "MyModule");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(declared, "image"), Is.EqualTo(Image));
                Assert.That(Attribute(declared, "preCondition"), Is.EqualTo("bitness64"));
            });
        }

        [Test]
        public void Build_EnablesTheModuleInModules()
        {
            BuildAndCommit(NewModule("MyModule"));

            XElement enabled = FindIn(Modules(), "MyModule");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(enabled, "preCondition"), Is.EqualTo("bitness64"));
                Assert.That(enabled.Attribute("image"), Is.Null, "a native module is referred to by name in <modules>");
                Assert.That(
                    Modules().Elements("add").Select(e => Attribute(e, "name")),
                    Does.Contain("MyModule"),
                    "the module has to appear in the list of the enabled modules");
            });
        }

        [Test]
        public void Build_WithoutPreCondition_WritesNone()
        {
            GlobalModule module = NewModule("MyModule");
            module.PreCondition = null;

            BuildAndCommit(module);

            Assert.Multiple(() =>
            {
                Assert.That(FindIn(GlobalModules(), "MyModule").Attribute("preCondition"), Is.Null);
                Assert.That(FindIn(Modules(), "MyModule").Attribute("preCondition"), Is.Null);
            });
        }

        [Test]
        public void Build_KeepsTheModulesAlreadyThere()
        {
            BuildAndCommit(NewModule("MyModule"));

            Assert.Multiple(() =>
            {
                Assert.That(
                    GlobalModules().Elements("add").Select(e => Attribute(e, "name")),
                    Does.Contain("HttpLoggingModule").And.Contain(BundledModule));
                Assert.That(
                    Modules().Elements("add").Select(e => Attribute(e, "name")),
                    Does.Contain("StaticFileModule").And.Contain(BundledModule));
                Assert.That(
                    Attribute(FindIn(Modules(), "HttpLoggingModule"), "lockItem"),
                    Is.EqualTo("true"),
                    "an entry already there is left as it was");
            });
        }

        [Test]
        public void Build_AppendsTheModuleAfterTheOnesAlreadyThere()
        {
            BuildAndCommit(NewModule("MyModule"));

            Assert.Multiple(() =>
            {
                Assert.That(GlobalModules().Elements("add").Last().Attribute("name").Value, Is.EqualTo("MyModule"));
                Assert.That(Modules().Elements("add").Last().Attribute("name").Value, Is.EqualTo("MyModule"));
            });
        }

        [Test]
        public void Build_ModuleAlreadyDeclared_UpdatesItInPlace()
        {
            GlobalModule module = NewModule(BundledModule);
            module.Image = @"C:\modules\updated.dll";
            module.PreCondition = "bitness32";

            BuildAndCommit(module);

            Assert.Multiple(() =>
            {
                Assert.That(Count(GlobalModules(), BundledModule), Is.EqualTo(1));
                Assert.That(Count(Modules(), BundledModule), Is.EqualTo(1));
                Assert.That(Attribute(FindIn(GlobalModules(), BundledModule), "image"), Is.EqualTo(@"C:\modules\updated.dll"));
                Assert.That(Attribute(FindIn(GlobalModules(), BundledModule), "preCondition"), Is.EqualTo("bitness32"));
                Assert.That(Attribute(FindIn(Modules(), BundledModule), "preCondition"), Is.EqualTo("bitness32"));
            });
        }

        [Test]
        public void Build_Twice_UpdatesTheSameModule()
        {
            BuildAndCommit(NewModule("MyModule"));

            GlobalModule updated = NewModule("MyModule");
            updated.Image = @"C:\modules\other.dll";
            BuildAndCommit(updated);

            Assert.Multiple(() =>
            {
                Assert.That(Count(GlobalModules(), "MyModule"), Is.EqualTo(1));
                Assert.That(Count(Modules(), "MyModule"), Is.EqualTo(1));
                Assert.That(Attribute(FindIn(GlobalModules(), "MyModule"), "image"), Is.EqualTo(@"C:\modules\other.dll"));
            });
        }

        [Test]
        public void Build_ImageWithEnvironmentVariable_IsWrittenAsGiven()
        {
            GlobalModule module = NewModule("MyModule");
            module.Image = @"%windir%\System32\inetsrv\my.dll";

            BuildAndCommit(module);

            Assert.That(Attribute(FindIn(GlobalModules(), "MyModule"), "image"), Is.EqualTo(@"%windir%\System32\inetsrv\my.dll"));
        }

        [Test]
        public void Build_UncommittedChanges_DoNotReachTheFile()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new GlobalModuleBuilder(NewModule("MyModule"), manager, _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
            }

            Assert.Multiple(() =>
            {
                Assert.That(GlobalModules().Elements("add").Select(e => Attribute(e, "name")), Does.Not.Contain("MyModule"));
                Assert.That(Modules().Elements("add").Select(e => Attribute(e, "name")), Does.Not.Contain("MyModule"));
            });
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public void Build_ModuleWithoutName_Fails(string name)
        {
            string error = AssertFails(NewModule(name), "the global module has no name");

            Assert.That(error, Does.Contain(_configPath));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public void Build_ModuleWithoutImage_Fails(string image)
        {
            GlobalModule module = NewModule("MyModule");
            module.Image = image;

            AssertFails(module, "the global module 'MyModule' has no image.");
        }

        [Test]
        public void Constructor_ExposesConfigPath()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new GlobalModuleBuilder(NewModule("MyModule"), manager, _configPath);

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
                    Assert.That(() => new GlobalModuleBuilder(null, manager, _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new GlobalModuleBuilder(new GlobalModule(), null, _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new GlobalModuleBuilder(new GlobalModule(), manager, ""), Throws.ArgumentException);
                    Assert.That(
                        () => new GlobalModuleBuilder(new GlobalModule(), manager, Path.Combine(_scratch, "missing.config")),
                        Throws.TypeOf<FileNotFoundException>());
                });
            }
        }

        /// <summary>The smallest module the builder accepts.</summary>
        private static GlobalModule NewModule(string name) => new GlobalModule
        {
            Name = name,
            Image = Image,
            PreCondition = "bitness64",
        };

        private void BuildAndCommit(GlobalModule module)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new GlobalModuleBuilder(module, manager, _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
                manager.CommitChanges();
            }
        }

        /// <summary>Builds and expects a refusal that names the culprit; returns the message.</summary>
        private string AssertFails(GlobalModule module, string expected)
        {
            int declared = GlobalModules().Elements("add").Count();

            using (var manager = new ServerManager(_configPath))
            {
                var builder = new GlobalModuleBuilder(module, manager, _configPath);

                bool ok = builder.Build(out string error);
                manager.CommitChanges();

                Assert.Multiple(() =>
                {
                    Assert.That(ok, Is.False);
                    Assert.That(error, Does.Contain(expected));
                    Assert.That(GlobalModules().Elements("add").Count(), Is.EqualTo(declared), "nothing is added when validation fails");
                });

                return error;
            }
        }

        private XElement Section(string name)
        {
            XElement section = XDocument.Load(_configPath).Root?
                .Element("system.webServer")?
                .Element(name);

            Assert.That(section, Is.Not.Null, $"{name} section is missing");
            return section;
        }

        private XElement GlobalModules() => Section("globalModules");

        private XElement Modules() => Section("modules");

        private static XElement FindIn(XElement section, string name)
        {
            XElement entry = section.Elements("add").SingleOrDefault(e => Attribute(e, "name") == name);

            Assert.That(entry, Is.Not.Null, $"'{name}' was not written to <{section.Name}>");
            return entry;
        }

        private static int Count(XElement section, string name) =>
            section.Elements("add").Count(e => Attribute(e, "name") == name);

        private static string Attribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);

            Assert.That(attribute, Is.Not.Null, $"<{element.Name}> has no '{name}' attribute");
            return attribute.Value;
        }
    }
}
