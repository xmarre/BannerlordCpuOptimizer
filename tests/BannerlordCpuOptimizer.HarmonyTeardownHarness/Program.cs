using System;
using System.Collections.Generic;
using System.Reflection;
using BannerlordCpuOptimizer.Optimization;
using HarmonyLib;

namespace BannerlordCpuOptimizer.HarmonyTeardownHarness
{
    internal sealed class FakeChoice
    {
        internal FakeChoice(string id)
        {
            Id = id;
        }

        internal string Id { get; }
    }

    internal struct FakePatchState
    {
        internal bool HadExpected;
        internal FakeChoice Expected;
    }

    internal static class FakeBridge
    {
        private static readonly Dictionary<string, FakeChoice> Cache =
            new Dictionary<string, FakeChoice>(StringComparer.Ordinal);

        public static bool Begin(string id, out object cachedResult, out FakePatchState state)
        {
            if (Cache.TryGetValue(id, out FakeChoice choice))
            {
                cachedResult = choice;
                state = new FakePatchState { HadExpected = true, Expected = choice };
                return false;
            }

            cachedResult = null;
            state = default(FakePatchState);
            return true;
        }

        public static void Complete(string id, object result, FakePatchState state)
        {
            var choice = result as FakeChoice;
            if (choice == null)
            {
                throw new InvalidOperationException("The fake target returned an unexpected result.");
            }

            if (state.HadExpected)
            {
                if (!ReferenceEquals(state.Expected, choice))
                {
                    throw new InvalidOperationException("The emitted patch changed the cached reference.");
                }

                return;
            }

            Cache[id] = choice;
        }
    }

    internal static class FakeTarget
    {
        internal static int OriginalCalls;

        internal static FakeChoice GetChoice(string id)
        {
            OriginalCalls++;
            return new FakeChoice(id);
        }
    }

    internal static class FakeProfilerPatch
    {
        public static void Prefix(out long __state)
        {
            __state = DateTime.UtcNow.Ticks;
        }

        public static void Postfix(long __state)
        {
            if (__state == 0L)
            {
                throw new InvalidOperationException("Profiler state was not propagated.");
            }
        }
    }

    internal static class Program
    {
        private const string OptimizationOwner = "test.bannerlordcpuoptimizer.optimization";
        private const string ProfilerOwner = "test.bannerlordcpuoptimizer.profiler";

        private static int Main()
        {
            MethodInfo target = typeof(FakeTarget).GetMethod(
                nameof(FakeTarget.GetChoice),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo beginBridge = typeof(FakeBridge).GetMethod(
                nameof(FakeBridge.Begin),
                BindingFlags.Static | BindingFlags.Public);
            MethodInfo completeBridge = typeof(FakeBridge).GetMethod(
                nameof(FakeBridge.Complete),
                BindingFlags.Static | BindingFlags.Public);
            ExactResultPatchMethods generated = ExactResultPatchFactory.Create(
                typeof(FakeChoice),
                typeof(FakePatchState),
                beginBridge,
                completeBridge);

            Assert(!generated.Prefix.DeclaringType.IsGenericType, "Prefix patch type must be non-generic.");
            Assert(!generated.Postfix.DeclaringType.IsGenericType, "Postfix patch type must be non-generic.");
            Assert(
                generated.Prefix.GetParameters()[1].ParameterType == typeof(FakeChoice).MakeByRefType(),
                "Prefix __result must use the exact by-reference return type.");
            Assert(
                generated.Postfix.GetParameters()[1].ParameterType == typeof(FakeChoice),
                "Postfix __result must use the exact return type.");

            var optimizationHarmony = new Harmony(OptimizationOwner);
            var profilerHarmony = new Harmony(ProfilerOwner);
            try
            {
                optimizationHarmony.Patch(
                    target,
                    prefix: new HarmonyMethod(generated.Prefix),
                    postfix: new HarmonyMethod(generated.Postfix));
                profilerHarmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(FakeProfilerPatch).GetMethod(nameof(FakeProfilerPatch.Prefix))),
                    postfix: new HarmonyMethod(typeof(FakeProfilerPatch).GetMethod(nameof(FakeProfilerPatch.Postfix))));

                FakeChoice first = FakeTarget.GetChoice("alpha");
                FakeChoice second = FakeTarget.GetChoice("alpha");
                Assert(FakeTarget.OriginalCalls == 1, "The second call should be served from the cache.");
                Assert(ReferenceEquals(first, second), "The cache must preserve reference identity.");

                profilerHarmony.UnpatchAll(ProfilerOwner);
                FakeChoice third = FakeTarget.GetChoice("alpha");
                Assert(ReferenceEquals(first, third), "Profiler removal must preserve the optimization patch.");
                Assert(FakeTarget.OriginalCalls == 1, "Profiler removal must not disable the cache.");

                optimizationHarmony.UnpatchAll(OptimizationOwner);
                FakeChoice fourth = FakeTarget.GetChoice("alpha");
                Assert(!ReferenceEquals(first, fourth), "Optimization removal must restore the original method.");
                Assert(FakeTarget.OriginalCalls == 2, "The original method must run after optimization removal.");

                Patches remaining = Harmony.GetPatchInfo(target);
                Assert(
                    remaining == null
                        || (!remaining.Owners.Contains(OptimizationOwner)
                            && !remaining.Owners.Contains(ProfilerOwner)),
                    "No test Harmony owner may remain after teardown.");

                Console.WriteLine("Harmony 2.4.2 exact-result patch teardown harness passed.");
                return 0;
            }
            finally
            {
                profilerHarmony.UnpatchAll(ProfilerOwner);
                optimizationHarmony.UnpatchAll(OptimizationOwner);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
