using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BannerlordCpuOptimizer.Compatibility;
using BannerlordCpuOptimizer.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using GameMission = TaleWorlds.MountAndBlade.Mission;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class FrameProfiler
    {
        private static readonly object SnapshotSync = new object();
        private static readonly List<ContextSnapshot> ContextSnapshots = new List<ContextSnapshot>();
        private static long _renderedFrames, _campaignHours, _missions, _nextContextTimestamp;
        private static bool _campaignActive, _missionActive;
        internal static long RenderedFrames => Interlocked.Read(ref _renderedFrames);
        internal static long CampaignHours => Interlocked.Read(ref _campaignHours);
        internal static long Missions => Interlocked.Read(ref _missions);
        private static long ContextIntervalTicks { get; set; } = Stopwatch.Frequency;
        private static bool AllowUnvalidatedOptionalMetrics { get; set; }

        internal static void Configure(double contextSampleSeconds, bool allowUnvalidatedOptionalMetrics)
        {
            long interval = Math.Max(1L, (long)(Stopwatch.Frequency * contextSampleSeconds));
            Interlocked.Exchange(ref _nextContextTimestamp, Stopwatch.GetTimestamp() + interval);
            ContextIntervalTicks = interval;
            AllowUnvalidatedOptionalMetrics = allowUnvalidatedOptionalMetrics;
            TorMetricsAdapter.Configure(allowUnvalidatedOptionalMetrics);
        }

        internal static void OnApplicationTick()
        {
            long frame = Interlocked.Increment(ref _renderedFrames);
            long now = Stopwatch.GetTimestamp();
            long due = Interlocked.Read(ref _nextContextTimestamp);
            if (now < due || Interlocked.CompareExchange(ref _nextContextTimestamp, now + ContextIntervalTicks, due) != due) { return; }
            try { lock (SnapshotSync) { ContextSnapshots.Add(CaptureContext(frame)); } }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("context-snapshot", "Runtime context sampling was disabled for this session", exception);
                Interlocked.Exchange(ref _nextContextTimestamp, long.MaxValue);
            }
        }

        internal static void CampaignHourElapsed() => Interlocked.Increment(ref _campaignHours);
        internal static void CampaignStarted() => _campaignActive = true;
        internal static void CampaignEnded() => _campaignActive = false;
        internal static void MissionStarted() { if (!_missionActive) { _missionActive = true; Interlocked.Increment(ref _missions); } }
        internal static void MissionEnded() => _missionActive = false;
        internal static IReadOnlyList<ContextSnapshot> SnapshotContexts() { lock (SnapshotSync) { return ContextSnapshots.ToArray(); } }
        internal static void ClearSessionData()
        {
            Interlocked.Exchange(ref _renderedFrames, 0L); Interlocked.Exchange(ref _campaignHours, 0L); Interlocked.Exchange(ref _missions, 0L);
            lock (SnapshotSync) { ContextSnapshots.Clear(); }
        }
        internal static void ResetLifecycleState() { _campaignActive = false; _missionActive = false; ClearSessionData(); }

        private static ContextSnapshot CaptureContext(long frame)
        {
            int activeParties = -1, settlements = -1, livingAgents = -1, totalAgents = -1, missiles = -1;
            string campaignSpeed = null, battleType = null;
            Campaign campaign = Campaign.Current;
            if (campaign != null)
            {
                activeParties = MobileParty.All?.Count ?? 0;
                settlements = Settlement.All?.Count ?? 0;
                campaignSpeed = campaign.TimeControlMode + " x" + campaign.SpeedUpMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            GameMission mission = GameMission.Current;
            if (mission != null)
            {
                livingAgents = totalAgents = 0;
                foreach (Agent agent in mission.AllAgents)
                {
                    totalAgents++;
                    if (agent != null && agent.IsActive() && agent.Health > 0f) { livingAgents++; }
                }
                missiles = mission.MissilesList?.Count ?? 0;
                battleType = mission.Mode.ToString();
            }
            return new ContextSnapshot
            {
                UtcTimestamp = DateTime.UtcNow.ToString("O"), RenderedFrame = frame, CampaignSpeed = campaignSpeed,
                MapZoom = ReflectionMetricReader.TryReadMapZoom(AllowUnvalidatedOptionalMetrics), ActivePartyCount = activeParties,
                SettlementCount = settlements, LivingAgentCount = livingAgents, TotalAgentCount = totalAgents,
                ActiveMissileCount = missiles, ActiveSpellOrEffectCount = TorMetricsAdapter.ReadActiveSpellOrEffectCount(mission),
                BattleType = battleType, Gen0Collections = GC.CollectionCount(0), Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2), ManagedBytes = GC.GetTotalMemory(false)
            };
        }
    }
}
