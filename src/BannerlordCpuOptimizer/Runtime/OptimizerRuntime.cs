using System;
using System.Globalization;
using System.Reflection;
using BannerlordCpuOptimizer.Benchmarking;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Optimization;
using BannerlordCpuOptimizer.Profiling;
using TaleWorlds.CampaignSystem;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class OptimizerRuntime
    {
        private const int AutomaticBenchmarkTargetHours = 200;

        private static readonly object Sync = new object();
        private static OptimizerSettings _settings;
        private static HarmonyProfilerPatches _profilerPatches;
        private static CareerChoiceCachePatches _careerChoiceCachePatches;
        private static ProfileSession _session;
        private static BenchmarkSession _benchmarkSession;
        private static MeasurementStartGate _measurementStartGate;
        private static RuntimeOverlay _overlay;
        private static bool _deferredProfilerTargetsApplied;
        private static bool _optimizationPatchAttempted;
        private static bool _gameActive;
        private static GameCampaign _observedCampaign;
        private static int _benchmarkCampaignHours;
        private static bool _initialized;

        internal static bool ProfilingEnabled => _settings?.Profiling.Enabled == true;
        internal static bool BenchmarkEnabled => _settings?.Benchmark.Enabled == true;
        internal static bool MeasurementEnabled => ProfilingEnabled || BenchmarkEnabled;

        internal static void Initialize()
        {
            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }

                string settingsSource;
                Exception mcmFailure = null;
                try
                {
                    OptimizerMcmSettings mcmSettings = OptimizerMcmSettings.Instance;
                    if (mcmSettings != null)
                    {
                        _settings = mcmSettings.BuildRuntimeSettings();
                        settingsSource = "MCM global settings (mode=" + mcmSettings.EffectiveRunMode + ")";
                    }
                    else
                    {
                        string settingsPath = PathProvider.ResolveSettingsPath();
                        _settings = SettingsLoader.LoadOrCreate(settingsPath);
                        settingsSource = settingsPath + " (legacy fallback because MCM settings were unavailable)";
                    }
                }
                catch (Exception exception)
                {
                    mcmFailure = exception;
                    string settingsPath = PathProvider.ResolveSettingsPath();
                    _settings = SettingsLoader.LoadOrCreate(settingsPath);
                    settingsSource = settingsPath + " (legacy fallback after MCM settings failure)";
                }

                _settings.Normalize();
                OptimizerLog.Initialize(PathProvider.LogDirectory, _settings.Diagnostics.VerboseLogging);
                EquivalenceValidator.IsEnabled = _settings.Diagnostics.ShadowValidation;
                CareerChoiceCache.Configure(
                    _settings.General.CareerChoiceCacheMode,
                    _settings.General.CareerChoiceShadowComparisons,
                    _settings.General.CareerChoiceMinimumDistinctIds,
                    _settings.General.CareerChoiceAuditEvery);

                _measurementStartGate = new MeasurementStartGate();
                _deferredProfilerTargetsApplied = false;
                _optimizationPatchAttempted = false;
                _gameActive = false;
                _observedCampaign = null;
                _benchmarkCampaignHours = 0;

                OptimizerLog.Info("BannerlordCpuOptimizer "
                    + (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                    + " loading.");
                OptimizerLog.Info("Settings source: " + settingsSource + ".");
                if (mcmFailure != null)
                {
                    OptimizerLog.WriteExceptionOnce(
                        "mcm-settings-load",
                        "Could not load MCM settings; legacy JSON fallback was used",
                        mcmFailure);
                }

                OptimizerLog.Info("Effective profiler configuration: enabled=" + _settings.Profiling.Enabled
                    + " focused=" + _settings.Profiling.ProfileFocusedTargets
                    + " torCampaign=" + _settings.Profiling.ProfileTorCampaignHandlers
                    + " torMission=" + _settings.Profiling.ProfileTorMissionHandlers
                    + " torModels=" + _settings.Profiling.ProfileTorModels
                    + " vanilla=" + _settings.Profiling.ProfileVanillaHandlers
                    + " optionalMetrics=" + _settings.Profiling.EnableOptionalContextMetrics
                    + " overlay=" + _settings.Diagnostics.RuntimeOverlay + ".");
                OptimizerLog.Info("Effective benchmark configuration: enabled=" + _settings.Benchmark.Enabled
                    + " label=" + _settings.Benchmark.RunLabel
                    + " format=" + _settings.Benchmark.ReportFormat
                    + " automaticTargetHours=" + AutomaticBenchmarkTargetHours
                    + " startGate=maximum-campaign-speed"
                    + " stableSeconds=" + MeasurementStartGate.RequiredStableSeconds.ToString("0.0", CultureInfo.InvariantCulture)
                    + ".");
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
                    OptimizerLog.Info("Profiler is disabled. Optimization and benchmark gating remain independent.");
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

            TryStartArmedMeasurement(currentCampaign);
            _benchmarkSession?.OnApplicationTick();
            if (!ProfilingEnabled)
            {
                return;
            }

            FrameProfiler.OnApplicationTick();
            _overlay?.Tick();
        }

        internal static void OnCampaignHourElapsed()
        {
            lock (Sync)
            {
                _benchmarkSession?.CampaignHourElapsed();
                if (_benchmarkSession == null)
                {
                    return;
                }

                _benchmarkCampaignHours++;
                if (_benchmarkCampaignHours < AutomaticBenchmarkTargetHours)
                {
                    return;
                }

                string completionReason = "automatic-target-" + AutomaticBenchmarkTargetHours + "-campaign-hours";
                WriteBenchmark(completionReason);
                if (ProfilingEnabled)
                {
                    WriteSession(completionReason);
                }

                OptimizerLog.Info("Automatic measurement target reached after "
                    + AutomaticBenchmarkTargetHours + " campaign hours; all active reports were written.");
                TaleWorlds.Library.InformationManager.DisplayMessage(
                    new TaleWorlds.Library.InformationMessage(
                        "Bannerlord CPU Optimizer: " + AutomaticBenchmarkTargetHours
                        + " campaign-hour run complete. Reports were written; you can exit normally."));
            }
        }

        internal static void OnGameStarted()
        {
            Initialize();
            lock (Sync)
            {
                if (ProfilingEnabled && _session != null)
                {
                    WriteSession("game-restart");
                }
                if (BenchmarkEnabled && _benchmarkSession != null)
                {
                    WriteBenchmark("game-restart");
                }

                _measurementStartGate?.Disarm();
                _gameActive = true;
                _observedCampaign = GameCampaign.Current;
                CareerChoiceCache.BeginGameSession(_observedCampaign);
                ResetCampaignOptimizationValidation();

                if (MeasurementEnabled)
                {
                    ArmMeasurementStart();
                }
            }
        }

        internal static void OnGameEnded()
        {
            lock (Sync)
            {
                _measurementStartGate?.Disarm();
                _gameActive = false;
                _observedCampaign = null;
                if (ProfilingEnabled)
                {
                    WriteSession("game-end");
                }
                if (BenchmarkEnabled)
                {
                    WriteBenchmark("game-end");
                }

                _benchmarkCampaignHours = 0;
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

                _measurementStartGate?.Disarm();
                _gameActive = false;
                _observedCampaign = null;
                if (ProfilingEnabled)
                {
                    WriteSession("module-unload");
                    _profilerPatches?.Remove();
                }
                if (BenchmarkEnabled)
                {
                    WriteBenchmark("module-unload");
                }

                _careerChoiceCachePatches?.Remove();
                LifecycleManager.ClearAll();
                _overlay = null;
                _profilerPatches = null;
                _careerChoiceCachePatches = null;
                _measurementStartGate = null;
                _session = null;
                _benchmarkSession = null;
                _settings = null;
                _deferredProfilerTargetsApplied = false;
                _optimizationPatchAttempted = false;
                _benchmarkCampaignHours = 0;
                _initialized = false;
                OptimizerLog.Info("Teardown complete; profiler, benchmark, start gate, optimization, cache, and lifecycle state cleared.");
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
                OptimizerLog.Info("Campaign-bound optimizer state cleared because Campaign.Current became unavailable.");
                return;
            }

            CareerChoiceCache.BeginGameSession(currentCampaign);
            ResetCampaignOptimizationValidation();
            OptimizerLog.Info("All TOR campaign optimizations re-entered validation for the current campaign instance.");
        }

        private static void TryStartArmedMeasurement(GameCampaign currentCampaign)
        {
            MeasurementStartGate gate = _measurementStartGate;
            if (gate == null || !gate.TryOpen(currentCampaign, out CampaignTimeControlMode startMode))
            {
                return;
            }

            lock (Sync)
            {
                if (!_gameActive
                    || currentCampaign == null
                    || !ReferenceEquals(currentCampaign, GameCampaign.Current))
                {
                    gate.Arm();
                    return;
                }

                CareerChoiceCache.BeginGameSession(currentCampaign);
                ResetCampaignOptimizationValidation();

                if (ProfilingEnabled)
                {
                    StartSession();
                }
                if (BenchmarkEnabled)
                {
                    StartBenchmark(startMode);
                }

                OptimizerLog.Info("Measurement started after maximum campaign speed remained stable for "
                    + MeasurementStartGate.RequiredStableSeconds.ToString("0.0", CultureInfo.InvariantCulture)
                    + " second(s); startMode=" + startMode + ". All measurement and optimization counters were reset at the boundary.");
                TaleWorlds.Library.InformationManager.DisplayMessage(
                    new TaleWorlds.Library.InformationMessage(
                        "Bannerlord CPU Optimizer: measurement started at stable maximum campaign speed. "
                        + AutomaticBenchmarkTargetHours + " campaign hours remaining."));
            }
        }

        private static void ArmMeasurementStart()
        {
            _benchmarkCampaignHours = 0;
            _measurementStartGate?.Arm();
            OptimizerLog.Info("Measurement armed. Waiting for maximum campaign speed to remain stable for "
                + MeasurementStartGate.RequiredStableSeconds.ToString("0.0", CultureInfo.InvariantCulture)
                + " second(s) before counters begin.");
            TaleWorlds.Library.InformationManager.DisplayMessage(
                new TaleWorlds.Library.InformationMessage(
                    "Bannerlord CPU Optimizer: set campaign speed to maximum. Measurement will begin after "
                    + MeasurementStartGate.RequiredStableSeconds.ToString("0.0", CultureInfo.InvariantCulture)
                    + " seconds of stable fast-forward."));
        }

        private static void ResetCampaignOptimizationValidation()
        {
            MapVisibilityEarlyExit.ResetSession();
            RaceLookupCache.ResetSession();
            WeeklyCompanionLinqElision.Reset();
        }

        private static void StartSession()
        {
            _session = new ProfileSession();
            OptimizerLog.Info("Profile session started: " + _session.SessionId + ".");
        }

        private static void StartBenchmark(CampaignTimeControlMode startMode)
        {
            _benchmarkCampaignHours = 0;
            _benchmarkSession = new BenchmarkSession(
                _settings.Benchmark.RunLabel,
                _settings.General.CareerChoiceCacheMode,
                ProfilingEnabled,
                "maximum-campaign-speed-stable",
                startMode.ToString(),
                MeasurementStartGate.RequiredStableSeconds);
            OptimizerLog.Info("Whole-process benchmark started: " + _benchmarkSession.SessionId
                + " label=" + _benchmarkSession.RunLabel
                + " startMode=" + startMode
                + " automaticTargetHours=" + AutomaticBenchmarkTargetHours + ".");
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

        private static void WriteBenchmark(string reason)
        {
            if (_benchmarkSession == null)
            {
                return;
            }

            try
            {
                BenchmarkReport report = _benchmarkSession.Complete(reason);
                BenchmarkReportWriter.Write(report, PathProvider.ReportDirectory, _settings.Benchmark.ReportFormat);
                OptimizerLog.Info("Whole-process benchmark completed because of " + reason + ".");
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce(
                    "benchmark-report-" + _benchmarkSession.SessionId,
                    "Could not write whole-process benchmark report",
                    exception);
            }
            finally
            {
                _benchmarkSession = null;
            }
        }

        private static void LogMilestoneState()
        {
            OptimizerLog.Info("Milestone 4 mode: exact-gated TOR career-choice, map-visibility, fixed-race, and weekly-companion optimizations with MCM, whole-process A/B benchmarking, focused attribution, stable maximum-speed start, and automatic 200-hour completion.");
            OptimizerLog.Info("Active optimization boundary: no final hit-point, visibility, companion, AI, random, mission, or save-state value is cached or rescheduled.");
            OptimizerLog.Info("Configured switches: vanilla=" + _settings.General.VanillaSafeOptimizations
                + " torCampaign=" + _settings.General.TorCampaignOptimizations
                + " torMission=" + _settings.General.TorMissionOptimizations
                + " ui=" + _settings.General.UiDirtyStateOptimizations
                + " mapVisibility=" + _settings.General.MapVisibilityEarlyExit
                + " raceLookup=" + _settings.General.RaceLookupCache
                + " weeklyCompanions=" + _settings.General.WeeklyCompanionLinqElision
                + " experimentalNative=" + _settings.General.ExperimentalNativePatches
                + " fallback=" + _settings.General.AutomaticFallback + ".");
        }
    }
}
