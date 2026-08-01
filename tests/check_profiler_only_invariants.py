#!/usr/bin/env python3
"""Static release gates for Milestone 3 measurement, MCM configuration, and the gated TOR cache."""
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
    cache = read("Optimization/CareerChoiceCache.cs")
    patches = read("Optimization/CareerChoiceCachePatches.cs")
    bridge = read("Optimization/CareerChoicePatchBridge.cs")
    exact_factory = read("Optimization/ExactResultPatchFactory.cs")
    optimization_targets = read("Optimization/KnownOptimizationTargets.cs")
    mcm = read("Configuration/OptimizerMcmSettings.cs")
    runtime = read("Runtime/OptimizerRuntime.cs")
    submodule = read("SubModule.cs")
    campaign_behavior = read("Campaign/ProfilerCampaignBehavior.cs")
    benchmark_session = read("Benchmarking/BenchmarkSession.cs")
    benchmark_writer = read("Benchmarking/BenchmarkReportWriter.cs")
    frame = read("Profiling/FrameProfiler.cs")
    targets = read("Profiling/KnownProfilerTargets.cs")
    profiler_patches = read("Profiling/HarmonyProfilerPatches.cs")
    gate = read("Runtime/PatchGate.cs")
    project = (SRC / "BannerlordCpuOptimizer.csproj").read_text(encoding="utf-8")
    module_xml = (ROOT / "module" / "BannerlordCpuOptimizer" / "SubModule.xml").read_text(encoding="utf-8")
    build = (ROOT / "build.ps1").read_text(encoding="utf-8")
    harness = (ROOT / "tests" / "BannerlordCpuOptimizer.HarmonyTeardownHarness" / "Program.cs").read_text(encoding="utf-8")

    # MCM is the packaged configuration surface. No manual mode/template copying remains.
    assert "AttributeGlobalSettings<OptimizerMcmSettings>" in mcm
    assert 'Id => "BannerlordCpuOptimizer_v1"' in mcm
    assert 'FormatType => "json2"' in mcm
    assert 'NormalMode = "Normal Gameplay"' in mcm
    assert 'BaselineMode = "Benchmark - Baseline"' in mcm
    assert 'OptimizedMode = "Benchmark - Optimized"' in mcm
    assert 'FocusedProfilerMode = "Focused Profiler"' in mcm
    assert 'CustomMode = "Custom"' in mcm
    assert "SettingPropertyDropdown" in mcm
    assert "SettingPropertyBool" in mcm
    assert "SettingPropertyInteger" in mcm
    assert "SettingPropertyFloatingInteger" in mcm
    assert "SettingPropertyText" in mcm
    assert "RequireRestart = false" not in mcm
    assert 'settings.Benchmark.RunLabel = "baseline-cache-disabled"' in mcm
    assert 'settings.General.CareerChoiceCacheMode = "Disabled"' in mcm
    assert 'settings.Benchmark.RunLabel = "optimized-cache-enabled"' in mcm
    assert mcm.count('settings.General.CareerChoiceCacheMode = "ShadowThenEnable"') >= 2
    assert "settings.Profiling.Enabled = CustomProfilerEnabled" in mcm
    assert "settings.Benchmark.Enabled = CustomBenchmarkEnabled" in mcm
    assert "settings.Normalize()" in mcm

    assert 'PackageReference Include="Bannerlord.MCM" Version="5.10.1" IncludeAssets="compile"' in project
    assert '<DependedModule Id="Bannerlord.MBOptionScreen" />' in module_xml
    for legacy_name in (
        "settings.json",
        "settings.profiler.json",
        "settings.benchmark-baseline.json",
        "settings.benchmark-optimized.json",
    ):
        assert not (MODULE_DATA / legacy_name).exists(), f"Manual template still packaged: {legacy_name}"

    on_load = submodule.split("protected override void OnSubModuleLoad()", 1)[1].split("protected override", 1)[0]
    before_root = submodule.split("protected override void OnBeforeInitialModuleScreenSetAsRoot()", 1)[1].split("protected override", 1)[0]
    assert "OptimizerRuntime.Initialize()" not in on_load
    assert "OptimizerRuntime.Initialize()" in before_root
    assert "OptimizerMcmSettings.Instance" in runtime
    assert "BuildRuntimeSettings()" in runtime
    assert "legacy fallback" in runtime
    assert "Settings source: " in runtime

    # Existing exact-gated optimization and teardown contracts remain intact.
    assert "TORCareerChoices" in optimization_targets
    assert "GetChoice" in optimization_targets
    assert "ExactResultPatchFactory.Create" in patches
    assert "typeof(CareerChoicePatchState)" in patches
    assert "TypedPatch<" not in patches
    assert "MakeGenericType" not in patches
    assert "PatchGate.ValidateTarget(target, specification, false" in patches
    assert "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d" in sources

    assert "AssemblyBuilderAccess.Run" in exact_factory
    assert "resultType.MakeByRefType()" in exact_factory
    assert "stateType.MakeByRefType()" in exact_factory
    assert "public struct CareerChoicePatchState" in bridge
    assert "CareerChoiceCache.CompleteCall(id, result, state.Inner)" in bridge

    assert "ReferenceEquals(state.Expected, result)" in cache
    assert "ReferenceEquals(_campaignIdentity, currentCampaign)" in cache
    assert "entry.Validated" in cache
    assert "_activeHitCandidates % _auditEvery" in cache
    assert "DisableLocked" in cache
    assert "Cache.Clear()" in cache

    assert "BenchmarkEnabled" in runtime
    assert "MeasurementEnabled" in runtime
    assert "StartBenchmark" in runtime
    assert "WriteBenchmark" in runtime
    assert "BenchmarkReportWriter.Write" in runtime
    assert "_benchmarkSession?.OnApplicationTick()" in runtime
    assert "_benchmarkSession?.CampaignHourElapsed()" in runtime
    assert "CareerChoiceCache.BeginGameSession(_observedCampaign)" in runtime
    assert "CareerChoiceCache.EndGameSession()" in runtime
    assert "TrackCampaignIdentity" in runtime
    assert runtime.index("currentCampaign != null") < runtime.index("_careerChoiceCachePatches?.Apply()")
    assert "MeasurementEnabled" in submodule
    assert "OnCampaignHourElapsed" in campaign_behavior

    assert "Process.GetCurrentProcess()" in benchmark_session
    assert "TotalProcessorTime" in benchmark_session
    assert "Environment.ProcessorCount" in benchmark_session
    assert "HistogramBinMilliseconds" in benchmark_session
    assert "new long[HistogramBinCount]" in benchmark_session
    assert "Percentile(0.95)" in benchmark_session
    assert "Percentile(0.99)" in benchmark_session
    assert "CampaignHoursPerRealMinute" in benchmark_session
    assert "CareerChoiceCache.Snapshot()" in benchmark_session
    assert "List<double>" not in benchmark_session
    assert "-summary.csv" in benchmark_writer
    assert "DataContractJsonSerializer" in benchmark_writer

    assert "TrackMissionIdentity(GameMission.Current)" in frame
    assert "ReferenceEquals(current, _trackedMission)" in frame
    assert "_missionActive" not in frame
    assert "EnableOptionalContextMetrics" in frame
    assert "AllowUnknownProfilerTargets" not in frame

    assert "ProfileFocusedTargets" in profiler_patches
    assert "KnownProfilerTargets.CreateFocused" in profiler_patches
    assert "TORCharacterStatsModel" in targets
    assert "MaxHitpoints" in targets
    assert "TORMapVisibilityModel" in targets
    assert "GetPartySpottingRange" in targets
    assert "TORCompanionsCampaignBehavior" in targets
    assert "WeeklyTick" in targets
    assert "34208fbc8958a6c869968edd8ac7e0018a2691f120cfb0893958d989ff971876" in targets

    assert "ValidateTarget" in gate
    assert "MethodFingerprint.ComputeSha256" in gate
    assert "HarmonyMethod transpiler" not in sources
    assert ".Transpiler" not in sources
    assert not re.search(r"\b(Task|Thread|ThreadPool|Timer)\s*\.", sources), "No background workers"
    assert "UnpatchAll" in sources
    assert "GetTotalWage" not in patches
    assert "AddCareerSpecificWagePerks" not in patches

    assert "HarmonyTeardownHarness" in build
    assert "profilerHarmony.UnpatchAll(ProfilerOwner)" in harness
    assert "optimizationHarmony.UnpatchAll(OptimizationOwner)" in harness
    assert "No test Harmony owner may remain after teardown" in harness

    print("Milestone 3 MCM, benchmark, attribution, cache, and Harmony teardown gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
