using System;
using System.Collections.Generic;
using BannerlordCpuOptimizer.Diagnostics;

namespace BannerlordCpuOptimizer.Optimization
{
    internal struct CareerChoiceCallState
    {
        internal int Generation;
        internal string Id;
        internal object Expected;
        internal bool HadExpected;
        internal bool Served;
        internal bool Audit;
        internal bool Tracked;
    }

    internal static class CareerChoiceCache
    {
        private sealed class CacheEntry
        {
            internal object Value;
            internal int ShadowMatches;
            internal bool Validated;
        }

        private enum ConfiguredMode
        {
            Disabled,
            ShadowOnly,
            ShadowThenEnable
        }

        private enum RuntimeState
        {
            Disabled,
            Shadow,
            Enabled
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, CacheEntry> Cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

        private static ConfiguredMode _configuredMode;
        private static RuntimeState _runtimeState;
        private static int _requiredShadowComparisons = 256;
        private static int _minimumDistinctIds = 1;
        private static int _auditEvery = 1024;
        private static int _generation;
        private static object _campaignIdentity;
        private static long _calls;
        private static long _activeHits;
        private static long _misses;
        private static long _stores;
        private static long _shadowComparisons;
        private static long _perIdValidations;
        private static long _mismatches;
        private static long _nullResults;
        private static long _promotions;
        private static long _audits;
        private static long _activeHitCandidates;
        private static string _disabledReason;

        internal static bool IsConfigured
        {
            get
            {
                lock (Sync)
                {
                    return _configuredMode != ConfiguredMode.Disabled;
                }
            }
        }

        internal static void Configure(string mode, int requiredShadowComparisons, int minimumDistinctIds, int auditEvery)
        {
            lock (Sync)
            {
                _configuredMode = ParseMode(mode);
                _requiredShadowComparisons = Math.Max(1, requiredShadowComparisons);
                _minimumDistinctIds = Math.Max(1, minimumDistinctIds);
                _auditEvery = Math.Max(1, auditEvery);
                _runtimeState = RuntimeState.Disabled;
                _campaignIdentity = null;
                ClearLocked(resetStatistics: true, reason: "waiting for a campaign session");
            }
        }

        internal static void BeginGameSession(object campaignIdentity)
        {
            string configuredMode;
            string runtimeState;
            lock (Sync)
            {
                unchecked { _generation++; }
                ClearLocked(resetStatistics: true, reason: campaignIdentity == null ? "waiting for Campaign.Current" : null);
                _campaignIdentity = campaignIdentity;
                _runtimeState = _configuredMode == ConfiguredMode.Disabled || campaignIdentity == null
                    ? RuntimeState.Disabled
                    : RuntimeState.Shadow;
                configuredMode = _configuredMode.ToString();
                runtimeState = _runtimeState.ToString();
            }

            OptimizerLog.Info("Career-choice cache session reset: mode=" + configuredMode
                + " state=" + runtimeState
                + " campaignBound=" + (campaignIdentity != null)
                + " requiredComparisons=" + _requiredShadowComparisons
                + " minimumDistinctIds=" + _minimumDistinctIds
                + " auditEvery=" + _auditEvery + ".");
        }

        internal static void EndGameSession()
        {
            lock (Sync)
            {
                unchecked { _generation++; }
                Cache.Clear();
                _campaignIdentity = null;
                _runtimeState = RuntimeState.Disabled;
                _disabledReason = "campaign session ended";
            }
        }

        internal static void ClearAll()
        {
            lock (Sync)
            {
                unchecked { _generation++; }
                Cache.Clear();
                _campaignIdentity = null;
                _runtimeState = RuntimeState.Disabled;
                _disabledReason = "module teardown";
                ResetStatisticsLocked();
            }
        }

        internal static bool TryServeOrBegin(
            string id,
            object currentCampaign,
            out object result,
            out CareerChoiceCallState state)
        {
            result = null;
            state = default(CareerChoiceCallState);
            try
            {
                lock (Sync)
                {
                    _calls++;
                    if (_runtimeState == RuntimeState.Disabled || string.IsNullOrEmpty(id))
                    {
                        return false;
                    }

                    if (currentCampaign == null || !ReferenceEquals(_campaignIdentity, currentCampaign))
                    {
                        unchecked { _generation++; }
                        Cache.Clear();
                        _campaignIdentity = null;
                        _runtimeState = RuntimeState.Disabled;
                        _disabledReason = "campaign identity changed; waiting for lifecycle synchronization";
                        return false;
                    }

                    bool hadExpected = Cache.TryGetValue(id, out CacheEntry entry);
                    object expected = hadExpected ? entry.Value : null;
                    if (!hadExpected)
                    {
                        _misses++;
                    }

                    bool audit = false;
                    if (_runtimeState == RuntimeState.Enabled && hadExpected && entry.Validated)
                    {
                        _activeHitCandidates++;
                        audit = (_activeHitCandidates % _auditEvery) == 0L;
                    }

                    bool served = _runtimeState == RuntimeState.Enabled
                        && hadExpected
                        && entry.Validated
                        && !audit;
                    state = new CareerChoiceCallState
                    {
                        Generation = _generation,
                        Id = id,
                        Expected = expected,
                        HadExpected = hadExpected,
                        Served = served,
                        Audit = audit,
                        Tracked = true
                    };

                    if (!served)
                    {
                        return false;
                    }

                    _activeHits++;
                    result = expected;
                    return true;
                }
            }
            catch (Exception exception)
            {
                state = default(CareerChoiceCallState);
                DisableForSession("prefix failure: " + exception.GetType().Name, exception);
                return false;
            }
        }

        internal static void CompleteCall(string id, object result, CareerChoiceCallState state)
        {
            if (!state.Tracked)
            {
                return;
            }

            try
            {
                lock (Sync)
                {
                    if (state.Generation != _generation || _runtimeState == RuntimeState.Disabled)
                    {
                        return;
                    }

                    if (!string.Equals(state.Id, id, StringComparison.Ordinal))
                    {
                        DisableLocked("Harmony state id mismatch");
                        return;
                    }

                    if (result == null)
                    {
                        _nullResults++;
                        if (state.HadExpected)
                        {
                            _mismatches++;
                            DisableLocked("cached non-null result changed to null for id '" + id + "'");
                        }
                        return;
                    }

                    if (state.HadExpected)
                    {
                        if (!ReferenceEquals(state.Expected, result))
                        {
                            _mismatches++;
                            DisableLocked("reference mismatch for id '" + id + "'");
                            return;
                        }

                        if (!state.Served)
                        {
                            CacheEntry entry = Cache[id];
                            if (state.Audit)
                            {
                                _audits++;
                            }
                            else
                            {
                                _shadowComparisons++;
                                if (!entry.Validated)
                                {
                                    entry.ShadowMatches++;
                                    if (_runtimeState == RuntimeState.Enabled && entry.ShadowMatches >= 1)
                                    {
                                        entry.Validated = true;
                                        _perIdValidations++;
                                    }
                                }

                                if (_runtimeState == RuntimeState.Shadow)
                                {
                                    TryPromoteLocked();
                                }
                            }
                        }

                        return;
                    }

                    Cache[id] = new CacheEntry { Value = result };
                    _stores++;
                }
            }
            catch (Exception exception)
            {
                DisableForSession("postfix failure: " + exception.GetType().Name, exception);
            }
        }

        internal static CareerChoiceCacheSnapshot Snapshot()
        {
            lock (Sync)
            {
                return new CareerChoiceCacheSnapshot
                {
                    ConfiguredMode = _configuredMode.ToString(),
                    RuntimeState = _runtimeState.ToString(),
                    SessionGeneration = _generation,
                    CampaignBound = _campaignIdentity != null,
                    CacheEntries = Cache.Count,
                    ValidatedEntries = CountValidatedEntriesLocked(),
                    Calls = _calls,
                    ActiveHits = _activeHits,
                    Misses = _misses,
                    Stores = _stores,
                    ShadowComparisons = _shadowComparisons,
                    PerIdValidations = _perIdValidations,
                    Mismatches = _mismatches,
                    NullResults = _nullResults,
                    Promotions = _promotions,
                    Audits = _audits,
                    DisabledReason = _disabledReason
                };
            }
        }

        private static void TryPromoteLocked()
        {
            int validatedEntries = CountShadowValidatedEntriesLocked();
            if (_configuredMode != ConfiguredMode.ShadowThenEnable
                || _runtimeState != RuntimeState.Shadow
                || _shadowComparisons < _requiredShadowComparisons
                || validatedEntries < _minimumDistinctIds)
            {
                return;
            }

            foreach (CacheEntry entry in Cache.Values)
            {
                if (!entry.Validated && entry.ShadowMatches >= 1)
                {
                    entry.Validated = true;
                    _perIdValidations++;
                }
            }

            _runtimeState = RuntimeState.Enabled;
            _promotions++;
            OptimizerLog.Info("Career-choice cache enabled after " + _shadowComparisons
                + " reference-identical shadow comparisons across " + validatedEntries
                + " independently validated id(s). New ids remain shadow-only until individually validated.");
        }

        private static int CountShadowValidatedEntriesLocked()
        {
            int count = 0;
            foreach (CacheEntry entry in Cache.Values)
            {
                if (entry.ShadowMatches >= 1)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountValidatedEntriesLocked()
        {
            int count = 0;
            foreach (CacheEntry entry in Cache.Values)
            {
                if (entry.Validated)
                {
                    count++;
                }
            }

            return count;
        }

        private static void DisableForSession(string reason, Exception exception)
        {
            lock (Sync)
            {
                DisableLocked(reason);
            }

            if (exception != null)
            {
                OptimizerLog.WriteExceptionOnce(
                    "career-choice-cache-fallback-" + _generation,
                    "Career-choice cache disabled for this campaign session",
                    exception);
            }
        }

        private static void DisableLocked(string reason)
        {
            Cache.Clear();
            _campaignIdentity = null;
            _runtimeState = RuntimeState.Disabled;
            _disabledReason = reason;
            OptimizerLog.Once(
                "career-choice-cache-disabled-" + _generation,
                "ERROR",
                "Career-choice cache disabled for this campaign session: " + reason
                    + ". Original TOR behavior will continue.");
        }

        private static void ClearLocked(bool resetStatistics, string reason)
        {
            Cache.Clear();
            _disabledReason = reason;
            if (resetStatistics)
            {
                ResetStatisticsLocked();
            }
        }

        private static void ResetStatisticsLocked()
        {
            _calls = 0L;
            _activeHits = 0L;
            _misses = 0L;
            _stores = 0L;
            _shadowComparisons = 0L;
            _perIdValidations = 0L;
            _mismatches = 0L;
            _nullResults = 0L;
            _promotions = 0L;
            _audits = 0L;
            _activeHitCandidates = 0L;
        }

        private static ConfiguredMode ParseMode(string mode)
        {
            if (string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                return ConfiguredMode.Disabled;
            }

            if (string.Equals(mode, "ShadowOnly", StringComparison.OrdinalIgnoreCase))
            {
                return ConfiguredMode.ShadowOnly;
            }

            return ConfiguredMode.ShadowThenEnable;
        }
    }
}
