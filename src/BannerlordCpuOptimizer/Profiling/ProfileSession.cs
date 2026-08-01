using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordCpuOptimizer.Runtime;

namespace BannerlordCpuOptimizer.Profiling
{
    internal sealed class ProfileSession
    {
        private readonly DateTime _startedUtc;
        private readonly int _gen0Start, _gen1Start, _gen2Start;
        private readonly IReadOnlyList<AssemblyIdentity> _assemblies;

        internal ProfileSession()
        {
            SessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _startedUtc = DateTime.UtcNow;
            _gen0Start = GC.CollectionCount(0); _gen1Start = GC.CollectionCount(1); _gen2Start = GC.CollectionCount(2);
            _assemblies = AssemblyProbe.CaptureLoadedAssemblies();
            FrameProfiler.ClearSessionData(); MethodProfiler.ClearSessionData();
        }

        internal string SessionId { get; }
        internal ProfileReport Complete()
        {
            long frames = FrameProfiler.RenderedFrames, campaignHours = FrameProfiler.CampaignHours, missions = FrameProfiler.Missions;
            return new ProfileReport
            {
                SessionId = SessionId, StartedUtc = _startedUtc.ToString("O"), EndedUtc = DateTime.UtcNow.ToString("O"),
                OptimizerVersion = typeof(ProfileSession).Assembly.GetName().Version?.ToString() ?? "unknown",
                AllocationCounterAvailable = AllocationCounter.IsAvailable, RenderedFrames = frames, CampaignHours = campaignHours, Missions = missions,
                Gen0CollectionsDelta = GC.CollectionCount(0) - _gen0Start, Gen1CollectionsDelta = GC.CollectionCount(1) - _gen1Start, Gen2CollectionsDelta = GC.CollectionCount(2) - _gen2Start,
                Assemblies = _assemblies.Select(identity => new AssemblySnapshot { Name = identity.Name, AssemblyVersion = identity.AssemblyVersion, FileVersion = identity.FileVersion, Mvid = identity.Mvid.ToString("D"), Location = identity.Location }).ToList(),
                Methods = MethodProfiler.Snapshot(frames, campaignHours, missions).ToList(), Context = FrameProfiler.SnapshotContexts().ToList(),
                Notes = "Milestone 1 profiler-only build. Estimated totals scale sampled duration/allocation by call-count ratio; sampled and estimated values are reported separately."
            };
        }
    }
}
