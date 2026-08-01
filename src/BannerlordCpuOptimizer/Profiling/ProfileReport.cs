using System.Collections.Generic;
using System.Runtime.Serialization;
using BannerlordCpuOptimizer.Optimization;

namespace BannerlordCpuOptimizer.Profiling
{
    [DataContract]
    internal sealed class ProfileReport
    {
        [DataMember(Order = 1)] public string SessionId { get; set; }
        [DataMember(Order = 2)] public string StartedUtc { get; set; }
        [DataMember(Order = 3)] public string EndedUtc { get; set; }
        [DataMember(Order = 4)] public string OptimizerVersion { get; set; }
        [DataMember(Order = 5)] public bool AllocationCounterAvailable { get; set; }
        [DataMember(Order = 6)] public long RenderedFrames { get; set; }
        [DataMember(Order = 7)] public long CampaignHours { get; set; }
        [DataMember(Order = 8)] public long Missions { get; set; }
        [DataMember(Order = 9)] public int Gen0CollectionsDelta { get; set; }
        [DataMember(Order = 10)] public int Gen1CollectionsDelta { get; set; }
        [DataMember(Order = 11)] public int Gen2CollectionsDelta { get; set; }
        [DataMember(Order = 12)] public List<AssemblySnapshot> Assemblies { get; set; }
        [DataMember(Order = 13)] public List<MethodProfileSnapshot> Methods { get; set; }
        [DataMember(Order = 14)] public List<ContextSnapshot> Context { get; set; }
        [DataMember(Order = 15)] public CareerChoiceCacheSnapshot CareerChoiceCache { get; set; }
        [DataMember(Order = 16)] public string MapVisibilityOptimization { get; set; }
        [DataMember(Order = 17)] public string RaceLookupOptimization { get; set; }
        [DataMember(Order = 18)] public string WeeklyCompanionOptimization { get; set; }
        [DataMember(Order = 19)] public string Notes { get; set; }
    }

    [DataContract]
    internal sealed class AssemblySnapshot
    {
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public string AssemblyVersion { get; set; }
        [DataMember(Order = 3)] public string FileVersion { get; set; }
        [DataMember(Order = 4)] public string Mvid { get; set; }
        [DataMember(Order = 5)] public string Location { get; set; }
    }
}
