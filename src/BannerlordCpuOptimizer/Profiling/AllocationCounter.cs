using System;
using System.Reflection;

namespace BannerlordCpuOptimizer.Profiling
{
    internal static class AllocationCounter
    {
        private static readonly Func<long> ReadAllocatedBytes = Resolve();
        internal static bool IsAvailable => ReadAllocatedBytes != null;
        internal static long Read()
        {
            try { return ReadAllocatedBytes == null ? -1L : ReadAllocatedBytes(); }
            catch { return -1L; }
        }
        private static Func<long> Resolve()
        {
            try
            {
                MethodInfo method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                return method == null ? null : (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            }
            catch { return null; }
        }
    }
}
