using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Core;

namespace BannerlordCpuOptimizer.Optimization
{
    public static class RaceLookupCache
    {
        private sealed class Entry
        {
            internal int Value;
            internal int Comparisons;
            internal long Hits;
            internal bool Active;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static bool _configured;
        private static bool _enabled;
        private static int _requiredComparisons = 256;
        private static int _auditEvery = 4096;
        private static long _calls;
        private static long _activeHits;
        private static long _audits;
        private static long _mismatches;
        private static string _disabledReason;

        internal static void Configure(bool enabled, int requiredComparisons, int auditEvery)
        {
            lock (Sync)
            {
                _configured = enabled;
                _enabled = enabled;
                _requiredComparisons = Math.Max(1, requiredComparisons);
                _auditEvery = Math.Max(1, auditEvery);
                ResetLocked();
            }
        }

        internal static void ResetSession()
        {
            lock (Sync)
            {
                _enabled = _configured;
                ResetLocked();
            }
        }

        internal static void Clear()
        {
            lock (Sync)
            {
                _configured = false;
                _enabled = false;
                ResetLocked();
            }
        }

        public static int GetRaceOrDefault(string raceId)
        {
            if (!_enabled || string.IsNullOrEmpty(raceId))
            {
                return FaceGen.GetRaceOrDefault(raceId);
            }

            Interlocked.Increment(ref _calls);
            lock (Sync)
            {
                if (!_enabled)
                {
                    return FaceGen.GetRaceOrDefault(raceId);
                }

                int reference = FaceGen.GetRaceOrDefault(raceId);
                if (!Entries.TryGetValue(raceId, out Entry entry))
                {
                    entry = new Entry { Value = reference, Comparisons = 1 };
                    Entries.Add(raceId, entry);
                    return reference;
                }

                if (!entry.Active)
                {
                    if (entry.Value != reference)
                    {
                        DisableLocked("shadow mismatch for " + raceId);
                        return reference;
                    }

                    entry.Comparisons++;
                    if (entry.Comparisons >= _requiredComparisons)
                    {
                        entry.Active = true;
                    }
                    return reference;
                }

                entry.Hits++;
                if (entry.Hits % _auditEvery == 0)
                {
                    _audits++;
                    if (entry.Value != reference)
                    {
                        DisableLocked("audit mismatch for " + raceId);
                        return reference;
                    }
                }

                _activeHits++;
                return entry.Value;
            }
        }

        private static void ResetLocked()
        {
            Entries.Clear();
            _calls = 0;
            _activeHits = 0;
            _audits = 0;
            _mismatches = 0;
            _disabledReason = null;
        }

        private static void DisableLocked(string reason)
        {
            _enabled = false;
            _mismatches++;
            _disabledReason = reason;
            Entries.Clear();
        }

        internal static string Describe()
        {
            lock (Sync)
            {
                int activeEntries = 0;
                foreach (Entry entry in Entries.Values)
                {
                    if (entry.Active)
                    {
                        activeEntries++;
                    }
                }

                return "configured=" + _configured
                    + " enabled=" + _enabled
                    + " entries=" + Entries.Count
                    + " activeEntries=" + activeEntries
                    + " calls=" + _calls
                    + " activeHits=" + _activeHits
                    + " audits=" + _audits
                    + " mismatches=" + _mismatches
                    + " disabledReason=" + (_disabledReason ?? "<none>");
            }
        }
    }
}
