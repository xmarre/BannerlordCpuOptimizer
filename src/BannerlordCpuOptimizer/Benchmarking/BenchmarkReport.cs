using System.Runtime.Serialization;
using BannerlordCpuOptimizer.Optimization;

namespace BannerlordCpuOptimizer.Benchmarking
{
    [DataContract]
    internal sealed class BenchmarkReport
    {
        [DataMember(Order = 1)] public string SessionId { get; set; }
        [DataMember(Order = 2)] public string RunLabel { get; set; }
        [DataMember(Order = 3)] public string CompletionReason { get; set; }
        [DataMember(Order = 4)] public string StartedUtc { get; set; }
        [DataMember(Order = 5)] public string EndedUtc { get; set; }
        [DataMember(Order = 6)] public string OptimizerVersion { get; set; }
        [DataMember(Order = 7)] public bool ProfilingEnabled { get; set; }
        [DataMember(Order = 8)] public string CareerChoiceCacheMode { get; set; }
        [DataMember(Order = 9)] public int LogicalProcessorCount { get; set; }
        [DataMember(Order = 10)] public double WallSeconds { get; set; }
        [DataMember(Order = 11)] public double ProcessCpuSeconds { get; set; }
        [DataMember(Order = 12)] public double ProcessCpuPercentOfOneLogicalCore { get; set; }
        [DataMember(Order = 13)] public double ProcessCpuPercentOfWholeMachine { get; set; }
        [DataMember(Order = 14)] public long ApplicationTicks { get; set; }
        [DataMember(Order = 15)] public double ApplicationTicksPerSecond { get; set; }
        [DataMember(Order = 16)] public double AverageFrameMilliseconds { get; set; }
        [DataMember(Order = 17)] public double P50FrameMilliseconds { get; set; }
        [DataMember(Order = 18)] public double P95FrameMilliseconds { get; set; }
        [DataMember(Order = 19)] public double P99FrameMilliseconds { get; set; }
        [DataMember(Order = 20)] public double MaximumFrameMilliseconds { get; set; }
        [DataMember(Order = 21)] public long CampaignHours { get; set; }
        [DataMember(Order = 22)] public double CampaignHoursPerRealMinute { get; set; }
        [DataMember(Order = 23)] public long Missions { get; set; }
        [DataMember(Order = 24)] public int Gen0CollectionsDelta { get; set; }
        [DataMember(Order = 25)] public int Gen1CollectionsDelta { get; set; }
        [DataMember(Order = 26)] public int Gen2CollectionsDelta { get; set; }
        [DataMember(Order = 27)] public long ManagedBytesStart { get; set; }
        [DataMember(Order = 28)] public long ManagedBytesEnd { get; set; }
        [DataMember(Order = 29)] public long ManagedBytesDelta { get; set; }
        [DataMember(Order = 30)] public CareerChoiceCacheSnapshot CareerChoiceCache { get; set; }
        [DataMember(Order = 31)] public string MapVisibilityOptimization { get; set; }
        [DataMember(Order = 32)] public string RaceLookupOptimization { get; set; }
        [DataMember(Order = 33)] public string WeeklyCompanionOptimization { get; set; }
        [DataMember(Order = 34)] public string Notes { get; set; }
        [DataMember(Order = 35)] public double ProcessCpuSecondsPerCampaignHour { get; set; }
        [DataMember(Order = 36)] public double WallSecondsPerCampaignHour { get; set; }
        [DataMember(Order = 37)] public bool ProcessCpuMeasurementAvailable { get; set; }
        [DataMember(Order = 38)] public string StartCondition { get; set; }
        [DataMember(Order = 39)] public string StartTimeControlMode { get; set; }
        [DataMember(Order = 40)] public double StartStabilitySeconds { get; set; }
        [DataMember(Order = 41)] public double ApplicationTicksPerCampaignHour { get; set; }
        [DataMember(Order = 42)] public double ProcessCpuMillisecondsPerApplicationTick { get; set; }
        [DataMember(Order = 43)] public double WallMillisecondsPerApplicationTick { get; set; }
    }
}
