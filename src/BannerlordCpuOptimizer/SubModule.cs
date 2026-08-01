using BannerlordCpuOptimizer.Campaign;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Mission;
using BannerlordCpuOptimizer.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordCpuOptimizer
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            OptimizerRuntime.Initialize();
        }

        protected override void OnSubModuleUnloaded()
        {
            OptimizerRuntime.Shutdown();
            base.OnSubModuleUnloaded();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            OptimizerRuntime.OnGameStarted();
            if (OptimizerRuntime.MeasurementEnabled && gameStarter is CampaignGameStarter campaignStarter)
            {
                campaignStarter.AddBehavior(new ProfilerCampaignBehavior());
                LifecycleManager.OnCampaignStarted();
            }
        }

        public override void OnGameEnd(Game game)
        {
            OptimizerRuntime.OnGameEnded();
            base.OnGameEnd(game);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            OptimizerRuntime.OnApplicationTick();
        }

        public override void OnMissionBehaviorInitialize(TaleWorlds.MountAndBlade.Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            if (OptimizerRuntime.ProfilingEnabled)
            {
                mission.AddMissionBehavior(new ProfilerMissionBehavior());
            }
        }
    }
}
