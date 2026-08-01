using System.Runtime.Serialization;

namespace BannerlordCpuOptimizer.Profiling
{
    [DataContract]
    internal sealed class ContextSnapshot
    {
        [DataMember(Order = 1)] public string UtcTimestamp { get; set; }
        [DataMember(Order = 2)] public long RenderedFrame { get; set; }
        [DataMember(Order = 3)] public string CampaignSpeed { get; set; }
        [DataMember(Order = 4)] public double? MapZoom { get; set; }
        [DataMember(Order = 5)] public int ActivePartyCount { get; set; }
        [DataMember(Order = 6)] public int SettlementCount { get; set; }
        [DataMember(Order = 7)] public int LivingAgentCount { get; set; }
        [DataMember(Order = 8)] public int TotalAgentCount { get; set; }
        [DataMember(Order = 9)] public int ActiveMissileCount { get; set; }
        [DataMember(Order = 10)] public int ActiveSpellOrEffectCount { get; set; }
        [DataMember(Order = 11)] public string BattleType { get; set; }
        [DataMember(Order = 12)] public int Gen0Collections { get; set; }
        [DataMember(Order = 13)] public int Gen1Collections { get; set; }
        [DataMember(Order = 14)] public int Gen2Collections { get; set; }
        [DataMember(Order = 15)] public long ManagedBytes { get; set; }
    }
}
