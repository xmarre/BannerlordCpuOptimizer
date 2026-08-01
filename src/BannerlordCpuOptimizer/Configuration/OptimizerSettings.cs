using System;
using System.Runtime.Serialization;

namespace BannerlordCpuOptimizer.Configuration
{
    [DataContract]
    public sealed class OptimizerSettings
    {
        [DataMember(Order = 1)] public GeneralSettings General { get; set; } = new GeneralSettings();
        [DataMember(Order = 2)] public ProfilingSettings Profiling { get; set; } = new ProfilingSettings();
        [DataMember(Order = 3)] public DiagnosticSettings Diagnostics { get; set; } = new DiagnosticSettings();
        [DataMember(Order = 4)] public BenchmarkSettings Benchmark { get; set; } = new BenchmarkSettings();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            General = new GeneralSettings();
            Profiling = new ProfilingSettings();
            Diagnostics = new DiagnosticSettings();
            Benchmark = new BenchmarkSettings();
        }

        public static OptimizerSettings CreateDefault() => new OptimizerSettings();

        public void Normalize()
        {
            General = General ?? new GeneralSettings();
            Profiling = Profiling ?? new ProfilingSettings();
            Diagnostics = Diagnostics ?? new DiagnosticSettings();
            Benchmark = Benchmark ?? new BenchmarkSettings();

            General.CareerChoiceShadowComparisons = Clamp(General.CareerChoiceShadowComparisons, 1, 1000000);
            General.CareerChoiceMinimumDistinctIds = Clamp(General.CareerChoiceMinimumDistinctIds, 1, 10000);
            General.CareerChoiceAuditEvery = Clamp(General.CareerChoiceAuditEvery, 1, 1000000);
            if (!IsCareerChoiceMode(General.CareerChoiceCacheMode))
            {
                General.CareerChoiceCacheMode = "ShadowThenEnable";
            }

            Profiling.HighFrequencySampleEvery = Clamp(Profiling.HighFrequencySampleEvery, 1, 4096);
            Profiling.NormalSampleEvery = Clamp(Profiling.NormalSampleEvery, 1, 4096);
            Profiling.FocusedSampleEvery = Clamp(Profiling.FocusedSampleEvery, 1, 4096);
            if (Profiling.ContextSampleSeconds < 0.25)
            {
                Profiling.ContextSampleSeconds = 0.25;
            }
            else if (Profiling.ContextSampleSeconds > 60.0)
            {
                Profiling.ContextSampleSeconds = 60.0;
            }

            if (string.IsNullOrWhiteSpace(Profiling.ReportFormat))
            {
                Profiling.ReportFormat = "Both";
            }

            if (string.IsNullOrWhiteSpace(Benchmark.RunLabel))
            {
                Benchmark.RunLabel = "unnamed";
            }
            if (string.IsNullOrWhiteSpace(Benchmark.ReportFormat))
            {
                Benchmark.ReportFormat = "Both";
            }
        }

        private static bool IsCareerChoiceMode(string value)
        {
            return string.Equals(value, "Disabled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "ShadowOnly", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "ShadowThenEnable", StringComparison.OrdinalIgnoreCase);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }

    [DataContract]
    public sealed class GeneralSettings
    {
        [DataMember(Order = 1)] public bool VanillaSafeOptimizations { get; set; } = true;
        [DataMember(Order = 2)] public bool TorCampaignOptimizations { get; set; } = true;
        [DataMember(Order = 3)] public bool TorMissionOptimizations { get; set; } = true;
        [DataMember(Order = 4)] public bool UiDirtyStateOptimizations { get; set; } = true;
        [DataMember(Order = 5)] public bool ExperimentalNativePatches { get; set; } = false;
        [DataMember(Order = 6)] public bool AutomaticFallback { get; set; } = true;
        [DataMember(Order = 7)] public string CareerChoiceCacheMode { get; set; } = "ShadowThenEnable";
        [DataMember(Order = 8)] public int CareerChoiceShadowComparisons { get; set; } = 256;
        [DataMember(Order = 9)] public int CareerChoiceMinimumDistinctIds { get; set; } = 1;
        [DataMember(Order = 10)] public int CareerChoiceAuditEvery { get; set; } = 1024;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            VanillaSafeOptimizations = true;
            TorCampaignOptimizations = true;
            TorMissionOptimizations = true;
            UiDirtyStateOptimizations = true;
            ExperimentalNativePatches = false;
            AutomaticFallback = true;
            CareerChoiceCacheMode = "ShadowThenEnable";
            CareerChoiceShadowComparisons = 256;
            CareerChoiceMinimumDistinctIds = 1;
            CareerChoiceAuditEvery = 1024;
        }
    }

    [DataContract]
    public sealed class ProfilingSettings
    {
        [DataMember(Order = 1)] public bool Enabled { get; set; } = false;
        [DataMember(Order = 2)] public bool ProfileTorCampaignHandlers { get; set; } = false;
        [DataMember(Order = 3)] public bool ProfileTorMissionHandlers { get; set; } = false;
        [DataMember(Order = 4)] public bool ProfileTorModels { get; set; } = false;
        [DataMember(Order = 5)] public bool ProfileVanillaHandlers { get; set; } = false;
        [DataMember(Order = 6)] public int HighFrequencySampleEvery { get; set; } = 16;
        [DataMember(Order = 7)] public int NormalSampleEvery { get; set; } = 1;
        [DataMember(Order = 8)] public double ContextSampleSeconds { get; set; } = 1.0;
        [DataMember(Order = 9)] public string ReportFormat { get; set; } = "Both";
        [DataMember(Order = 10)] public bool AllowUnknownProfilerTargets { get; set; } = false;
        [DataMember(Order = 11)] public bool ProfileFocusedTargets { get; set; } = true;
        [DataMember(Order = 12)] public int FocusedSampleEvery { get; set; } = 1;
        [DataMember(Order = 13)] public bool EnableOptionalContextMetrics { get; set; } = false;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Enabled = false;
            ProfileTorCampaignHandlers = false;
            ProfileTorMissionHandlers = false;
            ProfileTorModels = false;
            ProfileVanillaHandlers = false;
            HighFrequencySampleEvery = 16;
            NormalSampleEvery = 1;
            ContextSampleSeconds = 1.0;
            ReportFormat = "Both";
            AllowUnknownProfilerTargets = false;
            ProfileFocusedTargets = true;
            FocusedSampleEvery = 1;
            EnableOptionalContextMetrics = false;
        }
    }

    [DataContract]
    public sealed class BenchmarkSettings
    {
        [DataMember(Order = 1)] public bool Enabled { get; set; } = false;
        [DataMember(Order = 2)] public string RunLabel { get; set; } = "optimized";
        [DataMember(Order = 3)] public string ReportFormat { get; set; } = "Both";

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Enabled = false;
            RunLabel = "optimized";
            ReportFormat = "Both";
        }
    }

    [DataContract]
    public sealed class DiagnosticSettings
    {
        [DataMember(Order = 1)] public bool ShadowValidation { get; set; } = false;
        [DataMember(Order = 2)] public bool RuntimeOverlay { get; set; } = false;
        [DataMember(Order = 3)] public bool VerboseLogging { get; set; } = false;
        [DataMember(Order = 4)] public bool LogHarmonyConflicts { get; set; } = true;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            ShadowValidation = false;
            RuntimeOverlay = false;
            VerboseLogging = false;
            LogHarmonyConflicts = true;
        }
    }
}
