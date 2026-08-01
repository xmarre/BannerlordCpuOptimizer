using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Benchmarking
{
    internal static class BenchmarkReportWriter
    {
        internal static string Write(BenchmarkReport report, string directory, string format)
        {
            Directory.CreateDirectory(directory);
            string basePath = Path.Combine(directory, "BannerlordCpuOptimizer-Benchmark-" + report.SessionId + "-" + Sanitize(report.RunLabel));
            bool writeJson = string.Equals(format, "Json", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "Both", StringComparison.OrdinalIgnoreCase);
            bool writeCsv = string.Equals(format, "Csv", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "Both", StringComparison.OrdinalIgnoreCase);
            if (!writeJson && !writeCsv)
            {
                writeJson = true;
                writeCsv = true;
            }

            if (writeJson)
            {
                WriteJson(basePath + ".json", report);
            }

            if (writeCsv)
            {
                WriteCsv(basePath + "-summary.csv", report);
            }

            OptimizerLog.Info("Benchmark report written: " + basePath + ".");
            return basePath;
        }

        private static void WriteJson(string path, BenchmarkReport report)
        {
            string temporary = path + ".tmp";
            using (FileStream stream = File.Create(temporary))
            {
                new DataContractJsonSerializer(typeof(BenchmarkReport)).WriteObject(stream, report);
                stream.Flush(true);
            }
            Replace(temporary, path);
        }

        private static void WriteCsv(string path, BenchmarkReport report)
        {
            string temporary = path + ".tmp";
            using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("session_id,run_label,completion_reason,optimizer_version,profiling_enabled,career_choice_cache_mode,logical_processors,process_cpu_measurement_available,wall_seconds,process_cpu_seconds,process_cpu_seconds_per_campaign_hour,wall_seconds_per_campaign_hour,cpu_percent_one_logical_core,cpu_percent_whole_machine,application_ticks,application_ticks_per_second,average_frame_ms,p50_frame_ms,p95_frame_ms,p99_frame_ms,max_frame_ms,campaign_hours,campaign_hours_per_real_minute,missions,gen0,gen1,gen2,managed_bytes_start,managed_bytes_end,managed_bytes_delta,cache_runtime_state,cache_calls,cache_active_hits,cache_mismatches,cache_promotions,cache_disabled_reason,map_visibility_optimization,race_lookup_optimization,weekly_companion_optimization");
                var cache = report.CareerChoiceCache;
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(report.SessionId), Csv(report.RunLabel), Csv(report.CompletionReason), Csv(report.OptimizerVersion),
                    report.ProfilingEnabled ? "true" : "false", Csv(report.CareerChoiceCacheMode),
                    report.LogicalProcessorCount.ToString(CultureInfo.InvariantCulture), report.ProcessCpuMeasurementAvailable ? "true" : "false",
                    F(report.WallSeconds), F(report.ProcessCpuSeconds), F(report.ProcessCpuSecondsPerCampaignHour), F(report.WallSecondsPerCampaignHour),
                    F(report.ProcessCpuPercentOfOneLogicalCore), F(report.ProcessCpuPercentOfWholeMachine),
                    report.ApplicationTicks.ToString(CultureInfo.InvariantCulture), F(report.ApplicationTicksPerSecond),
                    F(report.AverageFrameMilliseconds), F(report.P50FrameMilliseconds), F(report.P95FrameMilliseconds), F(report.P99FrameMilliseconds), F(report.MaximumFrameMilliseconds),
                    report.CampaignHours.ToString(CultureInfo.InvariantCulture), F(report.CampaignHoursPerRealMinute), report.Missions.ToString(CultureInfo.InvariantCulture),
                    report.Gen0CollectionsDelta.ToString(CultureInfo.InvariantCulture), report.Gen1CollectionsDelta.ToString(CultureInfo.InvariantCulture), report.Gen2CollectionsDelta.ToString(CultureInfo.InvariantCulture),
                    report.ManagedBytesStart.ToString(CultureInfo.InvariantCulture), report.ManagedBytesEnd.ToString(CultureInfo.InvariantCulture), report.ManagedBytesDelta.ToString(CultureInfo.InvariantCulture),
                    Csv(cache == null ? null : cache.RuntimeState),
                    (cache == null ? 0L : cache.Calls).ToString(CultureInfo.InvariantCulture),
                    (cache == null ? 0L : cache.ActiveHits).ToString(CultureInfo.InvariantCulture),
                    (cache == null ? 0L : cache.Mismatches).ToString(CultureInfo.InvariantCulture),
                    (cache == null ? 0L : cache.Promotions).ToString(CultureInfo.InvariantCulture),
                    Csv(cache == null ? null : cache.DisabledReason),
                    Csv(report.MapVisibilityOptimization),
                    Csv(report.RaceLookupOptimization),
                    Csv(report.WeeklyCompanionOptimization)
                }));
            }
            Replace(temporary, path);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }
            var builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
            }
            return builder.ToString().Trim('-');
        }

        private static string Csv(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string F(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static void Replace(string temporary, string target)
        {
            if (File.Exists(target))
            {
                File.Replace(temporary, target, null);
            }
            else
            {
                File.Move(temporary, target);
            }
        }
    }
}
