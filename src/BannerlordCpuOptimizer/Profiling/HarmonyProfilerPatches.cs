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
        private readonly Harmony _harmony;
        private readonly OptimizerSettings _settings;
        private readonly List<MethodBase> _patchedMethods = new List<MethodBase>();

        internal HarmonyProfilerPatches(OptimizerSettings settings)
        {
            _settings = settings;
            _harmony = new Harmony(HarmonyId);
        }

        internal int Apply()
        {
            MethodInfo prefixMethod = typeof(HarmonyProfilerPatches).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfixMethod = typeof(HarmonyProfilerPatches).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            var prefix = new HarmonyMethod(prefixMethod) { priority = Priority.First };
            var postfix = new HarmonyMethod(postfixMethod) { priority = Priority.Last };
            foreach (ProfilerTargetSpec specification in ProfilerTargetDiscovery.Discover(_settings))
            {
                MethodBase target = specification.Resolve();
                if (!PatchGate.ValidateProfilerTarget(target, specification, _settings.Profiling.AllowUnknownProfilerTargets, out string reason))
                {
                    OptimizerLog.Verbose("Skipped profiler target " + specification.TypeName + "." + specification.MethodName + ": " + reason);
                    continue;
                }
                try
                {
                    LogForeignPatches(target);
                    MethodProfiler.Register(target, specification.Category, specification.SampleEvery);
                    _harmony.Patch(target, prefix, postfix);
                    _patchedMethods.Add(target);
                    OptimizerLog.Info("Profiler attached: " + ProfilerTargetSpec.FormatSignature(target) + " [sample 1/" + specification.SampleEvery + "]");
                }
                catch (Exception exception)
                {
                    OptimizerLog.WriteExceptionOnce("patch-" + target.Module.ModuleVersionId.ToString("N") + "-" + target.MetadataToken, "Profiler patch failed for " + ProfilerTargetSpec.FormatSignature(target), exception);
                }
            }
            OptimizerLog.Info("Profiler patch count: " + _patchedMethods.Count + ".");
            return _patchedMethods.Count;
        }

        internal void Remove()
        {
            try { _harmony.UnpatchAll(HarmonyId); }
            catch (Exception exception) { OptimizerLog.WriteExceptionOnce("unpatch-all", "Could not remove profiler patches", exception); }
            finally { _patchedMethods.Clear(); MethodProfiler.ClearAll(); }
        }

        private void LogForeignPatches(MethodBase target)
        {
            if (!_settings.Diagnostics.LogHarmonyConflicts) { return; }
            Patches patches = Harmony.GetPatchInfo(target);
            if (patches == null) { return; }
            string[] owners = patches.Owners.Where(owner => !string.Equals(owner, HarmonyId, StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).OrderBy(owner => owner, StringComparer.Ordinal).ToArray();
            if (owners.Length == 0) { return; }
            OptimizerLog.Once("foreign-patches-" + target.Module.ModuleVersionId.ToString("N") + "-" + target.MetadataToken, "WARN", "Existing Harmony owners on " + ProfilerTargetSpec.FormatSignature(target) + ": " + string.Join(", ", owners) + ". The profiler adds observation-only prefix/postfix patches and does not alter IL.");
        }

        private static void Prefix(MethodBase __originalMethod, out ProfileCallState __state) => MethodProfiler.Enter(__originalMethod, out __state);
        private static void Postfix(ref ProfileCallState __state) => MethodProfiler.Exit(ref __state);
    }
}
