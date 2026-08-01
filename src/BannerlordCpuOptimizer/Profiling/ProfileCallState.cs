namespace BannerlordCpuOptimizer.Profiling
{
    internal struct ProfileCallState
    {
        internal MethodProfile Profile;
        internal long StartTimestamp;
        internal long AllocationBefore;
        internal bool Sampled;
    }
}
