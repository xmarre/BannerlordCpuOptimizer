using System.Runtime.Serialization;

namespace BannerlordCpuOptimizer.Configuration
{
    [DataContract]
    public sealed class OptimizerSettings
    {
        [DataMember(Order = 1)]
        public GeneralSettings General { get; set; } = new GeneralSettings();

        [DataMember(Order = 2)]
        public ProfilingSettings Profiling { get; set; } = new ProfilingSettings();

        [DataMember(Order = 3)]
        public DiagnosticSettings Diagnostics { get; set; } = new DiagnosticSettings();

        public static OptimizerSettings CreateDefault()
        {
            return new OptimizerSettings();
        }

        public void Normalize()
        {
            General = General ?? new GeneralSettings();
            Profiling = Profiling ?? new ProfilingSettings();
            Diagnostics = Diagnostics ?? new DiagnosticSettings();
            Profiling.HighFrequencySampleEvery = Clamp(Profiling.HighFrequencySampleEvery, 1, 4096);
            Profiling.NormalSampleEvery = Clamp(Profiling.NormalSampleEvery, 1, 4096);
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
    }

    [DataContract]
    public sealed class ProfilingSettings
    {
        [DataMember(Order = 1)] public bool Enabled { get; set; } = false;
        [DataMember(Order = 2)] public bool ProfileTorCampaignHandlers { get; set; } = true;
        [DataMember(Order = 3)] public bool ProfileTorMissionHandlers { get; set; } = true;
        [DataMember(Order = 4)] public bool ProfileTorModels { get; set; } = true;
        [DataMember(Order = 5)] public bool ProfileVanillaHandlers { get; set; } = false;
        [DataMember(Order = 6)] public int HighFrequencySampleEvery { get; set; } = 16;
        [DataMember(Order = 7)] public int NormalSampleEvery { get; set; } = 1;
        [DataMember(Order = 8)] public double ContextSampleSeconds { get; set; } = 1.0;
        [DataMember(Order = 9)] public string ReportFormat { get; set; } = "Both";
        [DataMember(Order = 10)] public bool AllowUnknownProfilerTargets { get; set; } = false;
    }

    [DataContract]
    public sealed class DiagnosticSettings
    {
        [DataMember(Order = 1)] public bool ShadowValidation { get; set; } = false;
        [DataMember(Order = 2)] public bool RuntimeOverlay { get; set; } = false;
        [DataMember(Order = 3)] public bool VerboseLogging { get; set; } = false;
        [DataMember(Order = 4)] public bool LogHarmonyConflicts { get; set; } = true;
    }
}
