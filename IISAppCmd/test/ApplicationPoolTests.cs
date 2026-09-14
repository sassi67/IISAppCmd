using System;
using IISAppCmd.IIS;
using NUnit.Framework;

namespace IISAppCmd.Tests
{
    public class ApplicationPoolTests
    {
        private static readonly Type[] ModelTypes =
        {
            typeof(ApplicationPool),
            typeof(ApplicationPoolProcessModel),
            typeof(ApplicationPoolRecycling),
            typeof(ApplicationPoolPeriodicRestart),
            typeof(ApplicationPoolFailure),
            typeof(ApplicationPoolCpu),
            typeof(ApplicationPoolEnvironmentVariable),
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
        public void ApplicationPool_HasIisSchemaDefaults()
        {
            var pool = new ApplicationPool();

            Assert.Multiple(() =>
            {
                Assert.That(pool.Name, Is.Null);
                Assert.That(pool.QueueLength, Is.EqualTo(1000u));
                Assert.That(pool.AutoStart, Is.True);
                Assert.That(pool.Enable32BitAppOnWin64, Is.False);
                Assert.That(pool.EnableEmulationOnWinArm64, Is.True);
                Assert.That(pool.ManagedRuntimeVersion, Is.EqualTo(""));
                Assert.That(pool.ManagedRuntimeLoader, Is.EqualTo("webengine4.dll"));
                Assert.That(pool.EnableConfigurationOverride, Is.True);
                Assert.That(pool.ManagedPipelineMode, Is.EqualTo(ManagedPipelineMode.Integrated));
                Assert.That(pool.CLRConfigFile, Is.EqualTo(""));
                Assert.That(pool.PassAnonymousToken, Is.True);
                Assert.That(pool.StartMode, Is.EqualTo(StartMode.OnDemand));
                Assert.That(pool.ProcessModel, Is.Not.Null);
                Assert.That(pool.Recycling, Is.Not.Null);
                Assert.That(pool.Recycling.PeriodicRestart, Is.Not.Null);
                Assert.That(pool.Failure, Is.Not.Null);
                Assert.That(pool.Cpu, Is.Not.Null);
                Assert.That(pool.EnvironmentVariables, Is.Empty);
            });
        }

        [Test]
        public void ProcessModel_HasIisSchemaDefaults()
        {
            var processModel = new ApplicationPoolProcessModel();

            Assert.Multiple(() =>
            {
                Assert.That(processModel.IdentityType, Is.EqualTo(ProcessModelIdentityType.ApplicationPoolIdentity));
                Assert.That(processModel.UserName, Is.Null);
                Assert.That(processModel.Password, Is.Null);
                Assert.That(processModel.RequestQueueDelegatorIdentity, Is.Null);
                Assert.That(processModel.LoadUserProfile, Is.False);
                Assert.That(processModel.SetProfileEnvironment, Is.True);
                Assert.That(processModel.LogonType, Is.EqualTo(LogonType.LogonBatch));
                Assert.That(processModel.ManualGroupMembership, Is.False);
                Assert.That(processModel.IdleTimeout, Is.EqualTo(TimeSpan.Parse("00:20:00")));
                Assert.That(processModel.IdleTimeoutAction, Is.EqualTo(IdleTimeoutAction.Terminate));
                Assert.That(processModel.MaxProcesses, Is.EqualTo(1u));
                Assert.That(processModel.ShutdownTimeLimit, Is.EqualTo(TimeSpan.Parse("00:01:30")));
                Assert.That(processModel.StartupTimeLimit, Is.EqualTo(TimeSpan.Parse("00:01:30")));
                Assert.That(processModel.PingingEnabled, Is.True);
                Assert.That(processModel.PingInterval, Is.EqualTo(TimeSpan.Parse("00:00:30")));
                Assert.That(processModel.PingResponseTime, Is.EqualTo(TimeSpan.Parse("00:01:30")));
                Assert.That(processModel.LogEventOnProcessModel, Is.EqualTo(ProcessModelLogEvents.IdleTimeout));
            });
        }

        [Test]
        public void Recycling_HasIisSchemaDefaults()
        {
            var recycling = new ApplicationPoolRecycling();

            Assert.Multiple(() =>
            {
                Assert.That(recycling.DisallowOverlappingRotation, Is.False);
                Assert.That(recycling.DisallowRotationOnConfigChange, Is.False);
                Assert.That(recycling.LogEventOnRecycle, Is.EqualTo((RecyclingLogEvents)255));
                Assert.That(recycling.PeriodicRestart.Memory, Is.EqualTo(0u));
                Assert.That(recycling.PeriodicRestart.PrivateMemory, Is.EqualTo(0u));
                Assert.That(recycling.PeriodicRestart.Requests, Is.EqualTo(0u));
                Assert.That(recycling.PeriodicRestart.Time, Is.EqualTo(TimeSpan.Parse("1.05:00:00")));
                Assert.That(recycling.PeriodicRestart.Schedule, Is.Empty);
            });
        }

        [Test]
        public void Failure_HasIisSchemaDefaults()
        {
            var failure = new ApplicationPoolFailure();

            Assert.Multiple(() =>
            {
                Assert.That(failure.LoadBalancerCapabilities, Is.EqualTo(LoadBalancerCapabilities.HttpLevel));
                Assert.That(failure.OrphanWorkerProcess, Is.False);
                Assert.That(failure.OrphanActionExe, Is.Null);
                Assert.That(failure.OrphanActionParams, Is.Null);
                Assert.That(failure.RapidFailProtection, Is.True);
                Assert.That(failure.RapidFailProtectionInterval, Is.EqualTo(TimeSpan.Parse("00:05:00")));
                Assert.That(failure.RapidFailProtectionMaxCrashes, Is.EqualTo(5u));
                Assert.That(failure.AutoShutdownExe, Is.Null);
                Assert.That(failure.AutoShutdownParams, Is.Null);
            });
        }

        [Test]
        public void Cpu_HasIisSchemaDefaults()
        {
            var cpu = new ApplicationPoolCpu();

            Assert.Multiple(() =>
            {
                Assert.That(cpu.Limit, Is.EqualTo(0u));
                Assert.That(cpu.Action, Is.EqualTo(CpuAction.NoAction));
                Assert.That(cpu.ResetInterval, Is.EqualTo(TimeSpan.Parse("00:05:00")));
                Assert.That(cpu.SmpAffinitized, Is.False);
                Assert.That(cpu.SmpProcessorAffinityMask, Is.EqualTo(4294967295u));
                Assert.That(cpu.SmpProcessorAffinityMask2, Is.EqualTo(4294967295u));
                Assert.That(cpu.ProcessorGroup, Is.EqualTo(0));
                Assert.That(cpu.NumaNodeAssignment, Is.EqualTo(NumaNodeAssignment.MostAvailableMemory));
                Assert.That(cpu.NumaNodeAffinityMode, Is.EqualTo(NumaNodeAffinityMode.Soft));
            });
        }
    }
}
