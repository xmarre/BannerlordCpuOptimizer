using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace BannerlordCpuOptimizer.Optimization
{
    public static class MapVisibilityEarlyExit
    {
        private static readonly object Sync = new object();
        private static Func<Vec2, float, Func<Settlement, bool>, MBList<Settlement>> _original;
        private static bool _configured;
        private static bool _enabled;
        private static bool _active;
        private static int _requiredComparisons = 512;
        private static int _auditEvery = 2048;
        private static long _calls;
        private static long _shadowComparisons;
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
                _active = false;
                _requiredComparisons = Math.Max(1, requiredComparisons);
                _auditEvery = Math.Max(1, auditEvery);
                _calls = 0;
                _shadowComparisons = 0;
                _activeHits = 0;
                _audits = 0;
                _mismatches = 0;
                _disabledReason = null;
            }
        }

        internal static void BindOriginal(Func<Vec2, float, Func<Settlement, bool>, MBList<Settlement>> original)
        {
            lock (Sync)
            {
                _original = original;
            }
        }

        internal static void ResetSession()
        {
            lock (Sync)
            {
                _enabled = _configured;
                _active = false;
                _calls = 0;
                _shadowComparisons = 0;
                _activeHits = 0;
                _audits = 0;
                _mismatches = 0;
                _disabledReason = null;
            }
        }

        internal static void Clear(bool releaseOriginal)
        {
            lock (Sync)
            {
                _configured = false;
                _enabled = false;
                _active = false;
                if (releaseOriginal)
                {
                    _original = null;
                }
            }
        }

        public static bool AnySettlementAroundPosition(Vec2 position, float radius, Func<Settlement, bool> predicate)
        {
            Func<Vec2, float, Func<Settlement, bool>, MBList<Settlement>> original = _original;
            if (!_enabled || original == null)
            {
                return original != null
                    ? original(position, radius, predicate).Count > 0
                    : ComputeEarlyExit(position, radius, predicate);
            }

            long call = Interlocked.Increment(ref _calls);
            bool optimized = ComputeEarlyExit(position, radius, predicate);

            if (!_active)
            {
                bool reference = original(position, radius, predicate).Count > 0;
                Interlocked.Increment(ref _shadowComparisons);
                if (reference != optimized)
                {
                    Disable("shadow mismatch");
                    return reference;
                }

                if (Interlocked.Read(ref _shadowComparisons) >= _requiredComparisons)
                {
                    lock (Sync)
                    {
                        if (_enabled && !_active && _shadowComparisons >= _requiredComparisons)
                        {
                            _active = true;
                        }
                    }
                }

                return reference;
            }

            if (call % _auditEvery == 0)
            {
                bool reference = original(position, radius, predicate).Count > 0;
                Interlocked.Increment(ref _audits);
                if (reference != optimized)
                {
                    Disable("audit mismatch");
                    return reference;
                }
            }

            Interlocked.Increment(ref _activeHits);
            return optimized;
        }

        private static bool ComputeEarlyExit(Vec2 position, float radius, Func<Settlement, bool> predicate)
        {
            var locator = Settlement.StartFindingLocatablesAroundPosition(position, radius);
            Settlement settlement = Settlement.FindNextLocatable(ref locator);
            while (settlement != null)
            {
                if (predicate == null || predicate(settlement))
                {
                    return true;
                }

                settlement = Settlement.FindNextLocatable(ref locator);
            }

            return false;
        }

        private static void Disable(string reason)
        {
            lock (Sync)
            {
                _enabled = false;
                _active = false;
                _disabledReason = reason;
                Interlocked.Increment(ref _mismatches);
            }
        }

        internal static string Describe()
        {
            lock (Sync)
            {
                return "configured=" + _configured
                    + " enabled=" + _enabled
                    + " active=" + _active
                    + " calls=" + _calls
                    + " shadow=" + _shadowComparisons
                    + " activeHits=" + _activeHits
                    + " audits=" + _audits
                    + " mismatches=" + _mismatches
                    + " disabledReason=" + (_disabledReason ?? "<none>");
            }
        }
    }
}
