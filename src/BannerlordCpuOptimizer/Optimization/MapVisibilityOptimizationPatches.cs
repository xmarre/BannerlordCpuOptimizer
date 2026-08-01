using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Profiling;
using BannerlordCpuOptimizer.Runtime;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class MapVisibilityOptimizationPatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.optimizations.mapvisibility";
        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly OptimizerSettings _settings;
        private MethodBase _target;

        internal MapVisibilityOptimizationPatches(OptimizerSettings settings)
        {
            _settings = settings;
        }

        internal int Apply()
        {
            if (_target != null)
            {
                return 1;
            }

            if (!_settings.General.TorCampaignOptimizations || !_settings.General.MapVisibilityEarlyExit)
            {
                OptimizerLog.Info("Map-visibility early exit is disabled by configuration.");
                return 0;
            }

            ProfilerTargetSpec callerSpec = KnownOptimizationTargets.MapVisibilitySpottingRange();
            ProfilerTargetSpec findSpec = KnownOptimizationTargets.FindSettlementsAroundPosition();
            ProfilerTargetSpec predicateSpec = KnownOptimizationTargets.MapVisibilityPredicate();
            MethodBase caller = callerSpec.Resolve();
            MethodInfo find = findSpec.Resolve() as MethodInfo;
            MethodBase predicate = predicateSpec.Resolve();

            if (!PatchGate.ValidateTarget(caller, callerSpec, false, out string callerReason)
                || !PatchGate.ValidateTarget(find, findSpec, false, out string findReason)
                || !PatchGate.ValidateTarget(predicate, predicateSpec, false, out string predicateReason))
            {
                OptimizerLog.Once("map-visibility-gate", "ERROR",
                    "Map-visibility optimization was not applied. Caller=" + callerReason
                    + "; finder=" + findReason + "; predicate=" + predicateReason
                    + ". Original TOR behavior will continue.");
                return 0;
            }

            if (!ValidateForeignPatches(caller))
            {
                return 0;
            }

            try
            {
                var original = (Func<Vec2, float, Func<Settlement, bool>, MBList<Settlement>>)
                    Delegate.CreateDelegate(typeof(Func<Vec2, float, Func<Settlement, bool>, MBList<Settlement>>), find);
                MapVisibilityEarlyExit.BindOriginal(original);

                MethodInfo transpiler = typeof(MapVisibilityOptimizationPatches).GetMethod(
                    nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(caller, transpiler: new HarmonyMethod(transpiler) { priority = Priority.Normal });
                _target = caller;
                OptimizerLog.Info("Map-visibility early-exit patch attached: "
                    + ProfilerTargetSpec.FormatSignature(caller) + ".");
                return 1;
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("map-visibility-patch",
                    "Map-visibility optimization failed; original TOR behavior will continue", exception);
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
                OptimizerLog.WriteExceptionOnce("map-visibility-unpatch",
                    "Could not remove map-visibility optimization", exception);
            }
            finally
            {
                _target = null;
                MapVisibilityEarlyExit.Clear();
            }
        }

        private bool ValidateForeignPatches(MethodBase target)
        {
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null)
            {
                return true;
            }

            string[] owners = patches.Owners
                .Where(owner => !string.Equals(owner, HarmonyId, StringComparison.Ordinal)
                    && !string.Equals(owner, HarmonyProfilerPatches.HarmonyId, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(owner => owner, StringComparer.Ordinal)
                .ToArray();
            if (owners.Length == 0)
            {
                return true;
            }

            OptimizerLog.Once("map-visibility-foreign-owners", "WARN",
                "Map-visibility optimization was not applied because another Harmony owner modifies "
                + ProfilerTargetSpec.FormatSignature(target) + ": " + string.Join(", ", owners)
                + ". Original and third-party behavior will continue unchanged.");
            return false;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> source = instructions.ToList();
            MethodInfo replacement = AccessTools.Method(typeof(MapVisibilityEarlyExit),
                nameof(MapVisibilityEarlyExit.AnySettlementAroundPosition));
            int replacements = 0;

            for (int index = 0; index < source.Count; index++)
            {
                CodeInstruction current = source[index];
                if (index + 1 < source.Count
                    && IsFindSettlementsCall(current)
                    && IsSettlementAnyCall(source[index + 1]))
                {
                    CodeInstruction next = source[index + 1];
                    var call = new CodeInstruction(OpCodes.Call, replacement);
                    call.labels.AddRange(current.labels);
                    call.labels.AddRange(next.labels);
                    call.blocks.AddRange(current.blocks);
                    call.blocks.AddRange(next.blocks);
                    yield return call;
                    index++;
                    replacements++;
                    continue;
                }

                yield return current;
            }

            if (replacements != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one FindSettlementsAroundPosition/Any pair, found " + replacements + ".");
            }
        }

        private static bool IsFindSettlementsCall(CodeInstruction instruction)
        {
            MethodInfo method = instruction.operand as MethodInfo;
            return method != null
                && method.DeclaringType?.FullName == "TOR_Core.Utilities.TORCommon"
                && method.Name == "FindSettlementsAroundPosition";
        }

        private static bool IsSettlementAnyCall(CodeInstruction instruction)
        {
            MethodInfo method = instruction.operand as MethodInfo;
            return method != null
                && method.DeclaringType == typeof(Enumerable)
                && method.Name == nameof(Enumerable.Any)
                && method.IsGenericMethod
                && method.GetParameters().Length == 1
                && method.GetGenericArguments()[0] == typeof(Settlement);
        }
    }
}
