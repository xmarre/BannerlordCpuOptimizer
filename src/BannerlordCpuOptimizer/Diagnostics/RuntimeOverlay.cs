using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BannerlordCpuOptimizer.Profiling;

namespace BannerlordCpuOptimizer.Diagnostics
{
    internal sealed class RuntimeOverlay
    {
        private long _nextRefresh;
        private string _text = "Bannerlord CPU Optimizer profiler starting...";

        internal void Tick()
        {
            long now = Stopwatch.GetTimestamp();
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + Stopwatch.Frequency;
                _text = BuildText();
            }

            try
            {
                TaleWorlds.Library.Debug.RenderDebugText(20f, 20f, _text, uint.MaxValue, 12f);
            }
            catch (Exception exception)
            {
                OptimizerLog.WriteExceptionOnce("runtime-overlay", "Runtime overlay disabled", exception);
                _nextRefresh = long.MaxValue;
                _text = string.Empty;
            }
        }

        private static string BuildText()
        {
            var builder = new StringBuilder(512);
            builder.Append("BannerlordCpuOptimizer profiler | methods=")
                .Append(MethodProfiler.RegisteredCount)
                .Append(" frames=")
                .Append(FrameProfiler.RenderedFrames)
                .AppendLine();

            foreach (MethodProfileSnapshot method in MethodProfiler
                .Snapshot(FrameProfiler.RenderedFrames, FrameProfiler.CampaignHours, FrameProfiler.Missions)
                .Take(5))
            {
                builder.Append(method.MethodName)
                    .Append(" | est ")
                    .Append(method.EstimatedTotalMilliseconds.ToString("0.0"))
                    .Append(" ms | calls ")
                    .Append(method.CallCount)
                    .AppendLine();
            }

            return builder.ToString();
        }
    }
}
