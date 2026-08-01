using System;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Profiling;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class OptimizerRuntime
    {
        private static readonly object Sync = new object();
        private static OptimizerSettings _settings;
        private static HarmonyProfilerPatches _profilerPatches;
        private static ProfileSession _session;
        private static RuntimeOverlay _overlay;
        private static bool _deferredProfilerTargetsApplied;
        private static bool _initialized;

        internal static bool ProfilingEnabled => _settings?.Profiling.Enabled == true;

        internal static void Initialize()
        {
            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }

                string settingsPath = PathProvider.ResolveSettingsPath();
                _settings = SettingsLoader.LoadOrCreate(settingsPath);
                _settings.Normalize();
                OptimizerLog.Initialize(PathProvider.LogDirectory, _settings.Diagnostics.VerboseLogging);
                EquivalenceValidator.IsEnabled = _settings.Diagnostics.ShadowValidation;
                _deferredProfilerTargetsApplied = false;

                OptimizerLog.Info("BannerlordCpuOptimizer "
                    + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                    + " loading.");
                OptimizerLog.Info("Settings loaded from: " + settingsPath + ".");
                OptimizerLog.Info("Effective profiler configuration: enabled=" + _settings.Profiling.Enabled
                    + " torCampaign=" + _settings.Profiling.ProfileTorCampaignHandlers
                    + " torMission=" + _settings.Profiling.ProfileTorMissionHandlers
                    + " torModels=" + _settings.Profiling.ProfileTorModels
                    + " vanilla=" + _settings.Profiling.ProfileVanillaHandlers
                    + " overlay=" + _settings.Diagnostics.RuntimeOverlay + ".");
                foreach (AssemblyIdentity identity in AssemblyProbe.CaptureLoadedAssemblies())
                {
                    OptimizerLog.Info("Assembly: " + identity);
                }

                LogMilestoneState();

                if (_settings.Profiling.Enabled)
                {
                    FrameProfiler.Configure(
                        _settings.Profiling.ContextSampleSeconds,
                        _settings.Profiling.AllowUnknownProfilerTargets);
                    _profilerPatches = new HarmonyProfilerPatches(_settings);
                    _profilerPatches.Apply();
                    _overlay = _settings.Diagnostics.RuntimeOverlay ? new RuntimeOverlay() : null;
                }
                else
                {
                    OptimizerLog.Info("Profiler is disabled. No Harmony patches were applied by Milestone 1.");
                }

                _initialized = true;
            }
        }

        internal static void OnApplicationTick()
        {
            if (!ProfilingEnabled)
            {
                return;
            }

            if (!_deferredProfilerTargetsApplied && _session != null && GameCampaign.Current != null)
            {
                // OnApplicationTick cannot run inside the synchronous OnGameStart callback chain.
                // Reaching this point proves TOR has completed model registration and its text-backed
                // TORCustomResourceModel type initializer has already run under the correct game state.
                _deferredProfilerTargetsApplied = true;
                _profilerPatches?.ApplyDeferredTargets();
            }

            FrameProfiler.OnApplicationTick();
            _overlay?.Tick();
        }

        internal static void OnGameStarted()
        {
            if (!ProfilingEnabled)
            {
                return;
            }

            lock (Sync)
            {
                if (_session != null)
                {
                    WriteSession("game-restart");
                }

                StartSession();
            }
        }

        internal static void OnGameEnded()
        {
            if (!ProfilingEnabled)
            {
                LifecycleManager.OnCampaignEnded();
                return;
            }

            lock (Sync)
            {
                WriteSession("game-end");
                LifecycleManager.OnCampaignEnded();
            }
        }

        internal static void Shutdown()
        {
            lock (Sync)
            {
                if (!_initialized)
                {
                    return;
                }

                if (ProfilingEnabled)
                {
                    WriteSession("module-unload");
                    _profilerPatches?.Remove();
                }

                LifecycleManager.ClearAll();
                _overlay = null;
                _profilerPatches = null;
                _session = null;
                _settings = null;
                _deferredProfilerTargetsApplied = false;
                _initialized = false;
                OptimizerLog.Info("Teardown complete; profiler state and lifecycle references cleared.");
                OptimizerLog.Shutdown();
            }
        }

        private static void StartSession()
        {
            _session = new ProfileSession();
            OptimizerLog.Info("Profile session started: " + _session.SessionId + ".");
        }

        private static void WriteSession(string reason)
        {
            if (_session == null)
            {
                return;
            }

            try
            {
                ProfileReport report = _session.Complete();
                ProfileReportWriter.Write(report, PathProvider.ReportDirectory, _settings.Profiling.ReportFormat);
                OptimizerLog.Info("Profile session completed because of " + reason + ".");
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce(
                    "profile-report-" + _session.SessionId,
                    "Could not write profile report",
                    exception);
            }
            finally
            {
                _session = null;
            }
        }

        private static void LogMilestoneState()
        {
            OptimizerLog.Info("Milestone 1 mode: profiler-only; gameplay optimization patches are not implemented or applied.");
            OptimizerLog.Info("Configured future switches: vanilla=" + _settings.General.VanillaSafeOptimizations
                + " torCampaign=" + _settings.General.TorCampaignOptimizations
                + " torMission=" + _settings.General.TorMissionOptimizations
                + " ui=" + _settings.General.UiDirtyStateOptimizations
                + " experimentalNative=" + _settings.General.ExperimentalNativePatches
                + " fallback=" + _settings.General.AutomaticFallback + ".");
        }
    }
}
