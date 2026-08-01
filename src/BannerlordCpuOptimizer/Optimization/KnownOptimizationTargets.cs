using System.Collections.Generic;
using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Optimization
{
    internal static class KnownOptimizationTargets
    {
        internal static ProfilerTargetSpec CareerChoiceGetChoice()
        {
            return Spec(
                "TOR_Core.CharacterDevelopment.TORCareerChoices",
                "GetChoice",
                "TOR_Core.CharacterDevelopment.CareerSystem.CareerChoiceObject",
                new[] { "System.String" },
                "Milestone 2 career-choice cache",
                "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d");
        }

        internal static ProfilerTargetSpec MapVisibilitySpottingRange()
        {
            return Spec(
                "TOR_Core.Models.TORMapVisibilityModel",
                "GetPartySpottingRange",
                "TaleWorlds.CampaignSystem.ExplainedNumber",
                new[] { "TaleWorlds.CampaignSystem.Party.MobileParty", "System.Boolean" },
                "Milestone 4 map visibility early exit",
                "da46117a540b2ad0e28dad4e7076fd0311ce43e97f47912cd9c8f343a59b29e2");
        }

        internal static ProfilerTargetSpec FindSettlementsAroundPosition()
        {
            return Spec(
                "TOR_Core.Utilities.TORCommon",
                "FindSettlementsAroundPosition",
                "TaleWorlds.Library.MBList`1[TaleWorlds.CampaignSystem.Settlements.Settlement]",
                new[]
                {
                    "TaleWorlds.Library.Vec2",
                    "System.Single",
                    "System.Func`2[TaleWorlds.CampaignSystem.Settlements.Settlement,System.Boolean]"
                },
                "Milestone 4 map visibility reference",
                "34208fbc8958a6c869968edd8ac7e0018a2691f120cfb0893958d989ff971876");
        }

        internal static ProfilerTargetSpec MapVisibilityPredicate()
        {
            return Spec(
                "TOR_Core.Models.TORMapVisibilityModel+<>c",
                "<GetPartySpottingRange>b__0_0",
                "System.Boolean",
                new[] { "TaleWorlds.CampaignSystem.Settlements.Settlement" },
                "Milestone 4 map visibility predicate",
                "94729e2790e1421c91f7ec9754941025be6417c6c3babcc4a15506dd9cdd4bf6");
        }

        internal static ProfilerTargetSpec WeeklyCompanionTick()
        {
            return Spec(
                "TOR_Core.CampaignMechanics.Companions.TORCompanionsCampaignBehavior",
                "WeeklyTick",
                "System.Void",
                new string[0],
                "Milestone 4 weekly companion LINQ elision",
                "8fba5850abb733b65e720a74c1b5989e479c5ac242a8587907af8ec720c35c90");
        }

        internal static IReadOnlyList<ProfilerTargetSpec> RaceLookupCallers()
        {
            return new[]
            {
                Spec(
                    "TOR_Core.Models.TORCharacterStatsModel",
                    "CalculateHitPoints",
                    "TaleWorlds.CampaignSystem.ExplainedNumber",
                    new[] { "TaleWorlds.CampaignSystem.ExplainedNumber", "TaleWorlds.CampaignSystem.CharacterObject" },
                    "Milestone 4 hit-point fixed-race lookup",
                    "e9665f13d87afe89385473a3ff8e04773e066be7885f2ac720ac81f0647045fa"),
                CharacterRaceSpec("IsMinotaur", "208000ad9273813a38faeb288bed39921418c40daa5dd1a4829d32300f190efe"),
                CharacterRaceSpec("IsTroll", "14688937fbbf28368426ef1e52c819c509e0a2873bfe388da985ac097e948ca9"),
                CharacterRaceSpec("IsDwarf", "860f73d08da228bd999ef147b334c3f7f5a043c1cdf61c54f5c410760b20e091"),
                CharacterRaceSpec("IsGoblin", "85273fd90df12fc28e649d88f605f32bd3434b1facce550e0f50f602b73c9aaf"),
                CharacterRaceSpec("IsOrc", "448666f090a8cc9a860907d3f977c706756952ac5776732b9ae61999c8cd98c1")
            };
        }

        private static ProfilerTargetSpec CharacterRaceSpec(string methodName, string hash)
        {
            return Spec(
                "TOR_Core.Extensions.CharacterObjectExtensions",
                methodName,
                "System.Boolean",
                new[] { "TaleWorlds.CampaignSystem.CharacterObject" },
                "Milestone 4 hit-point fixed-race lookup",
                hash);
        }

        private static ProfilerTargetSpec Spec(
            string typeName,
            string methodName,
            string returnType,
            string[] parameters,
            string category,
            string hash)
        {
            return new ProfilerTargetSpec(
                "TOR_Core",
                typeName,
                methodName,
                returnType,
                parameters,
                category,
                1,
                hash);
        }
    }
}
