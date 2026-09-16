using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Web.Administration;
using IisPool = Microsoft.Web.Administration.ApplicationPool;

namespace IISAppCmd.IIS
{
    /// <summary>
    /// Writes an <see cref="ApplicationPool"/> into the applicationHost.config
    /// a <see cref="ServerManager"/> was opened on. Every value the model holds
    /// is written explicitly, so the pool never inherits from
    /// applicationPoolDefaults. Changes are left uncommitted so the caller can
    /// batch them with the rest of its edits.
    /// </summary>
    public sealed class ApplicationPoolBuilder
    {
        private readonly ApplicationPool _pool;
        private readonly ServerManager _manager;

        /// <param name="pool">The pool to write.</param>
        /// <param name="manager">Opened on the working copy at <paramref name="configPath"/>.</param>
        /// <param name="configPath">The working copy of applicationHost.config being edited.</param>
        public ApplicationPoolBuilder(ApplicationPool pool, ServerManager manager, string configPath)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException("The configuration path must not be empty.", nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("The configuration file does not exist.", configPath);

            _pool = pool;
            _manager = manager;
            ConfigPath = configPath;
        }

        public string ConfigPath { get; }

        /// <summary>
        /// Adds the pool to the applicationPools collection, or updates it when a
        /// pool of that name already exists, without committing.
        /// </summary>
        public bool Build(out string error)
        {
            if (string.IsNullOrWhiteSpace(_pool.Name))
            {
                error = $"the application pool has no name; nothing was written to '{ConfigPath}'.";
                return false;
            }

            // IIS requires both the name and the value of an environment variable
            // to be non-empty; checked here so the message names the culprit.
            if (_pool.EnvironmentVariables != null)
            {
                for (int i = 0; i < _pool.EnvironmentVariables.Count; i++)
                {
                    ApplicationPoolEnvironmentVariable variable = _pool.EnvironmentVariables[i];

                    if (variable == null || string.IsNullOrEmpty(variable.Name))
                    {
                        error = $"environment variable #{i + 1} of the application pool '{_pool.Name}' has no name.";
                        return false;
                    }

                    if (string.IsNullOrEmpty(variable.Value))
                    {
                        error = $"environment variable '{variable.Name}' of the application pool '{_pool.Name}' has no value.";
                        return false;
                    }
                }
            }

            try
            {
                // Re-running against the same file should not fail on a duplicate.
                IisPool target = _manager.ApplicationPools[_pool.Name]
                    ?? _manager.ApplicationPools.Add(_pool.Name);

                WriteAdd(target);
                WriteProcessModel(target.ProcessModel);
                WriteRecycling(target.Recycling);
                WriteFailure(target.Failure);
                WriteCpu(target.Cpu);
                WriteEnvironmentVariables(target);
            }
            catch (Exception ex) when (ex is COMException || ex is InvalidOperationException || ex is ArgumentException || ex is FileNotFoundException)
            {
                error = $"could not add the application pool '{_pool.Name}' to '{ConfigPath}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void WriteAdd(IisPool target)
        {
            Set(target, "queueLength", _pool.QueueLength);
            Set(target, "autoStart", _pool.AutoStart);
            Set(target, "enable32BitAppOnWin64", _pool.Enable32BitAppOnWin64);
            Set(target, "enableEmulationOnWinArm64", _pool.EnableEmulationOnWinArm64);
            Set(target, "managedRuntimeVersion", _pool.ManagedRuntimeVersion);
            Set(target, "managedRuntimeLoader", _pool.ManagedRuntimeLoader);
            Set(target, "enableConfigurationOverride", _pool.EnableConfigurationOverride);
            Set(target, "managedPipelineMode", _pool.ManagedPipelineMode);
            Set(target, "CLRConfigFile", _pool.CLRConfigFile);
            Set(target, "passAnonymousToken", _pool.PassAnonymousToken);
            Set(target, "startMode", _pool.StartMode);
        }

        private void WriteProcessModel(ConfigurationElement target)
        {
            ApplicationPoolProcessModel model = _pool.ProcessModel;
            if (model == null) return;

            Set(target, "identityType", model.IdentityType);
            Set(target, "userName", model.UserName);
            Set(target, "password", model.Password);
            Set(target, "requestQueueDelegatorIdentity", model.RequestQueueDelegatorIdentity);
            Set(target, "loadUserProfile", model.LoadUserProfile);
            Set(target, "setProfileEnvironment", model.SetProfileEnvironment);
            Set(target, "logonType", model.LogonType);
            Set(target, "manualGroupMembership", model.ManualGroupMembership);
            Set(target, "idleTimeout", model.IdleTimeout);
            Set(target, "idleTimeoutAction", model.IdleTimeoutAction);
            Set(target, "maxProcesses", model.MaxProcesses);
            Set(target, "shutdownTimeLimit", model.ShutdownTimeLimit);
            Set(target, "startupTimeLimit", model.StartupTimeLimit);
            Set(target, "pingingEnabled", model.PingingEnabled);
            Set(target, "pingInterval", model.PingInterval);
            Set(target, "pingResponseTime", model.PingResponseTime);
            Set(target, "logEventOnProcessModel", model.LogEventOnProcessModel);
        }

        private void WriteRecycling(ConfigurationElement target)
        {
            ApplicationPoolRecycling recycling = _pool.Recycling;
            if (recycling == null) return;

            Set(target, "disallowOverlappingRotation", recycling.DisallowOverlappingRotation);
            Set(target, "disallowRotationOnConfigChange", recycling.DisallowRotationOnConfigChange);
            Set(target, "logEventOnRecycle", recycling.LogEventOnRecycle);

            ApplicationPoolPeriodicRestart restart = recycling.PeriodicRestart;
            if (restart == null) return;

            ConfigurationElement periodicRestart = target.GetChildElement("periodicRestart");
            Set(periodicRestart, "memory", restart.Memory);
            Set(periodicRestart, "privateMemory", restart.PrivateMemory);
            Set(periodicRestart, "requests", restart.Requests);
            Set(periodicRestart, "time", restart.Time);

            if (restart.Schedule == null || restart.Schedule.Count == 0) return;

            ConfigurationElementCollection schedule = periodicRestart.GetCollection("schedule");
            schedule.Clear();
            foreach (TimeSpan time in restart.Schedule)
            {
                ConfigurationElement entry = schedule.CreateElement("add");
                entry["value"] = time;
                schedule.Add(entry);
            }
        }

        private void WriteFailure(ConfigurationElement target)
        {
            ApplicationPoolFailure failure = _pool.Failure;
            if (failure == null) return;

            Set(target, "loadBalancerCapabilities", failure.LoadBalancerCapabilities);
            Set(target, "orphanWorkerProcess", failure.OrphanWorkerProcess);
            Set(target, "orphanActionExe", failure.OrphanActionExe);
            Set(target, "orphanActionParams", failure.OrphanActionParams);
            Set(target, "rapidFailProtection", failure.RapidFailProtection);
            Set(target, "rapidFailProtectionInterval", failure.RapidFailProtectionInterval);
            Set(target, "rapidFailProtectionMaxCrashes", failure.RapidFailProtectionMaxCrashes);
            Set(target, "autoShutdownExe", failure.AutoShutdownExe);
            Set(target, "autoShutdownParams", failure.AutoShutdownParams);
        }

        private void WriteCpu(ConfigurationElement target)
        {
            ApplicationPoolCpu cpu = _pool.Cpu;
            if (cpu == null) return;

            Set(target, "limit", cpu.Limit);
            Set(target, "action", cpu.Action);
            Set(target, "resetInterval", cpu.ResetInterval);
            Set(target, "smpAffinitized", cpu.SmpAffinitized);
            Set(target, "smpProcessorAffinityMask", cpu.SmpProcessorAffinityMask);
            Set(target, "smpProcessorAffinityMask2", cpu.SmpProcessorAffinityMask2);
            Set(target, "processorGroup", cpu.ProcessorGroup);
            Set(target, "numaNodeAssignment", cpu.NumaNodeAssignment);
            Set(target, "numaNodeAffinityMode", cpu.NumaNodeAffinityMode);
        }

        private void WriteEnvironmentVariables(IisPool target)
        {
            List<ApplicationPoolEnvironmentVariable> variables = _pool.EnvironmentVariables;
            if (variables == null || variables.Count == 0) return;

            ConfigurationElementCollection collection = target.GetCollection("environmentVariables");
            collection.Clear();
            foreach (ApplicationPoolEnvironmentVariable variable in variables)
            {
                ConfigurationElement entry = collection.CreateElement("add");
                entry["name"] = variable.Name;
                entry["value"] = variable.Value;
                collection.Add(entry);
            }
        }

        /// <summary>
        /// Writes one attribute, converting the model's CLR type to what the IIS
        /// configuration system accepts. Null strings are left unset, and an
        /// attribute the installed IIS schema does not define is skipped rather
        /// than failing the whole pool on an older Windows.
        /// </summary>
        private static void Set(ConfigurationElement element, string name, object value)
        {
            if (value == null) return;
            if (element.Schema.AttributeSchemas[name] == null) return;

            if (value is Enum)
            {
                value = Convert.ToInt32(value);
            }
            else if (value is uint unsigned)
            {
                value = (long)unsigned;
            }

            element[name] = value;
        }
    }
}
