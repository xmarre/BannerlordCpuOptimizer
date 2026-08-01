using System;
using System.Reflection;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace BannerlordCpuOptimizer.Configuration
{
    internal sealed class OptimizerMcmSettings : AttributeGlobalSettings<OptimizerMcmSettings>
    {
        internal const string NormalMode = "Normal Gameplay";
        internal const string BaselineMode = "Benchmark - Baseline";
        internal const string OptimizedMode = "Benchmark - Optimized";
        internal const string FocusedProfilerMode = "Focused Profiler";
        internal const string CustomMode = "Custom";

        private static readonly string[] RunModes =
        {
            NormalMode,
            BaselineMode,
            OptimizedMode,
            FocusedProfilerMode,
            CustomMode
        };

        private static readonly string[] CacheModes =
        {
            "Disabled",
            "ShadowOnly",
            "ShadowThenEnable"
        };

        private static readonly string[] ReportFormats =
        {
            "Both",
            "Json",
            "Csv"
        };

        public override string Id => "BannerlordCpuOptimizer_v1";
        public override string DisplayName => "Bannerlord CPU Optimizer "
            + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown");
        public override string FolderName => "BannerlordCpuOptimizer";
        public override string FormatType => "json2";

        [SettingPropertyDropdown(
            "Operating Mode",
            Order = 0,
            RequireRestart = true,
            HintText = "Normal Gameplay uses the selected validated optimizations without measurement overhead. Baseline disables every optimizer patch. Optimized enables every released safe patch. Focused Profiler enables attribution and the released optimizations.")]
        [SettingPropertyGroup("Run Mode", GroupOrder = 0)]
        public Dropdown<string> RunMode { get; set; } = new Dropdown<string>(RunModes, 0);

        [SettingPropertyText(
            "Custom Run Label",
            Order = 1,
            RequireRestart = true,
            HintText = "Used for Custom and Focused Profiler reports. Baseline and Optimized use fixed comparison-safe labels.")]
        [SettingPropertyGroup("Run Mode", GroupOrder = 0)]
        public string CustomRunLabel { get; set; } = "custom";

        [SettingPropertyBool("Enable Benchmark in Custom Mode", Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Run Mode", GroupOrder = 0)]
        public bool CustomBenchmarkEnabled { get; set; }

        [SettingPropertyBool(
            "Enable Profiler in Custom Mode",
            Order = 3,
            RequireRestart = true,
            HintText = "Profiling adds overhead and must remain disabled during A/B benchmarks.")]
        [SettingPropertyGroup("Run Mode", GroupOrder = 0)]
        public bool CustomProfilerEnabled { get; set; }

        [SettingPropertyDropdown("Benchmark Report Format", Order = 4, RequireRestart = true)]
        [SettingPropertyGroup("Run Mode", GroupOrder = 0)]
        public Dropdown<string> BenchmarkReportFormat { get; set; } = new Dropdown<string>(ReportFormats, 0);

        [SettingPropertyBool("Vanilla Safe Optimizations", Order = 0, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool VanillaSafeOptimizations { get; set; } = true;

        [SettingPropertyBool("TOR Campaign Optimizations", Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool TorCampaignOptimizations { get; set; } = true;

        [SettingPropertyBool("TOR Mission Optimizations", Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool TorMissionOptimizations { get; set; } = true;

        [SettingPropertyBool("UI Dirty-State Optimizations", Order = 3, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool UiDirtyStateOptimizations { get; set; } = true;

        [SettingPropertyBool(
            "Experimental Native Patches",
            Order = 4,
            RequireRestart = true,
            HintText = "Keep disabled. No experimental native patch is released.")]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool ExperimentalNativePatches { get; set; }

        [SettingPropertyBool(
            "Automatic Fallback",
            Order = 5,
            RequireRestart = true,
            HintText = "Keep enabled. Any validation mismatch returns immediately to original Bannerlord or TOR behavior.")]
        [SettingPropertyGroup("Optimization\\Feature Gates", GroupOrder = 1)]
        public bool AutomaticFallback { get; set; } = true;

        [SettingPropertyDropdown(
            "Career-Choice Cache Mode",
            Order = 0,
            RequireRestart = true,
            HintText = "Baseline forces Disabled. Optimized and Focused Profiler force ShadowThenEnable.")]
        [SettingPropertyGroup("Optimization\\Career-Choice Cache", GroupOrder = 1)]
        public Dropdown<string> CareerChoiceCacheMode { get; set; } = new Dropdown<string>(CacheModes, 2);

        [SettingPropertyInteger("Shadow Comparisons Before Activation", 1, 1000000, Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Career-Choice Cache", GroupOrder = 1)]
        public int CareerChoiceShadowComparisons { get; set; } = 256;

        [SettingPropertyInteger("Minimum Distinct IDs", 1, 10000, Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Career-Choice Cache", GroupOrder = 1)]
        public int CareerChoiceMinimumDistinctIds { get; set; } = 1;

        [SettingPropertyInteger("Audit Every N Cache Hits", 1, 1000000, Order = 3, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\Career-Choice Cache", GroupOrder = 1)]
        public int CareerChoiceAuditEvery { get; set; } = 1024;

        [SettingPropertyBool(
            "Map Visibility Early Exit",
            Order = 0,
            RequireRestart = true,
            HintText = "Replaces TOR's temporary nearby-settlement list with an exact early-exit existence check after shadow validation.")]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public bool MapVisibilityEarlyExit { get; set; } = true;

        [SettingPropertyInteger("Visibility Shadow Comparisons", 1, 1000000, Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public int MapVisibilityShadowComparisons { get; set; } = 512;

        [SettingPropertyInteger("Visibility Audit Every N Calls", 1, 1000000, Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public int MapVisibilityAuditEvery { get; set; } = 2048;

        [SettingPropertyBool(
            "Fixed Race Lookup Cache",
            Order = 3,
            RequireRestart = true,
            HintText = "Caches only exact TOR fixed race-ID lookups used by health and race classification. Final hit-point values are never cached.")]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public bool RaceLookupCache { get; set; } = true;

        [SettingPropertyInteger("Race Lookup Shadow Comparisons", 1, 1000000, Order = 4, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public int RaceLookupShadowComparisons { get; set; } = 256;

        [SettingPropertyInteger("Race Lookup Audit Every N Hits", 1, 1000000, Order = 5, RequireRestart = true)]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public int RaceLookupAuditEvery { get; set; } = 4096;

        [SettingPropertyBool(
            "Weekly Companion LINQ Elision",
            Order = 6,
            RequireRestart = true,
            HintText = "Preserves TOR's weekly schedule, ordering, predicates, randomization, and side effects while removing two temporary iterator chains.")]
        [SettingPropertyGroup("Optimization\\TOR Campaign", GroupOrder = 1)]
        public bool WeeklyCompanionLinqElision { get; set; } = true;

        [SettingPropertyBool("Profile Focused Targets", Order = 0, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Targets", GroupOrder = 2)]
        public bool ProfileFocusedTargets { get; set; } = true;

        [SettingPropertyBool("Profile TOR Campaign Handlers", Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Targets", GroupOrder = 2)]
        public bool ProfileTorCampaignHandlers { get; set; }

        [SettingPropertyBool("Profile TOR Mission Handlers", Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Targets", GroupOrder = 2)]
        public bool ProfileTorMissionHandlers { get; set; }

        [SettingPropertyBool("Profile TOR Models", Order = 3, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Targets", GroupOrder = 2)]
        public bool ProfileTorModels { get; set; }

        [SettingPropertyBool("Profile Vanilla Handlers", Order = 4, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Targets", GroupOrder = 2)]
        public bool ProfileVanillaHandlers { get; set; }

        [SettingPropertyInteger("High-Frequency Sample Every", 1, 4096, Order = 0, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Sampling", GroupOrder = 2)]
        public int HighFrequencySampleEvery { get; set; } = 16;

        [SettingPropertyInteger("Normal Sample Every", 1, 4096, Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Sampling", GroupOrder = 2)]
        public int NormalSampleEvery { get; set; } = 1;

        [SettingPropertyInteger("Focused Sample Every", 1, 4096, Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Sampling", GroupOrder = 2)]
        public int FocusedSampleEvery { get; set; } = 1;

        [SettingPropertyFloatingInteger("Context Sample Seconds", 0.25f, 60.0f, Order = 3, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Sampling", GroupOrder = 2)]
        public float ContextSampleSeconds { get; set; } = 1.0f;

        [SettingPropertyDropdown("Profiler Report Format", Order = 4, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Sampling", GroupOrder = 2)]
        public Dropdown<string> ProfilerReportFormat { get; set; } = new Dropdown<string>(ReportFormats, 0);

        [SettingPropertyBool(
            "Allow Unknown Profiler Targets",
            Order = 5,
            RequireRestart = true,
            HintText = "Observation only. Never relaxes optimization gates.")]
        [SettingPropertyGroup("Profiler\\Advanced", GroupOrder = 2)]
        public bool AllowUnknownProfilerTargets { get; set; }

        [SettingPropertyBool("Enable Optional Context Metrics", Order = 6, RequireRestart = true)]
        [SettingPropertyGroup("Profiler\\Advanced", GroupOrder = 2)]
        public bool EnableOptionalContextMetrics { get; set; }

        [SettingPropertyBool("Shadow Validation", Order = 0, RequireRestart = true)]
        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        public bool ShadowValidation { get; set; }

        [SettingPropertyBool("Runtime Overlay", Order = 1, RequireRestart = true)]
        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        public bool RuntimeOverlay { get; set; }

        [SettingPropertyBool("Verbose Logging", Order = 2, RequireRestart = true)]
        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        public bool VerboseLogging { get; set; }

        [SettingPropertyBool("Log Harmony Conflicts", Order = 3, RequireRestart = true)]
        [SettingPropertyGroup("Diagnostics", GroupOrder = 3)]
        public bool LogHarmonyConflicts { get; set; } = true;

        internal string EffectiveRunMode => Selected(RunMode, NormalMode);

        internal OptimizerSettings BuildRuntimeSettings()
        {
            var settings = OptimizerSettings.CreateDefault();

            settings.General.VanillaSafeOptimizations = VanillaSafeOptimizations;
            settings.General.TorCampaignOptimizations = TorCampaignOptimizations;
            settings.General.TorMissionOptimizations = TorMissionOptimizations;
            settings.General.UiDirtyStateOptimizations = UiDirtyStateOptimizations;
            settings.General.ExperimentalNativePatches = ExperimentalNativePatches;
            settings.General.AutomaticFallback = AutomaticFallback;
            settings.General.CareerChoiceCacheMode = Selected(CareerChoiceCacheMode, "ShadowThenEnable");
            settings.General.CareerChoiceShadowComparisons = CareerChoiceShadowComparisons;
            settings.General.CareerChoiceMinimumDistinctIds = CareerChoiceMinimumDistinctIds;
            settings.General.CareerChoiceAuditEvery = CareerChoiceAuditEvery;
            settings.General.MapVisibilityEarlyExit = MapVisibilityEarlyExit;
            settings.General.MapVisibilityShadowComparisons = MapVisibilityShadowComparisons;
            settings.General.MapVisibilityAuditEvery = MapVisibilityAuditEvery;
            settings.General.RaceLookupCache = RaceLookupCache;
            settings.General.RaceLookupShadowComparisons = RaceLookupShadowComparisons;
            settings.General.RaceLookupAuditEvery = RaceLookupAuditEvery;
            settings.General.WeeklyCompanionLinqElision = WeeklyCompanionLinqElision;

            settings.Profiling.ProfileTorCampaignHandlers = ProfileTorCampaignHandlers;
            settings.Profiling.ProfileTorMissionHandlers = ProfileTorMissionHandlers;
            settings.Profiling.ProfileTorModels = ProfileTorModels;
            settings.Profiling.ProfileVanillaHandlers = ProfileVanillaHandlers;
            settings.Profiling.HighFrequencySampleEvery = HighFrequencySampleEvery;
            settings.Profiling.NormalSampleEvery = NormalSampleEvery;
            settings.Profiling.ContextSampleSeconds = ContextSampleSeconds;
            settings.Profiling.ReportFormat = Selected(ProfilerReportFormat, "Both");
            settings.Profiling.AllowUnknownProfilerTargets = AllowUnknownProfilerTargets;
            settings.Profiling.ProfileFocusedTargets = ProfileFocusedTargets;
            settings.Profiling.FocusedSampleEvery = FocusedSampleEvery;
            settings.Profiling.EnableOptionalContextMetrics = EnableOptionalContextMetrics;

            settings.Benchmark.RunLabel = string.IsNullOrWhiteSpace(CustomRunLabel) ? "custom" : CustomRunLabel.Trim();
            settings.Benchmark.ReportFormat = Selected(BenchmarkReportFormat, "Both");

            settings.Diagnostics.ShadowValidation = ShadowValidation;
            settings.Diagnostics.RuntimeOverlay = RuntimeOverlay;
            settings.Diagnostics.VerboseLogging = VerboseLogging;
            settings.Diagnostics.LogHarmonyConflicts = LogHarmonyConflicts;

            switch (EffectiveRunMode)
            {
                case BaselineMode:
                    settings.Profiling.Enabled = false;
                    settings.Benchmark.Enabled = true;
                    settings.Benchmark.RunLabel = "baseline-all-optimizations-disabled";
                    settings.General.VanillaSafeOptimizations = false;
                    settings.General.TorCampaignOptimizations = false;
                    settings.General.TorMissionOptimizations = false;
                    settings.General.UiDirtyStateOptimizations = false;
                    settings.General.CareerChoiceCacheMode = "Disabled";
                    settings.General.MapVisibilityEarlyExit = false;
                    settings.General.RaceLookupCache = false;
                    settings.General.WeeklyCompanionLinqElision = false;
                    break;

                case OptimizedMode:
                    settings.Profiling.Enabled = false;
                    settings.Benchmark.Enabled = true;
                    settings.Benchmark.RunLabel = "optimized-all-safe-optimizations-enabled";
                    settings.General.VanillaSafeOptimizations = true;
                    settings.General.TorCampaignOptimizations = true;
                    settings.General.TorMissionOptimizations = true;
                    settings.General.UiDirtyStateOptimizations = true;
                    settings.General.CareerChoiceCacheMode = "ShadowThenEnable";
                    settings.General.MapVisibilityEarlyExit = true;
                    settings.General.RaceLookupCache = true;
                    settings.General.WeeklyCompanionLinqElision = true;
                    break;

                case FocusedProfilerMode:
                    settings.Profiling.Enabled = true;
                    settings.Profiling.ProfileFocusedTargets = true;
                    settings.Benchmark.Enabled = true;
                    settings.Benchmark.RunLabel = string.IsNullOrWhiteSpace(CustomRunLabel)
                        ? "focused-profiler"
                        : CustomRunLabel.Trim();
                    settings.General.TorCampaignOptimizations = true;
                    settings.General.CareerChoiceCacheMode = "ShadowThenEnable";
                    settings.General.MapVisibilityEarlyExit = true;
                    settings.General.RaceLookupCache = true;
                    settings.General.WeeklyCompanionLinqElision = true;
                    break;

                case CustomMode:
                    settings.Profiling.Enabled = CustomProfilerEnabled;
                    settings.Benchmark.Enabled = CustomBenchmarkEnabled;
                    break;

                default:
                    settings.Profiling.Enabled = false;
                    settings.Benchmark.Enabled = false;
                    break;
            }

            settings.Normalize();
            return settings;
        }

        private static string Selected(Dropdown<string> dropdown, string fallback)
        {
            if (dropdown == null || dropdown.Count == 0)
            {
                return fallback;
            }

            int index = dropdown.SelectedIndex;
            return index >= 0 && index < dropdown.Count && !string.IsNullOrWhiteSpace(dropdown[index])
                ? dropdown[index]
                : fallback;
        }
    }
}
