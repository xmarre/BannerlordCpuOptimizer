using BannerlordCpuOptimizer.Compatibility;
using BannerlordCpuOptimizer.Optimization;
using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class LifecycleManager
    {
        internal static void OnMissionStarted()
        {
            TorMetricsAdapter.ClearInstanceCache();
        }

        internal static void OnMissionEnded()
        {
            TorMetricsAdapter.ClearInstanceCache();
        }

        internal static void OnCampaignStarted()
        {
            FrameProfiler.CampaignStarted();
        }

        internal static void OnCampaignEnded()
        {
            FrameProfiler.CampaignEnded();
            TorMetricsAdapter.ClearInstanceCache();
        }

        internal static void ClearAll()
        {
            CareerChoiceCache.ClearAll();
            TorMetricsAdapter.ClearAll();
            FrameProfiler.ResetLifecycleState();
            MethodProfiler.ClearSessionData();
        }
    }
}
