using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace BannerlordCpuOptimizer.Profiling
{
    internal sealed class MethodProfile
    {
        private long _callCount;
        private long _sampledCallCount;
        private long _totalSampledTicks;
        private long _maximumTicks;
        private long _totalSampledAllocations;
        private long _maximumAllocation;
        private int _disabled;

        internal MethodProfile(MethodBase method, string category, int sampleEvery)
        {
            Method = method;
            Category = category;
            SampleEvery = Math.Max(1, sampleEvery);
            DeclaringAssembly = method.Module.Assembly.GetName().Name;
            DeclaringType = method.DeclaringType?.FullName ?? "<global>";
            MethodName = method.Name;
            Signature = ProfilerTargetSpec.FormatSignature(method);
        }

        internal MethodBase Method { get; }
        internal string Category { get; }
        internal int SampleEvery { get; }
        internal string DeclaringAssembly { get; }
        internal string DeclaringType { get; }
        internal string MethodName { get; }
        internal string Signature { get; }
        internal bool IsDisabled => Volatile.Read(ref _disabled) != 0;

        internal ProfileCallState Enter()
        {
            long callNumber = Interlocked.Increment(ref _callCount);
            if (IsDisabled || ((callNumber - 1L) % SampleEvery) != 0L)
            {
                return default(ProfileCallState);
            }

            return new ProfileCallState { Profile = this, StartTimestamp = Stopwatch.GetTimestamp(), AllocationBefore = AllocationCounter.Read(), Sampled = true };
        }

        internal void Exit(long startTimestamp, long allocationBefore)
        {
            long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
            Interlocked.Increment(ref _sampledCallCount);
            Interlocked.Add(ref _totalSampledTicks, elapsed);
            UpdateMaximum(ref _maximumTicks, elapsed);
            if (allocationBefore >= 0L)
            {
                long allocationAfter = AllocationCounter.Read();
                if (allocationAfter >= allocationBefore)
                {
                    long allocated = allocationAfter - allocationBefore;
                    Interlocked.Add(ref _totalSampledAllocations, allocated);
                    UpdateMaximum(ref _maximumAllocation, allocated);
                }
            }
        }

        internal void Disable() => Interlocked.Exchange(ref _disabled, 1);

        internal void Reset()
        {
            Interlocked.Exchange(ref _callCount, 0L);
            Interlocked.Exchange(ref _sampledCallCount, 0L);
            Interlocked.Exchange(ref _totalSampledTicks, 0L);
            Interlocked.Exchange(ref _maximumTicks, 0L);
            Interlocked.Exchange(ref _totalSampledAllocations, 0L);
            Interlocked.Exchange(ref _maximumAllocation, 0L);
            Interlocked.Exchange(ref _disabled, 0);
        }

        internal MethodProfileSnapshot Snapshot(long renderedFrames, long campaignHours, long missions)
        {
            long calls = Interlocked.Read(ref _callCount);
            long sampledCalls = Interlocked.Read(ref _sampledCallCount);
            long sampledTicks = Interlocked.Read(ref _totalSampledTicks);
            long sampledAllocations = Interlocked.Read(ref _totalSampledAllocations);
            double tickToMilliseconds = 1000.0 / Stopwatch.Frequency;
            double sampledTotalMs = sampledTicks * tickToMilliseconds;
            return new MethodProfileSnapshot
            {
                Category = Category,
                DeclaringAssembly = DeclaringAssembly,
                DeclaringType = DeclaringType,
                MethodName = MethodName,
                Signature = Signature,
                SampleEvery = SampleEvery,
                Disabled = IsDisabled,
                CallCount = calls,
                SampledCallCount = sampledCalls,
                SampledTotalMilliseconds = sampledTotalMs,
                EstimatedTotalMilliseconds = sampledCalls == 0L ? 0.0 : sampledTotalMs * calls / sampledCalls,
                MaximumMilliseconds = Interlocked.Read(ref _maximumTicks) * tickToMilliseconds,
                AverageSampledMilliseconds = sampledCalls == 0L ? 0.0 : sampledTotalMs / sampledCalls,
                CallsPerRenderedFrame = renderedFrames <= 0L ? 0.0 : (double)calls / renderedFrames,
                CallsPerCampaignHour = campaignHours <= 0L ? 0.0 : (double)calls / campaignHours,
                CallsPerMission = missions <= 0L ? 0.0 : (double)calls / missions,
                SampledAllocatedBytes = sampledAllocations,
                EstimatedAllocatedBytes = sampledCalls == 0L ? 0L : (long)Math.Round((double)sampledAllocations * calls / sampledCalls),
                MaximumAllocatedBytes = Interlocked.Read(ref _maximumAllocation)
            };
        }

        private static void UpdateMaximum(ref long target, long candidate)
        {
            long current = Interlocked.Read(ref target);
            while (candidate > current)
            {
                long previous = Interlocked.CompareExchange(ref target, candidate, current);
                if (previous == current) { return; }
                current = previous;
            }
        }
    }
}
