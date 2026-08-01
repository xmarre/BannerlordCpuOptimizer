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
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var result = new List<T>();
            long visited = 0;
            foreach (T item in source)
            {
                visited++;
                if (predicate(item))
                {
                    result.Add(item);
                }
            }

            Interlocked.Add(ref _itemsVisited, visited);
            return result;
        }

        public static T FirstOrDefaultMatch<T>(IEnumerable<T> source, Func<T, bool> predicate)
        {
            Interlocked.Increment(ref _firstCalls);
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            long visited = 0;
            foreach (T item in source)
            {
                visited++;
                if (predicate(item))
                {
                    Interlocked.Add(ref _itemsVisited, visited);
                    return item;
                }
            }

            Interlocked.Add(ref _itemsVisited, visited);
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
