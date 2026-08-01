using System;
using System.Collections.Generic;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class KnownProfilerTargets
    {
        internal static IReadOnlyList<ProfilerTargetSpec> Create(int highFrequencySampleEvery)
        {
            return new[]
            {
                Spec("TOR_Core.CampaignMechanics.WaaaghMeter.WaaaghMeterMapView", "OnMapScreenUpdate", "System.Void", new[] { "System.Single" }, "TOR campaign UI", highFrequencySampleEvery, "4279fe59b9f5823da4d4e26a68c03586da267cc8c09ac3507a5246cb63371762"),
                Spec("TOR_Core.CampaignMechanics.WaaaghMeter.WaaaghMeterVM", "RefreshValues", "System.Void", Empty, "TOR campaign UI", highFrequencySampleEvery, "24e0e442851bc4d5bb7fac9b1899e9b851d86affca106e1c08204d8a162d4780"),
                Spec("TOR_Core.AbilitySystem.AbilityHUDMissionView", "OnMissionTick", "System.Void", new[] { "System.Single" }, "TOR mission UI", highFrequencySampleEvery, "ed4034fc41759d4dd2f7f1b2f277f34f5b5df8aa556232629f4821c03c9d53bb"),
                Spec("TOR_Core.BattleMechanics.StatusEffect.StatusEffectMissionLogic", "OnMissionTick", "System.Void", new[] { "System.Single" }, "TOR mission logic", highFrequencySampleEvery, "ac4e5117b8856ababc83586a67308861762a5d3e6538cf9d50c743b40c23ae7a"),
                Spec("TOR_Core.BattleMechanics.TORBattleAgentLogic", "OnMissionTick", "System.Void", new[] { "System.Single" }, "TOR mission logic", highFrequencySampleEvery, "2b81e1025a23dcb0ebef12b144a6bcb073eccb990d31ff383de0a4ff8774431f"),
                Spec("TOR_Core.AbilitySystem.Ability", "TickCastingState", "System.Void", Empty, "TOR casting", highFrequencySampleEvery, "9ec58a57f7802fdf54662fe399d0e105209e35486d908346df9acdbbf3a86378"),
                Spec("TOR_Core.AbilitySystem.AbilityManagerMissionLogic", "OnPreMissionTick", "System.Void", new[] { "System.Single" }, "TOR casting", highFrequencySampleEvery, "c5a8bcbc28bd23c17461e7ab41b2504c6f1917fe4d701a44070cacf0af11b4d1"),
                Spec("TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehaviorConfiguration", "FindTargets", "System.Collections.Generic.List`1[TOR_Core.BattleMechanics.AI.CommonAIFunctions+Target]", new[] { "TaleWorlds.MountAndBlade.Agent", "TOR_Core.AbilitySystem.AbilityTemplate" }, "TOR casting AI", highFrequencySampleEvery, "77545f770ea7ce91431d903b3ced71c952296bbb1ad54a95acd479b126e825e8"),
                Spec("TOR_Core.Models.TORMapVisibilityModel", "GetPartySpottingRange", "TaleWorlds.CampaignSystem.ExplainedNumber", new[] { "TaleWorlds.CampaignSystem.Party.MobileParty", "System.Boolean" }, "TOR campaign model", highFrequencySampleEvery, "da46117a540b2ad0e28dad4e7076fd0311ce43e97f47912cd9c8f343a59b29e2"),
                Spec("TOR_Core.Models.TORCustomResourceModel", "GetCultureSpecificCustomResourceChange", "TaleWorlds.CampaignSystem.ExplainedNumber", new[] { "TaleWorlds.CampaignSystem.Hero", "System.String" }, "TOR campaign model", highFrequencySampleEvery, "c1f7a09d03617f6144226060b21035b20ae7b8552c8cd55f17bc6bc74069839e"),
                Spec("TOR_Core.Extensions.ExtendedInfoSystem.ExtendedInfoManager", "HourlyTick", "System.Void", Empty, "TOR campaign behavior", 1, "cf2b82a2cbc3213f045658402d0263bfeb44810eaf8e4746c0228125bddc8c34")
            };
        }

        internal static IReadOnlyList<ProfilerTargetSpec> CreateFocused(int sampleEvery)
        {
            int lowFrequency = Math.Max(1, sampleEvery);
            int mediumFrequency = Math.Max(4, sampleEvery);
            int highFrequency = Math.Max(16, sampleEvery);
            int veryHighFrequency = Math.Max(64, sampleEvery);
            return new[]
            {
                Spec("TOR_Core.CharacterDevelopment.TORCareerChoices", "GetChoice", "TOR_Core.CharacterDevelopment.CareerSystem.CareerChoiceObject", new[] { "System.String" }, "Milestone 3 cache control", veryHighFrequency, "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d"),
                Spec("TOR_Core.Models.TORCharacterStatsModel", "MaxHitpoints", "TaleWorlds.CampaignSystem.ExplainedNumber", new[] { "TaleWorlds.CampaignSystem.CharacterObject", "System.Boolean" }, "Milestone 3 hit-points", highFrequency, null),
                Spec("TOR_Core.Models.TORMapVisibilityModel", "GetPartySpottingRange", "TaleWorlds.CampaignSystem.ExplainedNumber", new[] { "TaleWorlds.CampaignSystem.Party.MobileParty", "System.Boolean" }, "Milestone 3 map visibility", mediumFrequency, "da46117a540b2ad0e28dad4e7076fd0311ce43e97f47912cd9c8f343a59b29e2"),
                Spec("TOR_Core.Models.TORMapVisibilityModel+<>c", "<GetPartySpottingRange>b__0_0", "System.Boolean", new[] { "TaleWorlds.CampaignSystem.Settlements.Settlement" }, "Milestone 3 map visibility predicate", highFrequency, null),
                Spec("TOR_Core.Utilities.TORCommon", "FindSettlementsAroundPosition", "TaleWorlds.Library.MBList`1[TaleWorlds.CampaignSystem.Settlements.Settlement]", new[] { "TaleWorlds.Library.Vec2", "System.Single", "System.Func`2[TaleWorlds.CampaignSystem.Settlements.Settlement,System.Boolean]" }, "Milestone 3 map visibility child", mediumFrequency, "34208fbc8958a6c869968edd8ac7e0018a2691f120cfb0893958d989ff971876"),
                Spec("TOR_Core.CampaignMechanics.Companions.TORCompanionsCampaignBehavior", "WeeklyTick", "System.Void", Empty, "Milestone 3 weekly companion hitch", lowFrequency, null)
            };
        }

        private static readonly string[] Empty = new string[0];

        private static ProfilerTargetSpec Spec(string typeName, string methodName, string returnTypeName, string[] parameterTypeNames, string category, int sampleEvery, string hash)
        {
            return new ProfilerTargetSpec("TOR_Core", typeName, methodName, returnTypeName, parameterTypeNames, category, sampleEvery, hash);
        }
    }
}
