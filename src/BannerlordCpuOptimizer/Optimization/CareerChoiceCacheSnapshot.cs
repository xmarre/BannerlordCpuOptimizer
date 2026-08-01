using System.Runtime.Serialization;

namespace BannerlordCpuOptimizer.Optimization
{
    [DataContract]
    internal sealed class CareerChoiceCacheSnapshot
    {
        [DataMember(Order = 1)] public string ConfiguredMode { get; set; }
        [DataMember(Order = 2)] public string RuntimeState { get; set; }
        [DataMember(Order = 3)] public int SessionGeneration { get; set; }
        [DataMember(Order = 4)] public bool CampaignBound { get; set; }
        [DataMember(Order = 5)] public int CacheEntries { get; set; }
        [DataMember(Order = 6)] public int ValidatedEntries { get; set; }
        [DataMember(Order = 7)] public long Calls { get; set; }
        [DataMember(Order = 8)] public long ActiveHits { get; set; }
        [DataMember(Order = 9)] public long Misses { get; set; }
        [DataMember(Order = 10)] public long Stores { get; set; }
        [DataMember(Order = 11)] public long ShadowComparisons { get; set; }
        [DataMember(Order = 12)] public long PerIdValidations { get; set; }
        [DataMember(Order = 13)] public long Mismatches { get; set; }
        [DataMember(Order = 14)] public long NullResults { get; set; }
        [DataMember(Order = 15)] public long Promotions { get; set; }
        [DataMember(Order = 16)] public long Audits { get; set; }
        [DataMember(Order = 17)] public string DisabledReason { get; set; }
    }
}
