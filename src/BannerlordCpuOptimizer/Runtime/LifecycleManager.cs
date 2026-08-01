using BannerlordCpuOptimizer.Compatibility;
using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Runtime
{
    internal static class LifecycleManager
    {
        internal static void OnMissionStarted()
        {
            FrameProfiler.MissionStarted();
            TorMetricsAdapter.ClearInstanceCache();
        }

        internal static void OnMissionEnded()
        {
            TorMetricsAdapter.ClearInstanceCache();
            FrameProfiler.MissionEnded();
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
            TorMetricsAdapter.ClearAll();
            FrameProfiler.ResetLifecycleState();
            MethodProfiler.ClearSessionData();
        }
    }
}
