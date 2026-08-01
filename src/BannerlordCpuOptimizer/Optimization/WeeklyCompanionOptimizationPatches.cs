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

namespace BannerlordCpuOptimizer.Optimization
{
    internal sealed class WeeklyCompanionOptimizationPatches
    {
        internal const string HarmonyId = "com.bannerlordcpuoptimizer.optimizations.weeklycompanions";
        private readonly Harmony _harmony = new Harmony(HarmonyId);
        private readonly OptimizerSettings _settings;
        private MethodBase _target;

        internal WeeklyCompanionOptimizationPatches(OptimizerSettings settings)
        {
            _settings = settings;
        }

        internal int Apply()
        {
            if (_target != null)
            {
                return 1;
            }

            if (!_settings.General.TorCampaignOptimizations || !_settings.General.WeeklyCompanionLinqElision)
            {
                OptimizerLog.Info("Weekly companion LINQ elision is disabled by configuration.");
                return 0;
            }

            ProfilerTargetSpec specification = KnownOptimizationTargets.WeeklyCompanionTick();
            MethodBase target = specification.Resolve();
            if (!PatchGate.ValidateTarget(target, specification, false, out string reason))
            {
                OptimizerLog.Once("weekly-companion-gate", "ERROR",
                    "Weekly companion optimization was not applied: " + reason
                    + ". Original TOR behavior will continue.");
                return 0;
            }

            if (!HasOnlyAllowedOwners(target))
            {
                OptimizerLog.Once("weekly-companion-foreign-owners", "WARN",
                    "Weekly companion optimization was not applied because another Harmony owner modifies "
                    + ProfilerTargetSpec.FormatSignature(target) + ".");
                return 0;
            }

            try
            {
                MethodInfo transpiler = typeof(WeeklyCompanionOptimizationPatches).GetMethod(
                    nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, transpiler: new HarmonyMethod(transpiler) { priority = Priority.Normal });
                _target = target;
                OptimizerLog.Info("Weekly companion LINQ-elision patch attached: "
                    + ProfilerTargetSpec.FormatSignature(target) + ".");
                return 1;
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("weekly-companion-patch",
                    "Weekly companion optimization failed; original TOR behavior will continue", exception);
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
                OptimizerLog.WriteExceptionOnce("weekly-companion-unpatch",
                    "Could not remove weekly companion optimization", exception);
            }
            finally
            {
                _target = null;
                WeeklyCompanionLinqElision.Reset();
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> source = instructions.ToList();
            MethodInfo filterDefinition = typeof(WeeklyCompanionLinqElision).GetMethod(
                nameof(WeeklyCompanionLinqElision.FilterToList));
            MethodInfo firstDefinition = typeof(WeeklyCompanionLinqElision).GetMethod(
                nameof(WeeklyCompanionLinqElision.FirstOrDefaultMatch));
            int replacements = 0;

            for (int index = 0; index < source.Count; index++)
            {
                CodeInstruction current = source[index];
                if (index + 1 < source.Count && IsWhereQ(current, out Type elementType))
                {
                    CodeInstruction next = source[index + 1];
                    MethodInfo terminal = next.operand as MethodInfo;
                    MethodInfo replacement = null;
                    if (terminal != null && terminal.DeclaringType == typeof(Enumerable)
                        && terminal.Name == nameof(Enumerable.ToList))
                    {
                        replacement = filterDefinition.MakeGenericMethod(elementType);
                    }
                    else if (terminal != null && terminal.DeclaringType == typeof(Enumerable)
                        && terminal.Name == nameof(Enumerable.FirstOrDefault)
                        && terminal.GetParameters().Length == 1)
                    {
                        replacement = firstDefinition.MakeGenericMethod(elementType);
                    }

                    if (replacement != null)
                    {
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
                }

                yield return current;
            }

            if (replacements != 2)
            {
                throw new InvalidOperationException(
                    "Expected exactly two WeeklyTick WhereQ terminal chains, found " + replacements + ".");
            }
        }

        private static bool IsWhereQ(CodeInstruction instruction, out Type elementType)
        {
            MethodInfo method = instruction.operand as MethodInfo;
            if (method != null && method.Name == "WhereQ" && method.IsGenericMethod)
            {
                Type[] arguments = method.GetGenericArguments();
                if (arguments.Length == 1)
                {
                    elementType = arguments[0];
                    return true;
                }
            }

            elementType = null;
            return false;
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
