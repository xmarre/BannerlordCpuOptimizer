using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class ProfileReportWriter
    {
        internal static string Write(ProfileReport report, string directory, string format)
        {
            Directory.CreateDirectory(directory);
            string basePath = Path.Combine(directory, "BannerlordCpuOptimizer-Profile-" + report.SessionId);
            bool writeJson = string.Equals(format, "Json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(format, "Both", StringComparison.OrdinalIgnoreCase);
            bool writeCsv = string.Equals(format, "Csv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(format, "Both", StringComparison.OrdinalIgnoreCase);

            if (!writeJson && !writeCsv)
            {
                writeJson = true;
            }

            if (writeJson)
            {
                WriteJson(basePath + ".json", report);
            }

            if (writeCsv)
            {
                WriteMethodsCsv(basePath + "-methods.csv", report);
                WriteContextCsv(basePath + "-context.csv", report);
                WriteOptimizationCsv(basePath + "-optimization.csv", report);
            }

            OptimizerLog.Info("Profile report written: " + basePath + ".");
            return basePath;
        }

        private static void WriteJson(string path, ProfileReport report)
        {
            string temporary = path + ".tmp";
            using (FileStream stream = File.Create(temporary))
            {
                var serializer = new DataContractJsonSerializer(typeof(ProfileReport));
                serializer.WriteObject(stream, report);
                stream.Flush(true);
            }

            Replace(temporary, path);
        }

        private static void WriteMethodsCsv(string path, ProfileReport report)
        {
            string temporary = path + ".tmp";
            using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("category,assembly,type,method,signature,sample_every,disabled,call_count,sampled_calls,sampled_total_ms,estimated_total_ms,max_ms,average_sampled_ms,calls_per_frame,calls_per_campaign_hour,calls_per_mission,sampled_allocated_bytes,estimated_allocated_bytes,max_allocated_bytes");
                foreach (MethodProfileSnapshot method in report.Methods)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(method.Category), Csv(method.DeclaringAssembly), Csv(method.DeclaringType), Csv(method.MethodName), Csv(method.Signature),
                        method.SampleEvery.ToString(CultureInfo.InvariantCulture), method.Disabled ? "true" : "false",
                        method.CallCount.ToString(CultureInfo.InvariantCulture), method.SampledCallCount.ToString(CultureInfo.InvariantCulture),
                        F(method.SampledTotalMilliseconds), F(method.EstimatedTotalMilliseconds), F(method.MaximumMilliseconds), F(method.AverageSampledMilliseconds),
                        F(method.CallsPerRenderedFrame), F(method.CallsPerCampaignHour), F(method.CallsPerMission),
                        method.SampledAllocatedBytes.ToString(CultureInfo.InvariantCulture), method.EstimatedAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                        method.MaximumAllocatedBytes.ToString(CultureInfo.InvariantCulture)
                    }));
                }
            }

            Replace(temporary, path);
        }

        private static void WriteContextCsv(string path, ProfileReport report)
        {
            string temporary = path + ".tmp";
            using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("utc_timestamp,rendered_frame,campaign_speed,map_zoom,active_parties,settlements,living_agents,total_agents,active_missiles,active_spells_or_effects,battle_type,gen0,gen1,gen2,managed_bytes");
                foreach (ContextSnapshot context in report.Context)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(context.UtcTimestamp), context.RenderedFrame.ToString(CultureInfo.InvariantCulture), Csv(context.CampaignSpeed),
                        context.MapZoom.HasValue ? F(context.MapZoom.Value) : string.Empty,
                        context.ActivePartyCount.ToString(CultureInfo.InvariantCulture), context.SettlementCount.ToString(CultureInfo.InvariantCulture),
                        context.LivingAgentCount.ToString(CultureInfo.InvariantCulture), context.TotalAgentCount.ToString(CultureInfo.InvariantCulture),
                        context.ActiveMissileCount.ToString(CultureInfo.InvariantCulture), context.ActiveSpellOrEffectCount.ToString(CultureInfo.InvariantCulture),
                        Csv(context.BattleType), context.Gen0Collections.ToString(CultureInfo.InvariantCulture),
                        context.Gen1Collections.ToString(CultureInfo.InvariantCulture), context.Gen2Collections.ToString(CultureInfo.InvariantCulture),
                        context.ManagedBytes.ToString(CultureInfo.InvariantCulture)
                    }));
                }
            }

            Replace(temporary, path);
        }

        private static void WriteOptimizationCsv(string path, ProfileReport report)
        {
            string temporary = path + ".tmp";
            using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("configured_mode,runtime_state,session_generation,campaign_bound,cache_entries,validated_entries,calls,active_hits,misses,stores,shadow_comparisons,per_id_validations,mismatches,null_results,promotions,audits,disabled_reason");
                var cache = report.CareerChoiceCache;
                if (cache != null)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(cache.ConfiguredMode), Csv(cache.RuntimeState),
                        cache.SessionGeneration.ToString(CultureInfo.InvariantCulture),
                        cache.CampaignBound ? "true" : "false",
                        cache.CacheEntries.ToString(CultureInfo.InvariantCulture),
                        cache.ValidatedEntries.ToString(CultureInfo.InvariantCulture),
                        cache.Calls.ToString(CultureInfo.InvariantCulture),
                        cache.ActiveHits.ToString(CultureInfo.InvariantCulture),
                        cache.Misses.ToString(CultureInfo.InvariantCulture),
                        cache.Stores.ToString(CultureInfo.InvariantCulture),
                        cache.ShadowComparisons.ToString(CultureInfo.InvariantCulture),
                        cache.PerIdValidations.ToString(CultureInfo.InvariantCulture),
                        cache.Mismatches.ToString(CultureInfo.InvariantCulture),
                        cache.NullResults.ToString(CultureInfo.InvariantCulture),
                        cache.Promotions.ToString(CultureInfo.InvariantCulture),
                        cache.Audits.ToString(CultureInfo.InvariantCulture),
                        Csv(cache.DisabledReason)
                    }));
                }
            }

            Replace(temporary, path);
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
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
