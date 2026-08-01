using System;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Profiling;
using BannerlordCpuOptimizer.Runtime;
using HarmonyLib;

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class CareerChoiceCachePatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.optimizations.careerchoicecache";

        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly OptimizerSettings _settings;
        private readonly MapVisibilityOptimizationPatches _mapVisibility;
        private readonly RaceLookupOptimizationPatches _raceLookup;
        private readonly WeeklyCompanionOptimizationPatches _weeklyCompanions;
        private MethodBase _target;
        private Type _patchType;
        private MethodInfo _prefixMethod;
        private MethodInfo _postfixMethod;
        private bool _campaignOptimizationsApplied;

        internal CareerChoiceCachePatches(OptimizerSettings settings)
        {
            _settings = settings;
            _mapVisibility = new MapVisibilityOptimizationPatches(settings);
            _raceLookup = new RaceLookupOptimizationPatches(settings);
            _weeklyCompanions = new WeeklyCompanionOptimizationPatches(settings);

            MapVisibilityEarlyExit.Configure(
                settings.General.MapVisibilityEarlyExit,
                settings.General.MapVisibilityShadowComparisons,
                settings.General.MapVisibilityAuditEvery);
            RaceLookupCache.Configure(
                settings.General.RaceLookupCache,
                settings.General.RaceLookupShadowComparisons,
                settings.General.RaceLookupAuditEvery);
            WeeklyCompanionLinqElision.Reset();
        }

        internal int Apply()
        {
            int applied = ApplyCampaignOptimizations();
            if (_target != null)
            {
                return applied + 1;
            }

            if (!_settings.General.TorCampaignOptimizations || !CareerChoiceCache.IsConfigured)
            {
                OptimizerLog.Info("Career-choice cache patch is disabled by configuration.");
                return applied;
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
                return applied;
            }

            MethodInfo targetMethod = target as MethodInfo;
            if (targetMethod == null || targetMethod.ReturnType.IsValueType)
            {
                OptimizerLog.Once(
                    "career-choice-cache-return",
                    "ERROR",
                    "Career-choice cache patch requires a reference-type result. Original TOR behavior will continue.");
                return applied;
            }

            try
            {
                if (!ValidateForeignPatches(target))
                {
                    return applied;
                }

                MethodInfo beginBridge = typeof(CareerChoicePatchBridge).GetMethod(
                    nameof(CareerChoicePatchBridge.Begin),
                    BindingFlags.Public | BindingFlags.Static);
                MethodInfo completeBridge = typeof(CareerChoicePatchBridge).GetMethod(
                    nameof(CareerChoicePatchBridge.Complete),
                    BindingFlags.Public | BindingFlags.Static);
                ExactResultPatchMethods patchMethods = ExactResultPatchFactory.Create(
                    targetMethod.ReturnType,
                    typeof(CareerChoicePatchState),
                    beginBridge,
                    completeBridge);

                _patchType = patchMethods.PatchType;
                _prefixMethod = patchMethods.Prefix;
                _postfixMethod = patchMethods.Postfix;
                _harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(_prefixMethod) { priority = Priority.Normal },
                    postfix: new HarmonyMethod(_postfixMethod) { priority = Priority.Normal });
                _target = target;
                OptimizerLog.Info("Career-choice cache patch attached after campaign startup: "
                    + ProfilerTargetSpec.FormatSignature(target) + ".");
                return applied + 1;
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce(
                    "career-choice-cache-patch",
                    "Career-choice cache patch failed; original TOR behavior will continue",
                    exception);
                RemoveCareerChoiceOnly();
                return applied;
            }
        }

        internal void Remove()
        {
            RemoveCareerChoiceOnly();
            _weeklyCompanions.Remove();
            _raceLookup.Remove();
            _mapVisibility.Remove();
            _campaignOptimizationsApplied = false;
        }

        private int ApplyCampaignOptimizations()
        {
            if (_campaignOptimizationsApplied)
            {
                return 0;
            }

            _campaignOptimizationsApplied = true;
            int applied = 0;
            applied += _mapVisibility.Apply();
            applied += _raceLookup.Apply();
            applied += _weeklyCompanions.Apply();
            OptimizerLog.Info("Milestone 4 TOR campaign optimization patch count: " + applied + ".");
            return applied;
        }

        private void RemoveCareerChoiceOnly()
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
                _patchType = null;
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
    }
}
