using BannerlordCpuOptimizer.Profiling;
using BannerlordCpuOptimizer.Runtime;
using TaleWorlds.CampaignSystem;

namespace BannerlordCpuOptimizer.Campaign
{
    internal sealed class ProfilerCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Profiler state is intentionally never serialized.
        }

        private static void OnHourlyTick()
        {
            FrameProfiler.CampaignHourElapsed();
        }
    }
}
