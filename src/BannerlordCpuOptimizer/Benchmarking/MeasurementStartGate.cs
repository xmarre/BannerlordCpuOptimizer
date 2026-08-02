using System;
using System.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace BannerlordCpuOptimizer.Benchmarking
{
    internal sealed class MeasurementStartGate
    {
        internal const double RequiredStableSeconds = 1.5;

        private long _maximumSpeedSinceTimestamp;

        internal bool IsArmed { get; private set; }

        internal void Arm()
        {
            IsArmed = true;
            _maximumSpeedSinceTimestamp = 0L;
        }

        internal void Disarm()
        {
            IsArmed = false;
            _maximumSpeedSinceTimestamp = 0L;
        }

        internal bool TryOpen(Campaign campaign, out CampaignTimeControlMode startMode)
        {
            startMode = CampaignTimeControlMode.Stop;
            if (!IsArmed)
            {
                return false;
            }

            if (campaign == null
                || campaign.TimeControlModeLock
                || Mission.Current != null
                || !IsMaximumCampaignSpeed(campaign.TimeControlMode))
            {
                _maximumSpeedSinceTimestamp = 0L;
                return false;
            }

            long now = Stopwatch.GetTimestamp();
            if (_maximumSpeedSinceTimestamp == 0L)
            {
                _maximumSpeedSinceTimestamp = now;
                return false;
            }

            double stableSeconds = (now - _maximumSpeedSinceTimestamp) / (double)Stopwatch.Frequency;
            if (stableSeconds < RequiredStableSeconds)
            {
                return false;
            }

            startMode = campaign.TimeControlMode;
            Disarm();
            return true;
        }

        internal static bool IsMaximumCampaignSpeed(CampaignTimeControlMode mode)
        {
            return mode == CampaignTimeControlMode.StoppableFastForward
                || mode == CampaignTimeControlMode.UnstoppableFastForward
                || mode == CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime;
        }
    }
}
