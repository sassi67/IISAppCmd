using System;
using System.Linq;
using System.Reflection;
using IISAppCmd.IIS;
using NUnit.Framework;
using NUnit.Framework.Constraints;

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
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Assert.That(properties, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                foreach (var property in properties)
                {
                    Assert.That(property.GetGetMethod(), Is.Not.Null, $"{type.Name}.{property.Name} has no public getter");
                    Assert.That(property.GetSetMethod(), Is.Not.Null, $"{type.Name}.{property.Name} has no public setter");
                }
            });
        }

        [TestCaseSource(nameof(ModelTypes))]
        public void AllPublicProperties_ReturnAssignedValue(Type type)
        {
            var instance = Activator.CreateInstance(type);

            Assert.Multiple(() =>
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var value = DifferentValue(property.PropertyType, property.GetValue(instance));
                    property.SetValue(instance, value);

                    var expected = property.PropertyType.IsValueType || property.PropertyType == typeof(string)
                        ? (IResolveConstraint)Is.EqualTo(value)
                        : Is.SameAs(value);
                    Assert.That(property.GetValue(instance), expected, $"{type.Name}.{property.Name}");
                }
            });
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

        private static object DifferentValue(Type type, object current)
        {
            if (type == typeof(string)) return current + "-changed";
            if (type == typeof(bool)) return !(bool)current;
            if (type == typeof(uint)) return unchecked((uint)current + 1);
            if (type == typeof(int)) return (int)current + 1;
            if (type == typeof(TimeSpan)) return ((TimeSpan)current).Add(TimeSpan.FromMinutes(1));
            if (type.IsEnum) return Enum.GetValues(type).Cast<object>().First(v => !v.Equals(current));
            return Activator.CreateInstance(type);
        }
    }
}
