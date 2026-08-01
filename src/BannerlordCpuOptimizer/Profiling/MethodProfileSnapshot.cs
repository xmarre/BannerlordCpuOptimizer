using System.Runtime.Serialization;

namespace BannerlordCpuOptimizer.Profiling
{
    [DataContract]
    internal sealed class MethodProfileSnapshot
    {
        [DataMember(Order = 1)] public string Category { get; set; }
        [DataMember(Order = 2)] public string DeclaringAssembly { get; set; }
        [DataMember(Order = 3)] public string DeclaringType { get; set; }
        [DataMember(Order = 4)] public string MethodName { get; set; }
        [DataMember(Order = 5)] public string Signature { get; set; }
        [DataMember(Order = 6)] public int SampleEvery { get; set; }
        [DataMember(Order = 7)] public bool Disabled { get; set; }
        [DataMember(Order = 8)] public long CallCount { get; set; }
        [DataMember(Order = 9)] public long SampledCallCount { get; set; }
        [DataMember(Order = 10)] public double SampledTotalMilliseconds { get; set; }
        [DataMember(Order = 11)] public double EstimatedTotalMilliseconds { get; set; }
        [DataMember(Order = 12)] public double MaximumMilliseconds { get; set; }
        [DataMember(Order = 13)] public double AverageSampledMilliseconds { get; set; }
        [DataMember(Order = 14)] public double CallsPerRenderedFrame { get; set; }
        [DataMember(Order = 15)] public double CallsPerCampaignHour { get; set; }
        [DataMember(Order = 16)] public double CallsPerMission { get; set; }
        [DataMember(Order = 17)] public long SampledAllocatedBytes { get; set; }
        [DataMember(Order = 18)] public long EstimatedAllocatedBytes { get; set; }
        [DataMember(Order = 19)] public long MaximumAllocatedBytes { get; set; }
    }
}
