using System;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Profiling;
using BannerlordCpuOptimizer.Runtime;
using HarmonyLib;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class CareerChoiceCachePatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.optimizations.careerchoicecache";

        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly OptimizerSettings _settings;
        private MethodBase _target;
        private MethodInfo _prefixMethod;
        private MethodInfo _postfixMethod;

        internal CareerChoiceCachePatches(OptimizerSettings settings)
        {
            _settings = settings;
        }

        internal int Apply()
        {
            if (_target != null)
            {
                return 1;
            }

            if (!_settings.General.TorCampaignOptimizations || !CareerChoiceCache.IsConfigured)
            {
                OptimizerLog.Info("Career-choice cache patch is disabled by configuration.");
                return 0;
            }

            ProfilerTargetSpec specification = KnownOptimizationTargets.CareerChoiceGetChoice();
            MethodBase target = specification.Resolve();
            if (!PatchGate.ValidateTarget(target, specification, false, out string reason))
            {
                OptimizerLog.Once(
                    "career-choice-cache-gate",
                    "ERROR",
                    "Career-choice cache patch was not applied: " + reason
                        + ". Original TOR behavior will continue.");
                return 0;
            }

            MethodInfo targetMethod = target as MethodInfo;
            if (targetMethod == null || targetMethod.ReturnType.IsValueType)
            {
                OptimizerLog.Once(
                    "career-choice-cache-return",
                    "ERROR",
                    "Career-choice cache patch requires a reference-type result. Original TOR behavior will continue.");
                return 0;
            }

            try
            {
                if (!ValidateForeignPatches(target))
                {
                    return 0;
                }

                Type closedPatchType = typeof(TypedPatch<>).MakeGenericType(targetMethod.ReturnType);
                _prefixMethod = closedPatchType.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                _postfixMethod = closedPatchType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                if (_prefixMethod == null || _postfixMethod == null)
                {
                    throw new MissingMethodException("Could not construct the typed career-choice cache patch methods.");
                }

                _harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(_prefixMethod) { priority = Priority.Normal },
                    postfix: new HarmonyMethod(_postfixMethod) { priority = Priority.Normal });
                _target = target;
                OptimizerLog.Info("Career-choice cache patch attached after campaign startup: "
                    + ProfilerTargetSpec.FormatSignature(target) + ".");
                return 1;
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce(
                    "career-choice-cache-patch",
                    "Career-choice cache patch failed; original TOR behavior will continue",
                    exception);
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
                OptimizerLog.WriteExceptionOnce(
                    "career-choice-cache-unpatch",
                    "Could not remove the career-choice cache patch",
                    exception);
            }
            finally
            {
                _target = null;
                _prefixMethod = null;
                _postfixMethod = null;
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

            OptimizerLog.Once(
                "career-choice-cache-foreign-owners",
                "WARN",
                "Career-choice cache patch was not applied because another Harmony owner modifies "
                    + ProfilerTargetSpec.FormatSignature(target) + ": " + string.Join(", ", owners)
                    + ". Original and third-party behavior will continue unchanged.");
            return false;
        }

        private static class TypedPatch<TResult> where TResult : class
        {
            private static bool Prefix(
                string __0,
                ref TResult __result,
                out CareerChoiceCallState __state)
            {
                bool served = CareerChoiceCache.TryServeOrBegin(__0, GameCampaign.Current, out object cached, out __state);
                if (served)
                {
                    __result = (TResult)cached;
                }

                return !served;
            }

            private static void Postfix(
                string __0,
                TResult __result,
                CareerChoiceCallState __state)
            {
                CareerChoiceCache.CompleteCall(__0, __result, __state);
            }
        }
    }
}
