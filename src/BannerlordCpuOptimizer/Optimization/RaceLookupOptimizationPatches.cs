using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Profiling;
using BannerlordCpuOptimizer.Runtime;
using HarmonyLib;
using TaleWorlds.Core;

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class RaceLookupOptimizationPatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.optimizations.racelookup";
        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly OptimizerSettings _settings;
        private readonly List<MethodBase> _targets = new List<MethodBase>();

        internal RaceLookupOptimizationPatches(OptimizerSettings settings)
        {
            _settings = settings;
        }

        internal int Apply()
        {
            if (_targets.Count > 0)
            {
                return _targets.Count;
            }

            if (!_settings.General.TorCampaignOptimizations || !_settings.General.RaceLookupCache)
            {
                OptimizerLog.Info("TOR fixed-race lookup cache is disabled by configuration.");
                return 0;
            }

            try
            {
                MethodInfo transpiler = typeof(RaceLookupOptimizationPatches).GetMethod(
                    nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);
                foreach (ProfilerTargetSpec specification in KnownOptimizationTargets.RaceLookupCallers())
                {
                    MethodBase target = specification.Resolve();
                    if (!PatchGate.ValidateTarget(target, specification, false, out string reason))
                    {
                        throw new InvalidOperationException(specification.TypeName + "." + specification.MethodName
                            + " failed validation: " + reason);
                    }
                    if (!HasOnlyAllowedOwners(target))
                    {
                        throw new InvalidOperationException("foreign Harmony owner on "
                            + ProfilerTargetSpec.FormatSignature(target));
                    }

                    _harmony.Patch(target, transpiler: new HarmonyMethod(transpiler) { priority = Priority.Normal });
                    _targets.Add(target);
                }

                OptimizerLog.Info("TOR fixed-race lookup cache attached to " + _targets.Count
                    + " exactly fingerprinted caller(s).");
                return _targets.Count;
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("race-lookup-patch",
                    "TOR fixed-race lookup cache failed; original TOR behavior will continue", exception);
                Remove();
                return 0;
            }
        }

        internal void Remove()
        {
            try
            {
                _harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("race-lookup-unpatch",
                    "Could not remove TOR fixed-race lookup cache", exception);
            }
            finally
            {
                _targets.Clear();
                RaceLookupCache.Clear();
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            MethodInfo original = AccessTools.Method(typeof(FaceGen), nameof(FaceGen.GetRaceOrDefault), new[] { typeof(string) });
            MethodInfo replacement = AccessTools.Method(typeof(RaceLookupCache), nameof(RaceLookupCache.GetRaceOrDefault));
            int replacements = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    var call = new CodeInstruction(instruction.opcode, replacement);
                    call.labels.AddRange(instruction.labels);
                    call.blocks.AddRange(instruction.blocks);
                    yield return call;
                    replacements++;
                }
                else
                {
                    yield return instruction;
                }
            }

            int expected = __originalMethod.DeclaringType?.FullName == "TOR_Core.Models.TORCharacterStatsModel" ? 2 : 1;
            if (replacements != expected)
            {
                throw new InvalidOperationException("Expected " + expected + " race lookup call(s) in "
                    + ProfilerTargetSpec.FormatSignature(__originalMethod) + ", found " + replacements + ".");
            }
        }

        private static bool HasOnlyAllowedOwners(MethodBase target)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null)
            {
                return true;
            }

            return patches.Owners.All(owner => string.Equals(owner, HarmonyId, StringComparison.Ordinal)
                || string.Equals(owner, HarmonyProfilerPatches.HarmonyId, StringComparison.Ordinal));
        }
    }
}
