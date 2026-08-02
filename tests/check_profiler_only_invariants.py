#!/usr/bin/env python3
"""Static release gates for the complete Milestone 4 TOR campaign pass."""
from __future__ import annotations

import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "BannerlordCpuOptimizer"
MODULE_DATA = ROOT / "module" / "BannerlordCpuOptimizer" / "ModuleData" / "BannerlordCpuOptimizer"


def read(relative: str) -> str:
    return (SRC / relative).read_text(encoding="utf-8")


def main() -> int:
    sources = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))
    mcm = read("Configuration/OptimizerMcmSettings.cs")
    settings = read("Configuration/OptimizerSettings.cs")
    runtime = read("Runtime/OptimizerRuntime.cs")
    cache = read("Optimization/CareerChoiceCache.cs")
    coordinator = read("Optimization/CareerChoiceCachePatches.cs")
    targets = read("Optimization/KnownOptimizationTargets.cs")
    profiler_targets = read("Profiling/KnownProfilerTargets.cs")
    map_engine = read("Optimization/MapVisibilityEarlyExit.cs")
    map_patch = read("Optimization/MapVisibilityOptimizationPatches.cs")
    race_cache = read("Optimization/RaceLookupCache.cs")
    race_patch = read("Optimization/RaceLookupOptimizationPatches.cs")
    weekly_engine = read("Optimization/WeeklyCompanionLinqElision.cs")
    weekly_patch = read("Optimization/WeeklyCompanionOptimizationPatches.cs")
    benchmark = read("Benchmarking/BenchmarkSession.cs")
    profile = read("Profiling/ProfileSession.cs")
    project = read("BannerlordCpuOptimizer.csproj")
    module_xml = (ROOT / "module" / "BannerlordCpuOptimizer" / "SubModule.xml").read_text(encoding="utf-8")
    build = (ROOT / "build.ps1").read_text(encoding="utf-8")
    harness = (ROOT / "tests" / "BannerlordCpuOptimizer.HarmonyTeardownHarness" / "Program.cs").read_text(encoding="utf-8")

    # MCM is the only normal configuration surface and benchmark modes are isolated.
    assert "AttributeGlobalSettings<OptimizerMcmSettings>" in mcm
    for mode in ("Normal Gameplay", "Benchmark - Baseline", "Benchmark - Optimized", "Focused Profiler", "Custom"):
        assert mode in mcm
    for option in (
        "Map Visibility Early Exit",
        "Fixed Race Lookup Cache",
        "Weekly Companion LINQ Elision",
        "Visibility Shadow Comparisons",
        "Race Lookup Shadow Comparisons",
    ):
        assert option in mcm
    assert 'settings.Benchmark.RunLabel = "baseline-all-optimizations-disabled"' in mcm
    baseline = mcm.split("case BaselineMode:", 1)[1].split("case OptimizedMode:", 1)[0]
    assert 'settings.General.CareerChoiceCacheMode = "Disabled"' in baseline
    assert "settings.General.TorCampaignOptimizations = false" in baseline
    assert "settings.General.MapVisibilityEarlyExit = false" in baseline
    assert "settings.General.RaceLookupCache = false" in baseline
    assert "settings.General.WeeklyCompanionLinqElision = false" in baseline
    optimized = mcm.split("case OptimizedMode:", 1)[1].split("case FocusedProfilerMode:", 1)[0]
    assert 'settings.General.CareerChoiceCacheMode = "ShadowThenEnable"' in optimized
    assert "settings.General.TorCampaignOptimizations = true" in optimized
    assert "settings.General.MapVisibilityEarlyExit = true" in optimized
    assert "settings.General.RaceLookupCache = true" in optimized
    assert "settings.General.WeeklyCompanionLinqElision = true" in optimized
    assert "RequireRestart = false" not in mcm
    assert "MapVisibilityShadowComparisons" in settings
    assert "RaceLookupAuditEvery" in settings

    for legacy_name in (
        "settings.json",
        "settings.profiler.json",
        "settings.benchmark-baseline.json",
        "settings.benchmark-optimized.json",
    ):
        assert not (MODULE_DATA / legacy_name).exists(), f"Manual template still packaged: {legacy_name}"

    # Existing career-choice cache remains exact-result, campaign-bound, audited, and fail-closed.
    assert "ExactResultPatchFactory.Create" in coordinator
    assert "PatchGate.ValidateTarget(target, specification, false" in coordinator
    assert "ReferenceEquals(state.Expected, result)" in cache
    assert "ReferenceEquals(_campaignIdentity, currentCampaign)" in cache
    assert "_activeHitCandidates % _auditEvery" in cache
    assert "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d" in targets

    # Map visibility: exact three-way fingerprint gate, one surgical call-pair replacement,
    # reference shadowing before activation, periodic audits, and immediate disable on mismatch.
    for fingerprint in (
        "da46117a540b2ad0e28dad4e7076fd0311ce43e97f47912cd9c8f343a59b29e2",
        "34208fbc8958a6c869968edd8ac7e0018a2691f120cfb0893958d989ff971876",
        "94729e2790e1421c91f7ec9754941025be6417c6c3babcc4a15506dd9cdd4bf6",
    ):
        assert fingerprint in targets
    assert "Expected exactly one FindSettlementsAroundPosition/Any pair" in map_patch
    assert "PatchGate.ValidateTarget(caller" in map_patch
    assert "PatchGate.ValidateTarget(find" in map_patch
    assert "PatchGate.ValidateTarget(predicate" in map_patch
    assert "original(position, radius, predicate).Count > 0" in map_engine
    assert "_shadowComparisons" in map_engine and "_auditEvery" in map_engine
    assert "Disable(\"shadow mismatch\")" in map_engine
    assert "Disable(\"audit mismatch\")" in map_engine
    assert "return true;" in map_engine and "FindNextLocatable" in map_engine

    # Hit-point work: only fixed race-ID resolution is cached; final ExplainedNumber values are never cached.
    for fingerprint in (
        "e9665f13d87afe89385473a3ff8e04773e066be7885f2ac720ac81f0647045fa",
        "208000ad9273813a38faeb288bed39921418c40daa5dd1a4829d32300f190efe",
        "14688937fbbf28368426ef1e52c819c509e0a2873bfe388da985ac097e948ca9",
        "860f73d08da228bd999ef147b334c3f7f5a043c1cdf61c54f5c410760b20e091",
        "85273fd90df12fc28e649d88f605f32bd3434b1facce550e0f50f602b73c9aaf",
        "448666f090a8cc9a860907d3f977c706756952ac5776732b9ae61999c8cd98c1",
    ):
        assert fingerprint in targets
    assert "FaceGen.GetRaceOrDefault" in race_cache
    assert "entry.HitCandidates % _auditEvery" in race_cache
    assert "DisableLocked(\"shadow mismatch" in race_cache
    assert "DisableLocked(\"audit mismatch" in race_cache
    assert "Expected " in race_patch and "race lookup call(s)" in race_patch
    assert "Dictionary<CharacterObject" not in sources
    assert "Dictionary<Hero" not in sources
    assert "ExplainedNumber>" not in race_cache

    # WeeklyTick: exact caller fingerprint, exactly two recognized WhereQ terminal chains,
    # no schedule/randomness/spawn replacement, and no per-item atomic operation.
    assert "8fba5850abb733b65e720a74c1b5989e479c5ac242a8587907af8ec720c35c90" in targets
    assert "Expected exactly two WeeklyTick WhereQ terminal chains" in weekly_patch
    assert "FilterToList" in weekly_patch and "FirstOrDefaultMatch" in weekly_patch
    assert "Random" not in weekly_engine
    filter_loop = weekly_engine.split("foreach (T item in source)", 1)[1].split("return result", 1)[0]
    assert "Interlocked.Increment(ref _itemsVisited)" not in filter_loop
    assert "Interlocked.Add(ref _itemsVisited, visited)" in weekly_engine
    assert "SpawnWanderer" in profiler_targets
    assert "DisableWanderer" in profiler_targets
    assert "UnregisterWandererObject" in profiler_targets

    # Reports expose all optimization states; benchmark auto-completion remains intact.
    assert "MapVisibilityOptimization = MapVisibilityEarlyExit.Describe()" in benchmark
    assert "RaceLookupOptimization = RaceLookupCache.Describe()" in benchmark
    assert "WeeklyCompanionOptimization = WeeklyCompanionLinqElision.Describe()" in benchmark
    assert "MapVisibilityOptimization = MapVisibilityEarlyExit.Describe()" in profile
    assert "AutomaticBenchmarkTargetHours = 200" in runtime
    assert "automatic-target-" in runtime
    assert "MeasurementStartGate" in runtime
    assert "maximum-campaign-speed-stable" in runtime

    # Release/version/teardown constraints.
    assert "<Version>0.4.1</Version>" in project
    assert '<Version value="v0.4.1" />' in module_xml
    assert not re.search(r"\b(Task|Thread|ThreadPool|Timer)\s*\.", sources), "No background workers"
    assert "UnpatchAll" in coordinator and "UnpatchAll" in map_patch and "UnpatchAll" in race_patch and "UnpatchAll" in weekly_patch
    assert "HarmonyTeardownHarness" in build
    assert "No test Harmony owner may remain after teardown" in harness

    print("Milestone 4 exact-gated campaign optimization, stable-start, MCM, benchmark, profiler, and teardown gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
