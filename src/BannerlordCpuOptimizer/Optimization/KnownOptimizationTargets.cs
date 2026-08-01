using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Optimization
{
    internal static class KnownOptimizationTargets
    {
        internal static ProfilerTargetSpec CareerChoiceGetChoice()
        {
            return new ProfilerTargetSpec(
                "TOR_Core",
                "TOR_Core.CharacterDevelopment.TORCareerChoices",
                "GetChoice",
                "TOR_Core.CharacterDevelopment.CareerSystem.CareerChoiceObject",
                new[] { "System.String" },
                "Milestone 2 career-choice cache",
                1,
                "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d");
        }
    }
}
