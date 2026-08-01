using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BannerlordCpuOptimizer.Compatibility;
using BannerlordCpuOptimizer.Diagnostics;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;
using GameMission = TaleWorlds.MountAndBlade.Mission;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class FrameProfiler
    {
        private static readonly object SnapshotSync = new object();
        private static readonly List<ContextSnapshot> ContextSnapshots = new List<ContextSnapshot>();
        private static long _renderedFrames;
        private static long _campaignHours;
        private static long _missions;
        private static long _nextContextTimestamp;
        private static GameMission _trackedMission;

        internal static long RenderedFrames => Interlocked.Read(ref _renderedFrames);
        internal static long CampaignHours => Interlocked.Read(ref _campaignHours);
        internal static long Missions => Interlocked.Read(ref _missions);
        private static long ContextIntervalTicks { get; set; } = Stopwatch.Frequency;
        private static bool EnableOptionalContextMetrics { get; set; }

        internal static void Configure(double contextSampleSeconds, bool enableOptionalContextMetrics)
        {
            long interval = Math.Max(1L, (long)(Stopwatch.Frequency * contextSampleSeconds));
            Interlocked.Exchange(ref _nextContextTimestamp, Stopwatch.GetTimestamp() + interval);
            ContextIntervalTicks = interval;
            EnableOptionalContextMetrics = enableOptionalContextMetrics;
            TorMetricsAdapter.Configure(enableOptionalContextMetrics);
        }

        internal static void OnApplicationTick()
        {
            TrackMissionIdentity(GameMission.Current);

            long frame = Interlocked.Increment(ref _renderedFrames);
            long now = Stopwatch.GetTimestamp();
            long due = Interlocked.Read(ref _nextContextTimestamp);
            if (now < due || Interlocked.CompareExchange(ref _nextContextTimestamp, now + ContextIntervalTicks, due) != due)
            {
                return;
            }

            try
            {
                lock (SnapshotSync)
                {
                    ContextSnapshots.Add(CaptureContext(frame));
                }
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("context-snapshot", "Runtime context sampling was disabled for this session", exception);
                Interlocked.Exchange(ref _nextContextTimestamp, long.MaxValue);
            }
        }

        internal static void CampaignHourElapsed() => Interlocked.Increment(ref _campaignHours);
        internal static void CampaignStarted() { }
        internal static void CampaignEnded() { }

        internal static IReadOnlyList<ContextSnapshot> SnapshotContexts()
        {
            lock (SnapshotSync)
            {
                return ContextSnapshots.ToArray();
            }
        }

        internal static void ClearSessionData()
        {
            Interlocked.Exchange(ref _renderedFrames, 0L);
            Interlocked.Exchange(ref _campaignHours, 0L);
            Interlocked.Exchange(ref _missions, 0L);
            _trackedMission = null;
            lock (SnapshotSync)
            {
                ContextSnapshots.Clear();
            }
        }

        internal static void ResetLifecycleState()
        {
            _trackedMission = null;
            ClearSessionData();
        }

        private static void TrackMissionIdentity(GameMission current)
        {
            if (ReferenceEquals(current, _trackedMission))
            {
                return;
            }

            _trackedMission = current;
            TorMetricsAdapter.ClearInstanceCache();
            if (current != null)
            {
                Interlocked.Increment(ref _missions);
            }
        }

        private static ContextSnapshot CaptureContext(long frame)
        {
            int activeParties = -1;
            int settlements = -1;
            string campaignSpeed = null;
            GameCampaign campaign = GameCampaign.Current;
            if (campaign != null)
            {
                activeParties = MobileParty.All?.Count ?? 0;
                settlements = Settlement.All?.Count ?? 0;
                campaignSpeed = campaign.TimeControlMode + " x"
                    + campaign.SpeedUpMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }

            int livingAgents = -1;
            int totalAgents = -1;
            int missiles = -1;
            string battleType = null;
            GameMission mission = GameMission.Current;
            if (mission != null)
            {
                livingAgents = 0;
                totalAgents = 0;
                foreach (Agent agent in mission.AllAgents)
                {
                    totalAgents++;
                    if (agent != null && agent.IsActive() && agent.Health > 0f)
                    {
                        livingAgents++;
                    }
                }

                missiles = mission.MissilesList?.Count ?? 0;
                battleType = mission.Mode.ToString();
            }

            return new ContextSnapshot
            {
                UtcTimestamp = DateTime.UtcNow.ToString("O"),
                RenderedFrame = frame,
                CampaignSpeed = campaignSpeed,
                MapZoom = ReflectionMetricReader.TryReadMapZoom(EnableOptionalContextMetrics),
                ActivePartyCount = activeParties,
                SettlementCount = settlements,
                LivingAgentCount = livingAgents,
                TotalAgentCount = totalAgents,
                ActiveMissileCount = missiles,
                ActiveSpellOrEffectCount = TorMetricsAdapter.ReadActiveSpellOrEffectCount(mission),
                BattleType = battleType,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                ManagedBytes = GC.GetTotalMemory(false)
            };
        }
    }
}
