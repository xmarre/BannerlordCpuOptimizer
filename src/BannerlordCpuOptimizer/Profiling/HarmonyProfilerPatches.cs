using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Runtime;
using HarmonyLib;

namespace BannerlordCpuOptimizer.Profiling
{
    internal sealed class HarmonyProfilerPatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.profiler";
        private const string DeferredTorResourceModelType = "TOR_Core.Models.TORCustomResourceModel";

        private readonly Harmony _harmony;
        private readonly OptimizerSettings _settings;
        private readonly HarmonyMethod _prefix;
        private readonly HarmonyMethod _postfix;
        private readonly List<MethodBase> _patchedMethods = new List<MethodBase>();
        private readonly List<ProfilerTargetSpec> _deferredTargets = new List<ProfilerTargetSpec>();

        internal HarmonyProfilerPatches(OptimizerSettings settings)
        {
            _settings = settings;
            _harmony = new Harmony(HarmonyId);

            MethodInfo prefixMethod = typeof(HarmonyProfilerPatches).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfixMethod = typeof(HarmonyProfilerPatches).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            _prefix = new HarmonyMethod(prefixMethod) { priority = Priority.First };
            _postfix = new HarmonyMethod(postfixMethod) { priority = Priority.Last };
        }

        internal int Apply()
        {
            var specifications = new List<ProfilerTargetSpec>();
            if (_settings.Profiling.ProfileFocusedTargets)
            {
                specifications.AddRange(KnownProfilerTargets.CreateFocused(_settings.Profiling.FocusedSampleEvery));
            }
            specifications.AddRange(ProfilerTargetDiscovery.Discover(_settings));

            foreach (ProfilerTargetSpec specification in specifications)
            {
                if (MustDeferUntilCampaignReady(specification))
                {
                    _deferredTargets.Add(specification);
                    continue;
                }

                TryPatch(specification);
            }

            OptimizerLog.Info("Profiler patch count: " + _patchedMethods.Count + ".");
            if (_deferredTargets.Count > 0)
            {
                OptimizerLog.Info("Deferred " + _deferredTargets.Count
                    + " TORCustomResourceModel profiler target(s) until the campaign has completed startup.");
            }

            return _patchedMethods.Count;
        }

        internal int ApplyDeferredTargets()
        {
            if (_deferredTargets.Count == 0)
            {
                return 0;
            }

            ProfilerTargetSpec[] pending = _deferredTargets.ToArray();
            _deferredTargets.Clear();
            int before = _patchedMethods.Count;

            foreach (ProfilerTargetSpec specification in pending)
            {
                TryPatch(specification);
            }

            int applied = _patchedMethods.Count - before;
            OptimizerLog.Info("Deferred TORCustomResourceModel profiler targets processed after campaign startup: "
                + applied + " attached, " + (pending.Length - applied) + " skipped or failed.");
            return applied;
        }

        internal void Remove()
        {
            try
            {
                _harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("unpatch-all", "Could not remove profiler patches", exception);
            }
            finally
            {
                _deferredTargets.Clear();
                _patchedMethods.Clear();
                MethodProfiler.ClearAll();
            }
        }

        private void TryPatch(ProfilerTargetSpec specification)
        {
            MethodBase target = null;
            try
            {
                target = specification.Resolve();
                if (!PatchGate.ValidateProfilerTarget(
                    target,
                    specification,
                    _settings.Profiling.AllowUnknownProfilerTargets,
                    out string reason))
                {
                    OptimizerLog.Verbose("Skipped profiler target " + specification.TypeName + "."
                        + specification.MethodName + ": " + reason);
                    return;
                }

                LogForeignPatches(target);
                MethodProfiler.Register(target, specification.Category, specification.SampleEvery);
                _harmony.Patch(target, _prefix, _postfix);
                _patchedMethods.Add(target);
                OptimizerLog.Info("Profiler attached: " + ProfilerTargetSpec.FormatSignature(target)
                    + " [sample 1/" + specification.SampleEvery + "]");
            }
            catch (Exception exception)
            {
                string key = target == null
                    ? "patch-spec-" + specification.AssemblyName + "-" + specification.TypeName + "-" + specification.MethodName
                    : "patch-" + target.Module.ModuleVersionId.ToString("N") + "-" + target.MetadataToken;
                string signature = target == null
                    ? specification.TypeName + "." + specification.MethodName
                    : ProfilerTargetSpec.FormatSignature(target);
                OptimizerLog.WriteExceptionOnce(key, "Profiler patch failed for " + signature, exception);
            }
        }

        private static bool MustDeferUntilCampaignReady(ProfilerTargetSpec specification)
        {
            if (!string.Equals(specification.AssemblyName, "TOR_Core", StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(specification.TypeName, DeferredTorResourceModelType, StringComparison.Ordinal)
                || specification.TypeName.StartsWith(DeferredTorResourceModelType + "+", StringComparison.Ordinal);
        }

        private void LogForeignPatches(MethodBase target)
        {
            if (!_settings.Diagnostics.LogHarmonyConflicts)
            {
                return;
            }

            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null)
            {
                return;
            }

            string[] owners = patches.Owners
                .Where(owner => !string.Equals(owner, HarmonyId, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(owner => owner, StringComparer.Ordinal)
                .ToArray();
            if (owners.Length == 0)
            {
                return;
            }

            OptimizerLog.Once(
                "foreign-patches-" + target.Module.ModuleVersionId.ToString("N") + "-" + target.MetadataToken,
                "WARN",
                "Existing Harmony owners on " + ProfilerTargetSpec.FormatSignature(target) + ": "
                    + string.Join(", ", owners)
                    + ". The profiler adds observation-only prefix/postfix patches and does not alter IL.");
        }

        private static void Prefix(MethodBase __originalMethod, out ProfileCallState __state)
        {
            MethodProfiler.Enter(__originalMethod, out __state);
        }

        private static void Postfix(ref ProfileCallState __state)
        {
            MethodProfiler.Exit(ref __state);
        }
    }
}
