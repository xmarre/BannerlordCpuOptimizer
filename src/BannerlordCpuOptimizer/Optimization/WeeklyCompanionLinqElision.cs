using System;
using System.Collections.Generic;
using System.Threading;

namespace BannerlordCpuOptimizer.Optimization
{
    public static class WeeklyCompanionLinqElision
    {
        private static long _filterCalls;
        private static long _firstCalls;
        private static long _itemsVisited;

        public static List<T> FilterToList<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            Interlocked.Increment(ref _filterCalls);
            var result = new List<T>();
            if (source == null)
            {
                return result;
            }

            foreach (T item in source)
            {
                Interlocked.Increment(ref _itemsVisited);
                if (predicate == null || predicate(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static T FirstOrDefaultMatch<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            Interlocked.Increment(ref _firstCalls);
            if (source == null)
            {
                return default(T);
            }

            foreach (T item in source)
            {
                Interlocked.Increment(ref _itemsVisited);
                if (predicate == null || predicate(item))
                {
                    return item;
                }
            }

            return default(T);
        }

        internal static void Reset()
        {
            Interlocked.Exchange(ref _filterCalls, 0);
            Interlocked.Exchange(ref _firstCalls, 0);
            Interlocked.Exchange(ref _itemsVisited, 0);
        }

        internal static string Describe()
        {
            return "filterCalls=" + Interlocked.Read(ref _filterCalls)
                + " firstCalls=" + Interlocked.Read(ref _firstCalls)
                + " itemsVisited=" + Interlocked.Read(ref _itemsVisited);
        }
    }
}
