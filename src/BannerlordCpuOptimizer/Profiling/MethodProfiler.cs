using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class MethodProfiler
    {
        private static readonly ConcurrentDictionary<MethodBase, MethodProfile> Profiles = new ConcurrentDictionary<MethodBase, MethodProfile>();
        internal static MethodProfile Register(MethodBase method, string category, int sampleEvery) => Profiles.GetOrAdd(method, value => new MethodProfile(value, category, sampleEvery));

        internal static void Enter(MethodBase method, out ProfileCallState state)
        {
            state = default(ProfileCallState);
            try
            {
                if (method != null && Profiles.TryGetValue(method, out MethodProfile profile)) { state = profile.Enter(); }
            }
            catch (Exception exception)
            {
                if (method != null && Profiles.TryGetValue(method, out MethodProfile profile)) { profile.Disable(); }
                OptimizerLog.WriteExceptionOnce("profile-enter-" + SafeToken(method), "Profiler prefix disabled for " + SafeName(method), exception);
            }
        }

        internal static void Exit(ref ProfileCallState state)
        {
            if (!state.Sampled || state.Profile == null) { return; }
            try { state.Profile.Exit(state.StartTimestamp, state.AllocationBefore); }
            catch (Exception exception)
            {
                state.Profile.Disable();
                OptimizerLog.WriteExceptionOnce("profile-exit-" + SafeToken(state.Profile.Method), "Profiler postfix disabled for " + SafeName(state.Profile.Method), exception);
            }
            finally { state = default(ProfileCallState); }
        }

        internal static IReadOnlyList<MethodProfileSnapshot> Snapshot(long renderedFrames, long campaignHours, long missions)
        {
            return Profiles.Values.Select(profile => profile.Snapshot(renderedFrames, campaignHours, missions))
                .OrderByDescending(snapshot => snapshot.EstimatedTotalMilliseconds)
                .ThenBy(snapshot => snapshot.DeclaringAssembly, StringComparer.Ordinal)
                .ThenBy(snapshot => snapshot.Signature, StringComparer.Ordinal).ToArray();
        }

        internal static void ClearSessionData() { foreach (MethodProfile profile in Profiles.Values) { profile.Reset(); } }
        internal static void ClearAll() => Profiles.Clear();
        internal static int RegisteredCount => Profiles.Count;
        private static string SafeName(MethodBase method) => method == null ? "<unknown>" : (method.DeclaringType?.FullName ?? "<global>") + "." + method.Name;
        private static string SafeToken(MethodBase method)
        {
            try { return method == null ? "unknown" : method.Module.ModuleVersionId.ToString("N") + "-" + method.MetadataToken; }
            catch { return "unknown"; }
        }
    }
}
