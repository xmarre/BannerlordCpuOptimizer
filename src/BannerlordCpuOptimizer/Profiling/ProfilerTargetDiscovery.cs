using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Configuration;
using BannerlordCpuOptimizer.Diagnostics;
using BannerlordCpuOptimizer.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class ProfilerTargetDiscovery
    {
        private static readonly HashSet<string> CampaignCallbackNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "HourlyTick", "DailyTick", "HourlyPartyTick", "DailyTickParty", "OnMapScreenUpdate", "UpdateVisibility", "UpdateVisibilityAndInspected"
        };
        private static readonly HashSet<string> MissionCallbackNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "OnMissionTick", "OnMissionScreenTick", "OnPreMissionTick", "OnPreDisplayMissionTick", "OnFixedMissionTick", "OnAgentCreated", "OnAgentBuild", "OnAgentRemoved", "OnAgentDeleted", "OnAgentHit", "OnAgentTeamChanged", "OnAgentControllerChanged", "TickCastingState"
        };

        internal static IReadOnlyList<ProfilerTargetSpec> Discover(OptimizerSettings settings)
        {
            var result = new List<ProfilerTargetSpec>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (ProfilerTargetSpec known in KnownProfilerTargets.Create(settings.Profiling.HighFrequencySampleEvery))
            {
                if (ShouldIncludeKnownTarget(known, settings)) { Add(result, identities, known); }
            }

            Assembly torAssembly = AssemblyProbe.FindLoaded("TOR_Core");
            if (torAssembly != null)
            {
                foreach (Type type in GetLoadableTypes(torAssembly))
                {
                    if (settings.Profiling.ProfileTorCampaignHandlers && typeof(CampaignBehaviorBase).IsAssignableFrom(type))
                    {
                        AddMatchingMethods(result, identities, type, CampaignCallbackNames, "TOR campaign behavior", settings.Profiling.NormalSampleEvery);
                        foreach (MethodInfo handler in EventHandlerIlDiscovery.FindHandlers(type))
                        {
                            if (IsSafeProfilerCandidate(handler)) { Add(result, identities, ProfilerTargetSpec.FromMethod(handler, "TOR registered campaign event", GetSampleRate(handler.Name, settings.Profiling.NormalSampleEvery))); }
                        }
                    }
                    if (settings.Profiling.ProfileTorMissionHandlers && typeof(MissionBehavior).IsAssignableFrom(type))
                    {
                        AddMatchingMethods(result, identities, type, MissionCallbackNames, "TOR mission behavior", settings.Profiling.HighFrequencySampleEvery);
                    }
                    if (settings.Profiling.ProfileTorCampaignHandlers || settings.Profiling.ProfileTorMissionHandlers)
                    {
                        AddMatchingMethods(result, identities, type, CampaignCallbackNames, "TOR update/UI", settings.Profiling.HighFrequencySampleEvery);
                        AddMatchingMethods(result, identities, type, MissionCallbackNames, "TOR update/casting", settings.Profiling.HighFrequencySampleEvery);
                        AddMethodsByName(result, identities, type, "RefreshValues", "TOR UI refresh", settings.Profiling.HighFrequencySampleEvery);
                    }
                    if (settings.Profiling.ProfileTorModels && type.Namespace != null && type.Namespace.StartsWith("TOR_Core.Models", StringComparison.Ordinal))
                    {
                        AddModelMethods(result, identities, type, settings.Profiling.HighFrequencySampleEvery);
                    }
                }
            }
            if (settings.Profiling.ProfileVanillaHandlers) { DiscoverVanillaBehaviorOverrides(result, identities, settings); }
            return result;
        }

        private static bool ShouldIncludeKnownTarget(ProfilerTargetSpec target, OptimizerSettings settings)
        {
            if (target.Category.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0) { return settings.Profiling.ProfileTorModels; }
            if (target.Category.IndexOf("mission", StringComparison.OrdinalIgnoreCase) >= 0 || target.Category.IndexOf("casting", StringComparison.OrdinalIgnoreCase) >= 0) { return settings.Profiling.ProfileTorMissionHandlers; }
            return settings.Profiling.ProfileTorCampaignHandlers;
        }

        private static void DiscoverVanillaBehaviorOverrides(List<ProfilerTargetSpec> result, HashSet<string> identities, OptimizerSettings settings)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith("TaleWorlds.", StringComparison.Ordinal)) { continue; }
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (typeof(CampaignBehaviorBase).IsAssignableFrom(type))
                    {
                        AddMatchingMethods(result, identities, type, CampaignCallbackNames, "vanilla campaign behavior", settings.Profiling.NormalSampleEvery);
                        foreach (MethodInfo handler in EventHandlerIlDiscovery.FindHandlers(type))
                        {
                            if (IsSafeProfilerCandidate(handler)) { Add(result, identities, ProfilerTargetSpec.FromMethod(handler, "vanilla registered campaign event", GetSampleRate(handler.Name, settings.Profiling.NormalSampleEvery))); }
                        }
                    }
                    else if (typeof(MissionBehavior).IsAssignableFrom(type))
                    {
                        AddMatchingMethods(result, identities, type, MissionCallbackNames, "vanilla mission behavior", settings.Profiling.HighFrequencySampleEvery);
                    }
                }
            }
        }

        private static void AddMatchingMethods(List<ProfilerTargetSpec> result, HashSet<string> identities, Type type, HashSet<string> names, string category, int sampleEvery)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (names.Contains(method.Name) && IsSafeProfilerCandidate(method)) { Add(result, identities, ProfilerTargetSpec.FromMethod(method, category, GetSampleRate(method.Name, sampleEvery))); }
            }
        }

        private static void AddMethodsByName(List<ProfilerTargetSpec> result, HashSet<string> identities, Type type, string name, string category, int sampleEvery)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (string.Equals(method.Name, name, StringComparison.Ordinal) && IsSafeProfilerCandidate(method)) { Add(result, identities, ProfilerTargetSpec.FromMethod(method, category, sampleEvery)); }
            }
        }

        private static void AddModelMethods(List<ProfilerTargetSpec> result, HashSet<string> identities, Type type, int sampleEvery)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal)) { continue; }
                if (IsSafeProfilerCandidate(method)) { Add(result, identities, ProfilerTargetSpec.FromMethod(method, "TOR model", sampleEvery)); }
            }
        }

        private static bool IsSafeProfilerCandidate(MethodInfo method) => !method.IsAbstract && !method.ContainsGenericParameters && method.GetMethodBody() != null && method.GetParameters().Length <= 16;
        private static int GetSampleRate(string methodName, int defaultRate) => methodName == "HourlyTick" || methodName == "DailyTick" || methodName == "HourlyPartyTick" || methodName == "DailyTickParty" ? 1 : defaultRate;
        private static void Add(List<ProfilerTargetSpec> result, HashSet<string> identities, ProfilerTargetSpec specification)
        {
            string identity = specification.AssemblyName + "|" + specification.TypeName + "|" + specification.MethodName + "|" + string.Join(",", specification.ParameterTypeNames) + "|" + specification.ReturnTypeName;
            if (identities.Add(identity)) { result.Add(specification); }
        }
        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException exception)
            {
                OptimizerLog.Once("partial-types-" + assembly.GetName().Name, "WARN", "Some types could not be loaded from " + assembly.GetName().Name + "; resolvable types will still be profiled.");
                return exception.Types.Where(type => type != null).ToArray();
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("types-" + assembly.GetName().Name, "Could not enumerate types from " + assembly.GetName().Name, exception);
                return new Type[0];
            }
        }
    }
}
