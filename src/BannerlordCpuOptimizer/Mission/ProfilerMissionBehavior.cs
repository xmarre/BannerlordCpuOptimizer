using BannerlordCpuOptimizer.Runtime;
using TaleWorlds.MountAndBlade;

namespace BannerlordCpuOptimizer.Mission
{
    internal sealed class ProfilerMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            LifecycleManager.OnMissionStarted();
        }

        protected override void OnEndMission()
        {
            LifecycleManager.OnMissionEnded();
            base.OnEndMission();
        }

        public override void OnRemoveBehavior()
        {
            LifecycleManager.OnMissionEnded();
            base.OnRemoveBehavior();
        }
    }
}
