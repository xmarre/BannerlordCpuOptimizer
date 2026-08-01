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
        private readonly DateTime _startedUtc;
        private readonly long _wallStartTimestamp;
        private readonly long _processCpuStartTicks;
        private readonly int _gen0Start;
        private readonly int _gen1Start;
        private readonly int _gen2Start;
        private readonly long _managedBytesStart;
        private readonly long[] _frameHistogram = new long[HistogramBinCount];

        private long _lastFrameTimestamp;
        private long _renderedFrames;
        private long _totalFrameTicks;
        private long _maximumFrameTicks;
        private long _campaignHours;
        private long _missions;
        private GameMission _trackedMission;

        internal BenchmarkSession(string runLabel, string careerChoiceCacheMode, bool profilingEnabled)
        {
            SessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _runLabel = string.IsNullOrWhiteSpace(runLabel) ? "unnamed" : runLabel.Trim();
            _careerChoiceCacheMode = careerChoiceCacheMode ?? "unknown";
            _profilingEnabled = profilingEnabled;
            _startedUtc = DateTime.UtcNow;
            _wallStartTimestamp = Stopwatch.GetTimestamp();
            _processCpuStartTicks = ReadProcessCpuTicks();
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
                    _renderedFrames++;
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
            long processCpuEndTicks = ReadProcessCpuTicks();
            long managedBytesEnd = GC.GetTotalMemory(false);
            double wallSeconds = Math.Max(0.000001, (wallEndTimestamp - _wallStartTimestamp) / (double)Stopwatch.Frequency);
            double processCpuSeconds = Math.Max(0L, processCpuEndTicks - _processCpuStartTicks) / (double)TimeSpan.TicksPerSecond;
            int logicalProcessors = Math.Max(1, Environment.ProcessorCount);
            double averageFrameMilliseconds = _renderedFrames == 0L
                ? 0.0
                : _totalFrameTicks * 1000.0 / Stopwatch.Frequency / _renderedFrames;
            double cpuSecondsPerCampaignHour = _campaignHours == 0L ? 0.0 : processCpuSeconds / _campaignHours;
            double wallSecondsPerCampaignHour = _campaignHours == 0L ? 0.0 : wallSeconds / _campaignHours;

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
                ProcessCpuPercentOfOneLogicalCore = processCpuSeconds / wallSeconds * 100.0,
                ProcessCpuPercentOfWholeMachine = processCpuSeconds / wallSeconds / logicalProcessors * 100.0,
                RenderedFrames = _renderedFrames,
                ApplicationTicksPerSecond = _renderedFrames / wallSeconds,
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
                Notes = "Whole-process CPU includes every Bannerlord thread. Frame percentiles use a fixed 0.1 ms histogram with values at or above 250 ms grouped into the final bin. Use identical saves, module lists, camera state, campaign speed, and duration for A/B comparisons.",
                ProcessCpuSecondsPerCampaignHour = cpuSecondsPerCampaignHour,
                WallSecondsPerCampaignHour = wallSecondsPerCampaignHour
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
            if (_renderedFrames <= 0L)
            {
                return 0.0;
            }

            long target = (long)Math.Ceiling(_renderedFrames * percentile);
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

        private static long ReadProcessCpuTicks()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    return process.TotalProcessorTime.Ticks;
                }
            }
            catch
            {
                return 0L;
            }
        }
    }
}
