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
    public class ApplicationPoolBuilderTests
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
        public void Build_WritesPoolFromCommandLine()
        {
            var options = new CommandLineOptions { Bitness = Bitness.X86, Tfm = "net10.0" };
            ApplicationPool pool = ApplicationPoolFactory.FromCommandLine(options, "7430f109");

            BuildAndCommit(pool);

            XElement add = FindPool("AppPool_7430f109");
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(add, "enable32BitAppOnWin64"), Is.EqualTo("true"));
                Assert.That(Attribute(add, "managedRuntimeVersion"), Is.EqualTo(""), "an empty version must be explicit so applicationPoolDefaults' v4.0 is not inherited");
                Assert.That(Attribute(add, "managedPipelineMode"), Is.EqualTo("Integrated"));
                Assert.That(Attribute(add, "autoStart"), Is.EqualTo("true"));
                Assert.That(Attribute(add, "CLRConfigFile"), Is.EqualTo(@"%IIS_BIN%\config\templates\PersonalWebServer\aspnet.config"));
            });
        }

        [Test]
        public void Build_WritesEveryAddAttribute()
        {
            var pool = new ApplicationPool
            {
                Name = "AppPool_attrs",
                QueueLength = 2000,
                AutoStart = false,
                Enable32BitAppOnWin64 = true,
                EnableEmulationOnWinArm64 = false,
                ManagedRuntimeVersion = "v4.0",
                ManagedRuntimeLoader = "custom.dll",
                EnableConfigurationOverride = false,
                ManagedPipelineMode = ManagedPipelineMode.Classic,
                CLRConfigFile = @"C:\clr.config",
                PassAnonymousToken = false,
                StartMode = StartMode.AlwaysRunning,
            };

            BuildAndCommit(pool);

            XElement add = FindPool("AppPool_attrs");
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(add, "queueLength"), Is.EqualTo("2000"));
                Assert.That(Attribute(add, "autoStart"), Is.EqualTo("false"));
                Assert.That(Attribute(add, "enable32BitAppOnWin64"), Is.EqualTo("true"));
                Assert.That(Attribute(add, "enableEmulationOnWinArm64"), Is.EqualTo("false"));
                Assert.That(Attribute(add, "managedRuntimeVersion"), Is.EqualTo("v4.0"));
                Assert.That(Attribute(add, "managedRuntimeLoader"), Is.EqualTo("custom.dll"));
                Assert.That(Attribute(add, "enableConfigurationOverride"), Is.EqualTo("false"));
                Assert.That(Attribute(add, "managedPipelineMode"), Is.EqualTo("Classic"));
                Assert.That(Attribute(add, "CLRConfigFile"), Is.EqualTo(@"C:\clr.config"));
                Assert.That(Attribute(add, "passAnonymousToken"), Is.EqualTo("false"));
                Assert.That(Attribute(add, "startMode"), Is.EqualTo("AlwaysRunning"));
            });
        }

        [Test]
        public void Build_WritesProcessModel()
        {
            var pool = new ApplicationPool { Name = "AppPool_pm" };
            pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
            pool.ProcessModel.LoadUserProfile = true;
            pool.ProcessModel.SetProfileEnvironment = false;
            pool.ProcessModel.LogonType = LogonType.LogonService;
            pool.ProcessModel.ManualGroupMembership = true;
            pool.ProcessModel.IdleTimeout = TimeSpan.FromMinutes(10);
            pool.ProcessModel.IdleTimeoutAction = IdleTimeoutAction.Suspend;
            pool.ProcessModel.MaxProcesses = 4;
            pool.ProcessModel.ShutdownTimeLimit = TimeSpan.FromSeconds(45);
            pool.ProcessModel.StartupTimeLimit = TimeSpan.FromSeconds(60);
            pool.ProcessModel.PingingEnabled = false;
            pool.ProcessModel.PingInterval = TimeSpan.FromSeconds(15);
            pool.ProcessModel.PingResponseTime = TimeSpan.FromSeconds(120);
            pool.ProcessModel.LogEventOnProcessModel = ProcessModelLogEvents.None;
            pool.ProcessModel.RequestQueueDelegatorIdentity = @"NT AUTHORITY\LOCAL SERVICE";

            BuildAndCommit(pool);

            XElement processModel = FindPool("AppPool_pm").Element("processModel");
            Assert.That(processModel, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(processModel, "identityType"), Is.EqualTo("NetworkService"));
                Assert.That(Attribute(processModel, "loadUserProfile"), Is.EqualTo("true"));
                Assert.That(Attribute(processModel, "setProfileEnvironment"), Is.EqualTo("false"));
                Assert.That(Attribute(processModel, "logonType"), Is.EqualTo("LogonService"));
                Assert.That(Attribute(processModel, "manualGroupMembership"), Is.EqualTo("true"));
                Assert.That(Attribute(processModel, "idleTimeout"), Is.EqualTo("00:10:00"));
                Assert.That(Attribute(processModel, "idleTimeoutAction"), Is.EqualTo("Suspend"));
                Assert.That(Attribute(processModel, "maxProcesses"), Is.EqualTo("4"));
                Assert.That(Attribute(processModel, "shutdownTimeLimit"), Is.EqualTo("00:00:45"));
                Assert.That(Attribute(processModel, "startupTimeLimit"), Is.EqualTo("00:01:00"));
                Assert.That(Attribute(processModel, "pingingEnabled"), Is.EqualTo("false"));
                Assert.That(Attribute(processModel, "pingInterval"), Is.EqualTo("00:00:15"));
                Assert.That(Attribute(processModel, "pingResponseTime"), Is.EqualTo("00:02:00"));
                Assert.That(Attribute(processModel, "logEventOnProcessModel"), Is.EqualTo(""), "IIS writes an empty flags value as an empty string");
                Assert.That(Attribute(processModel, "requestQueueDelegatorIdentity"), Is.EqualTo(@"NT AUTHORITY\LOCAL SERVICE"));
                Assert.That(processModel.Attribute("userName"), Is.Null, "null strings are left unset");
                Assert.That(processModel.Attribute("password"), Is.Null, "null strings are left unset");
            });
        }

        [Test]
        public void Build_WritesRecyclingWithScheduleAndPeriodicRestart()
        {
            var pool = new ApplicationPool { Name = "AppPool_rec" };
            pool.Recycling.DisallowOverlappingRotation = true;
            pool.Recycling.DisallowRotationOnConfigChange = true;
            pool.Recycling.LogEventOnRecycle = RecyclingLogEvents.Time | RecyclingLogEvents.Memory;
            pool.Recycling.PeriodicRestart.Memory = 1024;
            pool.Recycling.PeriodicRestart.PrivateMemory = 2048;
            pool.Recycling.PeriodicRestart.Requests = 500;
            pool.Recycling.PeriodicRestart.Time = TimeSpan.FromHours(12);
            pool.Recycling.PeriodicRestart.Schedule.Add(new TimeSpan(3, 0, 0));
            pool.Recycling.PeriodicRestart.Schedule.Add(new TimeSpan(15, 30, 0));

            BuildAndCommit(pool);

            XElement recycling = FindPool("AppPool_rec").Element("recycling");
            Assert.That(recycling, Is.Not.Null);
            XElement periodicRestart = recycling.Element("periodicRestart");
            Assert.That(periodicRestart, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(recycling, "disallowOverlappingRotation"), Is.EqualTo("true"));
                Assert.That(Attribute(recycling, "disallowRotationOnConfigChange"), Is.EqualTo("true"));
                Assert.That(Attribute(recycling, "logEventOnRecycle"), Is.EqualTo("Time, Memory"));
                Assert.That(Attribute(periodicRestart, "memory"), Is.EqualTo("1024"));
                Assert.That(Attribute(periodicRestart, "privateMemory"), Is.EqualTo("2048"));
                Assert.That(Attribute(periodicRestart, "requests"), Is.EqualTo("500"));
                Assert.That(Attribute(periodicRestart, "time"), Is.EqualTo("12:00:00"));

                var schedule = periodicRestart.Element("schedule")?.Elements("add").Select(e => Attribute(e, "value")).ToList();
                Assert.That(schedule, Is.EqualTo(new[] { "03:00:00", "15:30:00" }));
            });
        }

        [Test]
        public void Build_WritesFailure()
        {
            var pool = new ApplicationPool { Name = "AppPool_fail" };
            pool.Failure.LoadBalancerCapabilities = LoadBalancerCapabilities.TcpLevel;
            pool.Failure.OrphanWorkerProcess = true;
            pool.Failure.OrphanActionExe = @"C:\orphan.exe";
            pool.Failure.OrphanActionParams = "-x";
            pool.Failure.RapidFailProtection = false;
            pool.Failure.RapidFailProtectionInterval = TimeSpan.FromMinutes(2);
            pool.Failure.RapidFailProtectionMaxCrashes = 3;
            pool.Failure.AutoShutdownExe = @"C:\shutdown.exe";
            pool.Failure.AutoShutdownParams = "-y";

            BuildAndCommit(pool);

            XElement failure = FindPool("AppPool_fail").Element("failure");
            Assert.That(failure, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(failure, "loadBalancerCapabilities"), Is.EqualTo("TcpLevel"));
                Assert.That(Attribute(failure, "orphanWorkerProcess"), Is.EqualTo("true"));
                Assert.That(Attribute(failure, "orphanActionExe"), Is.EqualTo(@"C:\orphan.exe"));
                Assert.That(Attribute(failure, "orphanActionParams"), Is.EqualTo("-x"));
                Assert.That(Attribute(failure, "rapidFailProtection"), Is.EqualTo("false"));
                Assert.That(Attribute(failure, "rapidFailProtectionInterval"), Is.EqualTo("00:02:00"));
                Assert.That(Attribute(failure, "rapidFailProtectionMaxCrashes"), Is.EqualTo("3"));
                Assert.That(Attribute(failure, "autoShutdownExe"), Is.EqualTo(@"C:\shutdown.exe"));
                Assert.That(Attribute(failure, "autoShutdownParams"), Is.EqualTo("-y"));
            });
        }

        [Test]
        public void Build_WritesCpu()
        {
            var pool = new ApplicationPool { Name = "AppPool_cpu" };
            pool.Cpu.Limit = 50000;
            pool.Cpu.Action = CpuAction.ThrottleUnderLoad;
            pool.Cpu.ResetInterval = TimeSpan.FromMinutes(1);
            pool.Cpu.SmpAffinitized = true;
            pool.Cpu.SmpProcessorAffinityMask = 3;
            pool.Cpu.SmpProcessorAffinityMask2 = 5;
            pool.Cpu.ProcessorGroup = 1;
            pool.Cpu.NumaNodeAssignment = NumaNodeAssignment.WindowsScheduling;
            pool.Cpu.NumaNodeAffinityMode = NumaNodeAffinityMode.Hard;

            BuildAndCommit(pool);

            XElement cpu = FindPool("AppPool_cpu").Element("cpu");
            Assert.That(cpu, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(Attribute(cpu, "limit"), Is.EqualTo("50000"));
                Assert.That(Attribute(cpu, "action"), Is.EqualTo("ThrottleUnderLoad"));
                Assert.That(Attribute(cpu, "resetInterval"), Is.EqualTo("00:01:00"));
                Assert.That(Attribute(cpu, "smpAffinitized"), Is.EqualTo("true"));
                Assert.That(Attribute(cpu, "smpProcessorAffinityMask"), Is.EqualTo("3"));
                Assert.That(Attribute(cpu, "smpProcessorAffinityMask2"), Is.EqualTo("5"));
                Assert.That(Attribute(cpu, "processorGroup"), Is.EqualTo("1"));
                Assert.That(Attribute(cpu, "numaNodeAssignment"), Is.EqualTo("WindowsScheduling"));
                Assert.That(Attribute(cpu, "numaNodeAffinityMode"), Is.EqualTo("Hard"));
            });
        }

        [Test]
        public void Build_WritesEnvironmentVariables()
        {
            var pool = new ApplicationPool { Name = "AppPool_env" };
            pool.EnvironmentVariables.Add(new ApplicationPoolEnvironmentVariable { Name = "DT_AGENTNAME", Value = "IISAgent_1" });
            pool.EnvironmentVariables.Add(new ApplicationPoolEnvironmentVariable { Name = "SITES_ROOT", Value = @"C:\apps" });

            BuildAndCommit(pool);

            XElement variables = FindPool("AppPool_env").Element("environmentVariables");
            Assert.That(variables, Is.Not.Null);

            var entries = variables.Elements("add")
                .Select(e => new { Name = Attribute(e, "name"), Value = Attribute(e, "value") })
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(entries.Count, Is.EqualTo(2));
                Assert.That(entries[0].Name, Is.EqualTo("DT_AGENTNAME"));
                Assert.That(entries[0].Value, Is.EqualTo("IISAgent_1"));
                Assert.That(entries[1].Name, Is.EqualTo("SITES_ROOT"));
                Assert.That(entries[1].Value, Is.EqualTo(@"C:\apps"));
            });
        }

        [TestCase("", "value", "#1", "has no name")]
        [TestCase(null, "value", "#1", "has no name")]
        [TestCase("NAME", "", "'NAME'", "has no value")]
        [TestCase("NAME", null, "'NAME'", "has no value")]
        public void Build_InvalidEnvironmentVariable_FailsBeforeWriting(string name, string value, string culprit, string reason)
        {
            var pool = new ApplicationPool { Name = "AppPool_badenv" };
            pool.EnvironmentVariables.Add(new ApplicationPoolEnvironmentVariable { Name = name, Value = value });

            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ApplicationPoolBuilder(pool, manager, _configPath);

                bool ok = builder.Build(out string error);

                Assert.Multiple(() =>
                {
                    Assert.That(ok, Is.False);
                    Assert.That(error, Does.Contain(culprit).And.Contain(reason));
                    Assert.That(manager.ApplicationPools["AppPool_badenv"], Is.Null, "nothing is added when validation fails");
                });
            }
        }

        [Test]
        public void Build_WithoutEnvironmentVariables_WritesNoCollection()
        {
            BuildAndCommit(new ApplicationPool { Name = "AppPool_noenv" });

            XElement add = FindPool("AppPool_noenv");
            Assert.Multiple(() =>
            {
                Assert.That(add.Element("environmentVariables"), Is.Null);
                Assert.That(add.Element("recycling")?.Element("periodicRestart")?.Element("schedule"), Is.Null);
            });
        }

        [Test]
        public void Build_KeepsExistingPoolsAndDefaults()
        {
            BuildAndCommit(new ApplicationPool { Name = "AppPool_keep" });

            XElement pools = ApplicationPools();
            Assert.Multiple(() =>
            {
                Assert.That(pools.Elements("add").Select(e => Attribute(e, "name")), Is.EquivalentTo(new[] { "DefaultAppPool", "AppPool_keep" }));
                Assert.That(Attribute(pools.Element("applicationPoolDefaults"), "managedRuntimeVersion"), Is.EqualTo("v4.0"));
            });
        }

        [Test]
        public void Build_Twice_UpdatesTheSamePool()
        {
            BuildAndCommit(new ApplicationPool { Name = "AppPool_twice", QueueLength = 1000 });
            BuildAndCommit(new ApplicationPool { Name = "AppPool_twice", QueueLength = 3000 });

            var matches = ApplicationPools().Elements("add").Where(e => Attribute(e, "name") == "AppPool_twice").ToList();
            Assert.Multiple(() =>
            {
                Assert.That(matches.Count, Is.EqualTo(1));
                Assert.That(Attribute(matches[0], "queueLength"), Is.EqualTo("3000"));
            });
        }

        [Test]
        public void Build_UncommittedChanges_DoNotReachTheFile()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ApplicationPoolBuilder(new ApplicationPool { Name = "AppPool_uncommitted" }, manager, _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
            }

            Assert.That(ApplicationPools().Elements("add").Select(e => Attribute(e, "name")), Is.EqualTo(new[] { "DefaultAppPool" }));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("   ")]
        public void Build_PoolWithoutName_Fails(string name)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ApplicationPoolBuilder(new ApplicationPool { Name = name }, manager, _configPath);

                bool ok = builder.Build(out string error);

                Assert.Multiple(() =>
                {
                    Assert.That(ok, Is.False);
                    Assert.That(error, Does.Contain("has no name"));
                    Assert.That(error, Does.Contain(_configPath));
                });
            }
        }

        [Test]
        public void Constructor_ExposesConfigPath()
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ApplicationPoolBuilder(new ApplicationPool { Name = "x" }, manager, _configPath);

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
                    Assert.That(() => new ApplicationPoolBuilder(null, manager, _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new ApplicationPoolBuilder(new ApplicationPool(), null, _configPath), Throws.ArgumentNullException);
                    Assert.That(() => new ApplicationPoolBuilder(new ApplicationPool(), manager, ""), Throws.ArgumentException);
                    Assert.That(() => new ApplicationPoolBuilder(new ApplicationPool(), manager, Path.Combine(_scratch, "missing.config")), Throws.TypeOf<FileNotFoundException>());
                });
            }
        }

        private void BuildAndCommit(ApplicationPool pool)
        {
            using (var manager = new ServerManager(_configPath))
            {
                var builder = new ApplicationPoolBuilder(pool, manager, _configPath);
                Assert.That(builder.Build(out string error), Is.True, error);
                manager.CommitChanges();
            }
        }

        private XElement ApplicationPools()
        {
            XElement pools = XDocument.Load(_configPath).Root?
                .Element("system.applicationHost")?
                .Element("applicationPools");

            Assert.That(pools, Is.Not.Null, "applicationPools section is missing");
            return pools;
        }

        private XElement FindPool(string name)
        {
            XElement add = ApplicationPools().Elements("add").SingleOrDefault(e => Attribute(e, "name") == name);

            Assert.That(add, Is.Not.Null, $"pool '{name}' was not written");
            return add;
        }

        private static string Attribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);

            Assert.That(attribute, Is.Not.Null, $"<{element.Name}> has no '{name}' attribute");
            return attribute.Value;
        }
    }
}
