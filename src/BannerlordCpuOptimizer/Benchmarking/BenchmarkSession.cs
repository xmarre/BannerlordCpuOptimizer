using System;
using System.Diagnostics;
using BannerlordCpuOptimizer.Optimization;
using GameMission = TaleWorlds.MountAndBlade.Mission;

namespace BannerlordCpuOptimizer.Benchmarking
{
    internal sealed class BenchmarkSession
    {
        private const double HistogramBinMilliseconds = 0.1;
        private const int HistogramBinCount = 2501;

        private readonly string _runLabel;
        private readonly string _careerChoiceCacheMode;
        private readonly bool _profilingEnabled;
        private readonly string _startCondition;
        private readonly string _startTimeControlMode;
        private readonly double _startStabilitySeconds;
        private readonly DateTime _startedUtc;
        private readonly long _wallStartTimestamp;
        private readonly long _processCpuStartTicks;
        private readonly bool _processCpuStartAvailable;
        private readonly int _gen0Start;
        private readonly int _gen1Start;
        private readonly int _gen2Start;
        private readonly long _managedBytesStart;
        private readonly long[] _frameHistogram = new long[HistogramBinCount];

        private long _lastFrameTimestamp;
        private long _applicationTicks;
        private long _totalFrameTicks;
        private long _maximumFrameTicks;
        private long _campaignHours;
        private long _missions;
        private GameMission _trackedMission;

        internal BenchmarkSession(
            string runLabel,
            string careerChoiceCacheMode,
            bool profilingEnabled,
            string startCondition,
            string startTimeControlMode,
            double startStabilitySeconds)
        {
            SessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _runLabel = string.IsNullOrWhiteSpace(runLabel) ? "unnamed" : runLabel.Trim();
            _careerChoiceCacheMode = careerChoiceCacheMode ?? "unknown";
            _profilingEnabled = profilingEnabled;
            _startCondition = startCondition ?? "unknown";
            _startTimeControlMode = startTimeControlMode ?? "unknown";
            _startStabilitySeconds = Math.Max(0.0, startStabilitySeconds);
            _startedUtc = DateTime.UtcNow;
            _wallStartTimestamp = Stopwatch.GetTimestamp();
            _processCpuStartAvailable = TryReadProcessCpuTicks(out long processCpuStartTicks);
            _processCpuStartTicks = processCpuStartTicks;
            _gen0Start = GC.CollectionCount(0);
            _gen1Start = GC.CollectionCount(1);
            _gen2Start = GC.CollectionCount(2);
            _managedBytesStart = GC.GetTotalMemory(false);
        }

        internal string SessionId { get; }
        internal string RunLabel => _runLabel;

        internal void OnApplicationTick()
        {
            long now = Stopwatch.GetTimestamp();
            if (_lastFrameTimestamp != 0L)
            {
                long elapsed = now - _lastFrameTimestamp;
                if (elapsed > 0L)
                {
                    _applicationTicks++;
                    _totalFrameTicks += elapsed;
                    if (elapsed > _maximumFrameTicks)
                    {
                        _maximumFrameTicks = elapsed;
                    }

                    double milliseconds = elapsed * 1000.0 / Stopwatch.Frequency;
                    int bin = (int)(milliseconds / HistogramBinMilliseconds);
                    if (bin < 0)
                    {
                        bin = 0;
                    }
                    else if (bin >= HistogramBinCount)
                    {
                        bin = HistogramBinCount - 1;
                    }

                    _frameHistogram[bin]++;
                }
            }

            _lastFrameTimestamp = now;
            TrackMission(GameMission.Current);
        }

        internal void CampaignHourElapsed()
        {
            _campaignHours++;
        }

        internal BenchmarkReport Complete(string reason)
        {
            long wallEndTimestamp = Stopwatch.GetTimestamp();
            bool processCpuEndAvailable = TryReadProcessCpuTicks(out long processCpuEndTicks);
            bool processCpuAvailable = _processCpuStartAvailable
                && processCpuEndAvailable
                && processCpuEndTicks >= _processCpuStartTicks;
            long managedBytesEnd = GC.GetTotalMemory(false);
            double wallSeconds = Math.Max(0.000001, (wallEndTimestamp - _wallStartTimestamp) / (double)Stopwatch.Frequency);
            double processCpuSeconds = processCpuAvailable
                ? (processCpuEndTicks - _processCpuStartTicks) / (double)TimeSpan.TicksPerSecond
                : 0.0;
            int logicalProcessors = Math.Max(1, Environment.ProcessorCount);
            double averageFrameMilliseconds = _applicationTicks == 0L
                ? 0.0
                : _totalFrameTicks * 1000.0 / Stopwatch.Frequency / _applicationTicks;
            double cpuSecondsPerCampaignHour = !processCpuAvailable || _campaignHours == 0L
                ? 0.0
                : processCpuSeconds / _campaignHours;
            double wallSecondsPerCampaignHour = _campaignHours == 0L ? 0.0 : wallSeconds / _campaignHours;
            double applicationTicksPerCampaignHour = _campaignHours == 0L
                ? 0.0
                : _applicationTicks / (double)_campaignHours;
            double processCpuMillisecondsPerApplicationTick = !processCpuAvailable || _applicationTicks == 0L
                ? 0.0
                : processCpuSeconds * 1000.0 / _applicationTicks;
            double wallMillisecondsPerApplicationTick = _applicationTicks == 0L
                ? 0.0
                : wallSeconds * 1000.0 / _applicationTicks;

            return new BenchmarkReport
            {
                SessionId = SessionId,
                RunLabel = _runLabel,
                CompletionReason = reason,
                StartedUtc = _startedUtc.ToString("O"),
                EndedUtc = DateTime.UtcNow.ToString("O"),
                OptimizerVersion = typeof(BenchmarkSession).Assembly.GetName().Version?.ToString() ?? "unknown",
                ProfilingEnabled = _profilingEnabled,
                CareerChoiceCacheMode = _careerChoiceCacheMode,
                LogicalProcessorCount = logicalProcessors,
                WallSeconds = wallSeconds,
                ProcessCpuSeconds = processCpuSeconds,
                ProcessCpuPercentOfOneLogicalCore = processCpuAvailable ? processCpuSeconds / wallSeconds * 100.0 : 0.0,
                ProcessCpuPercentOfWholeMachine = processCpuAvailable ? processCpuSeconds / wallSeconds / logicalProcessors * 100.0 : 0.0,
                ApplicationTicks = _applicationTicks,
                ApplicationTicksPerSecond = _applicationTicks / wallSeconds,
                AverageFrameMilliseconds = averageFrameMilliseconds,
                P50FrameMilliseconds = Percentile(0.50),
                P95FrameMilliseconds = Percentile(0.95),
                P99FrameMilliseconds = Percentile(0.99),
                MaximumFrameMilliseconds = _maximumFrameTicks * 1000.0 / Stopwatch.Frequency,
                CampaignHours = _campaignHours,
                CampaignHoursPerRealMinute = _campaignHours / wallSeconds * 60.0,
                Missions = _missions,
                Gen0CollectionsDelta = GC.CollectionCount(0) - _gen0Start,
                Gen1CollectionsDelta = GC.CollectionCount(1) - _gen1Start,
                Gen2CollectionsDelta = GC.CollectionCount(2) - _gen2Start,
                ManagedBytesStart = _managedBytesStart,
                ManagedBytesEnd = managedBytesEnd,
                ManagedBytesDelta = managedBytesEnd - _managedBytesStart,
                CareerChoiceCache = CareerChoiceCache.Snapshot(),
                MapVisibilityOptimization = MapVisibilityEarlyExit.Describe(),
                RaceLookupOptimization = RaceLookupCache.Describe(),
                WeeklyCompanionOptimization = WeeklyCompanionLinqElision.Describe(),
                Notes = "Measurement begins only after maximum campaign speed remains stable for the recorded start-gate interval. Load time, manual fast-forward selection, and initial stabilization are excluded. Whole-process CPU includes every Bannerlord thread. Frame intervals are measured between MBSubModuleBase.OnApplicationTick callbacks. Percentiles use a fixed 0.1 ms histogram with values at or above 250 ms grouped into the final bin; the maximum remains exact.",
                ProcessCpuSecondsPerCampaignHour = cpuSecondsPerCampaignHour,
                WallSecondsPerCampaignHour = wallSecondsPerCampaignHour,
                ProcessCpuMeasurementAvailable = processCpuAvailable,
                StartCondition = _startCondition,
                StartTimeControlMode = _startTimeControlMode,
                StartStabilitySeconds = _startStabilitySeconds,
                ApplicationTicksPerCampaignHour = applicationTicksPerCampaignHour,
                ProcessCpuMillisecondsPerApplicationTick = processCpuMillisecondsPerApplicationTick,
                WallMillisecondsPerApplicationTick = wallMillisecondsPerApplicationTick
            };
        }

        private void TrackMission(GameMission current)
        {
            if (ReferenceEquals(current, _trackedMission))
            {
                return;
            }

            _trackedMission = current;
            if (current != null)
            {
                _missions++;
            }
        }

        private double Percentile(double percentile)
        {
            if (_applicationTicks <= 0L)
            {
                return 0.0;
            }

            long target = (long)Math.Ceiling(_applicationTicks * percentile);
            long cumulative = 0L;
            for (int index = 0; index < _frameHistogram.Length; index++)
            {
                cumulative += _frameHistogram[index];
                if (cumulative >= target)
                {
                    return (index + 1) * HistogramBinMilliseconds;
                }
            }

            return HistogramBinCount * HistogramBinMilliseconds;
        }

        private static bool TryReadProcessCpuTicks(out long ticks)
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    ticks = process.TotalProcessorTime.Ticks;
                    return true;
                }
            }
            catch
            {
                ticks = 0L;
                return false;
            }
        }
    }
}
