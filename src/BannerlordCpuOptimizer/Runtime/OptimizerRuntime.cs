using System;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Optimization;
using BannerlordCpuOptimizer.Profiling;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class OptimizerRuntime
    {
        private static readonly object Sync = new object();
        private static OptimizerSettings _settings;
        private static HarmonyProfilerPatches _profilerPatches;
        private static CareerChoiceCachePatches _careerChoiceCachePatches;
        private static ProfileSession _session;
        private static RuntimeOverlay _overlay;
        private static bool _deferredProfilerTargetsApplied;
        private static bool _optimizationPatchAttempted;
        private static bool _gameActive;
        private static GameCampaign _observedCampaign;
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
                CareerChoiceCache.Configure(
                    _settings.General.CareerChoiceCacheMode,
                    _settings.General.CareerChoiceShadowComparisons,
                    _settings.General.CareerChoiceMinimumDistinctIds,
                    _settings.General.CareerChoiceAuditEvery);

                _deferredProfilerTargetsApplied = false;
                _optimizationPatchAttempted = false;
                _gameActive = false;
                _observedCampaign = null;

                OptimizerLog.Info("BannerlordCpuOptimizer "
                    + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                    + " loading.");
                OptimizerLog.Info("Settings loaded from: " + settingsPath + ".");
                OptimizerLog.Info("Effective profiler configuration: enabled=" + _settings.Profiling.Enabled
                    + " focused=" + _settings.Profiling.ProfileFocusedTargets
                    + " torCampaign=" + _settings.Profiling.ProfileTorCampaignHandlers
                    + " torMission=" + _settings.Profiling.ProfileTorMissionHandlers
                    + " torModels=" + _settings.Profiling.ProfileTorModels
                    + " vanilla=" + _settings.Profiling.ProfileVanillaHandlers
                    + " optionalMetrics=" + _settings.Profiling.EnableOptionalContextMetrics
                    + " overlay=" + _settings.Diagnostics.RuntimeOverlay + ".");
                OptimizerLog.Info("Effective career-choice cache configuration: mode="
                    + _settings.General.CareerChoiceCacheMode
                    + " comparisons=" + _settings.General.CareerChoiceShadowComparisons
                    + " minimumDistinctIds=" + _settings.General.CareerChoiceMinimumDistinctIds
                    + " auditEvery=" + _settings.General.CareerChoiceAuditEvery + ".");

                foreach (AssemblyIdentity identity in AssemblyProbe.CaptureLoadedAssemblies())
                {
                    OptimizerLog.Info("Assembly: " + identity);
                }

                LogMilestoneState();
                _careerChoiceCachePatches = new CareerChoiceCachePatches(_settings);

                if (_settings.Profiling.Enabled)
                {
                    FrameProfiler.Configure(
                        _settings.Profiling.ContextSampleSeconds,
                        _settings.Profiling.EnableOptionalContextMetrics);
                    _profilerPatches = new HarmonyProfilerPatches(_settings);
                    _profilerPatches.Apply();
                    _overlay = _settings.Diagnostics.RuntimeOverlay ? new RuntimeOverlay() : null;
                }
                else
                {
                    OptimizerLog.Info("Profiler is disabled. Focused optimization gating remains active independently.");
                }

                _initialized = true;
            }
        }

        internal static void OnApplicationTick()
        {
            if (!_initialized)
            {
                return;
            }

            GameCampaign currentCampaign = GameCampaign.Current;
            TrackCampaignIdentity(currentCampaign);

            if (_gameActive && currentCampaign != null)
            {
                if (!_optimizationPatchAttempted)
                {
                    _optimizationPatchAttempted = true;
                    _careerChoiceCachePatches?.Apply();
                }

                if (ProfilingEnabled && !_deferredProfilerTargetsApplied)
                {
                    _deferredProfilerTargetsApplied = true;
                    _profilerPatches?.ApplyDeferredTargets();
                }
            }

            if (!ProfilingEnabled)
            {
                return;
            }

            FrameProfiler.OnApplicationTick();
            _overlay?.Tick();
        }

        internal static void OnGameStarted()
        {
            lock (Sync)
            {
                if (ProfilingEnabled && _session != null)
                {
                    WriteSession("game-restart");
                }

                _gameActive = true;
                _observedCampaign = GameCampaign.Current;
                CareerChoiceCache.BeginGameSession(_observedCampaign);

                if (ProfilingEnabled)
                {
                    StartSession();
                }
            }
        }

        internal static void OnGameEnded()
        {
            lock (Sync)
            {
                _gameActive = false;
                _observedCampaign = null;
                if (ProfilingEnabled)
                {
                    WriteSession("game-end");
                }

                CareerChoiceCache.EndGameSession();
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

                _gameActive = false;
                _observedCampaign = null;
                if (ProfilingEnabled)
                {
                    WriteSession("module-unload");
                    _profilerPatches?.Remove();
                }

                _careerChoiceCachePatches?.Remove();
                LifecycleManager.ClearAll();
                _overlay = null;
                _profilerPatches = null;
                _careerChoiceCachePatches = null;
                _session = null;
                _settings = null;
                _deferredProfilerTargetsApplied = false;
                _optimizationPatchAttempted = false;
                _initialized = false;
                OptimizerLog.Info("Teardown complete; profiler, optimization, cache, and lifecycle state cleared.");
                OptimizerLog.Shutdown();
            }
        }

        private static void TrackCampaignIdentity(GameCampaign currentCampaign)
        {
            if (!_gameActive)
            {
                return;
            }

            if (ReferenceEquals(_observedCampaign, currentCampaign))
            {
                return;
            }

            _observedCampaign = currentCampaign;
            if (currentCampaign == null)
            {
                CareerChoiceCache.EndGameSession();
                OptimizerLog.Info("Career-choice cache cleared because Campaign.Current became unavailable.");
                return;
            }

            CareerChoiceCache.BeginGameSession(currentCampaign);
            OptimizerLog.Info("Career-choice cache entered shadow validation for the current campaign instance.");
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
            OptimizerLog.Info("Milestone 2 mode: one strictly gated TOR campaign optimization plus focused profiling.");
            OptimizerLog.Info("Active optimization boundary: TORCareerChoices.GetChoice reference cache only; no AI, simulation, mission, UI, native, or background-thread changes.");
            OptimizerLog.Info("Configured switches: vanilla=" + _settings.General.VanillaSafeOptimizations
                + " torCampaign=" + _settings.General.TorCampaignOptimizations
                + " torMission=" + _settings.General.TorMissionOptimizations
                + " ui=" + _settings.General.UiDirtyStateOptimizations
                + " experimentalNative=" + _settings.General.ExperimentalNativePatches
                + " fallback=" + _settings.General.AutomaticFallback + ".");
        }
    }
}
