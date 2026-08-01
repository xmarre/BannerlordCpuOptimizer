#!/usr/bin/env python3
"""Static release gate for the narrowly scoped Milestone 2 optimization."""
from __future__ import annotations

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "BannerlordCpuOptimizer"
MODULE_DATA = ROOT / "module" / "BannerlordCpuOptimizer" / "ModuleData" / "BannerlordCpuOptimizer"


def read(relative: str) -> str:
    return (SRC / relative).read_text(encoding="utf-8")


def main() -> int:
    sources = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))
    settings = json.loads((MODULE_DATA / "settings.json").read_text(encoding="utf-8"))
    profiler = json.loads((MODULE_DATA / "settings.profiler.json").read_text(encoding="utf-8"))
    cache = read("Optimization/CareerChoiceCache.cs")
    patches = read("Optimization/CareerChoiceCachePatches.cs")
    bridge = read("Optimization/CareerChoicePatchBridge.cs")
    exact_factory = read("Optimization/ExactResultPatchFactory.cs")
    optimization_targets = read("Optimization/KnownOptimizationTargets.cs")
    runtime = read("Runtime/OptimizerRuntime.cs")
    frame = read("Profiling/FrameProfiler.cs")
    targets = read("Profiling/KnownProfilerTargets.cs")
    profiler_patches = read("Profiling/HarmonyProfilerPatches.cs")
    gate = read("Runtime/PatchGate.cs")
    build = (ROOT / "build.ps1").read_text(encoding="utf-8")
    harness = (ROOT / "tests" / "BannerlordCpuOptimizer.HarmonyTeardownHarness" / "Program.cs").read_text(encoding="utf-8")

    assert settings["Profiling"]["Enabled"] is False
    assert settings["General"]["ExperimentalNativePatches"] is False
    assert settings["General"]["CareerChoiceCacheMode"] == "ShadowThenEnable"
    assert settings["General"]["CareerChoiceShadowComparisons"] >= 256
    assert settings["General"]["CareerChoiceAuditEvery"] >= 1

    assert profiler["Profiling"]["Enabled"] is True
    assert profiler["Profiling"]["ProfileFocusedTargets"] is True
    assert profiler["Profiling"]["ProfileTorCampaignHandlers"] is False
    assert profiler["Profiling"]["ProfileTorMissionHandlers"] is False
    assert profiler["Profiling"]["ProfileTorModels"] is False
    assert profiler["Diagnostics"]["RuntimeOverlay"] is False

    assert "TORCareerChoices" in optimization_targets
    assert "GetChoice" in optimization_targets
    assert "ExactResultPatchFactory.Create" in patches
    assert "typeof(CareerChoicePatchState)" in patches
    assert "TypedPatch<" not in patches
    assert "MakeGenericType" not in patches
    assert "PatchGate.ValidateTarget(target, specification, false" in patches
    assert "d43d63915c133164674d16f246e8d55afd0e165d322fd6ca2b3d5a9e6956d56d" in sources

    assert "AssemblyBuilderAccess.Run" in exact_factory
    assert "TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed" in exact_factory
    assert "resultType.MakeByRefType()" in exact_factory
    assert "stateType.MakeByRefType()" in exact_factory
    assert 'DefineParameter(2, ParameterAttributes.None, "__result")' in exact_factory
    assert 'DefineParameter(3, ParameterAttributes.Out, "__state")' in exact_factory
    assert "OpCodes.Castclass, resultType" in exact_factory
    assert "ContainsGenericParameters" in exact_factory
    assert "public struct CareerChoicePatchState" in bridge
    assert "internal CareerChoiceCallState Inner" in bridge
    assert "out CareerChoicePatchState state" in bridge
    assert "CareerChoiceCache.CompleteCall(id, result, state.Inner)" in bridge

    assert "ReferenceEquals(state.Expected, result)" in cache
    assert "ReferenceEquals(_campaignIdentity, currentCampaign)" in cache
    assert "entry.Validated" in cache
    assert "New ids remain shadow-only until individually validated" in cache
    assert "result == null" in cache
    assert "Cache[id] = new CacheEntry" in cache
    assert "TryPromoteLocked" in cache
    assert "_shadowComparisons < _requiredShadowComparisons" in cache
    assert "_activeHitCandidates % _auditEvery" in cache
    assert "DisableLocked" in cache
    assert "Cache.Clear()" in cache

    assert "CareerChoiceCache.BeginGameSession(_observedCampaign)" in runtime
    assert "CareerChoiceCache.EndGameSession()" in runtime
    assert "TrackCampaignIdentity" in runtime
    assert "ReferenceEquals(_observedCampaign, currentCampaign)" in runtime
    assert runtime.index("currentCampaign != null") < runtime.index("_careerChoiceCachePatches?.Apply()")
    assert "HarmonyProfilerPatches.HarmonyId" in patches
    assert "another Harmony owner modifies" in patches

    assert "TrackMissionIdentity(GameMission.Current)" in frame
    assert "ReferenceEquals(current, _trackedMission)" in frame
    assert "_trackedMission = null" in frame
    assert "_missionActive" not in frame

    assert "EnableOptionalContextMetrics" in frame
    assert "AllowUnknownProfilerTargets" not in frame
    assert "ProfileFocusedTargets" in profiler_patches
    assert "KnownProfilerTargets.CreateFocused" in profiler_patches
    assert "625660a4834ee1ff607d04d167920656c598be39c61cc01564711205731e816e" in targets
    assert "34208fbc8958a6c869968edd8ac7e0018a2691f120cfb0893958d989ff971876" in targets

    assert "ValidateTarget" in gate
    assert "allowUnknownModule" in gate
    assert "MethodFingerprint.ComputeSha256" in gate

    assert "HarmonyMethod transpiler" not in sources
    assert ".Transpiler" not in sources
    assert not re.search(r"\b(Task|Thread|ThreadPool|Timer)\s*\.", sources), "No background workers"
    assert "Profiler state is intentionally never serialized" in sources
    assert "UnpatchAll" in sources
    assert "GetTotalWage" not in patches
    assert "AddCareerSpecificWagePerks" not in patches

    assert "HarmonyTeardownHarness" in build
    assert "profilerHarmony.UnpatchAll(ProfilerOwner)" in harness
    assert "optimizationHarmony.UnpatchAll(OptimizationOwner)" in harness
    assert "Prefix __result must use the exact by-reference return type" in harness
    assert "No test Harmony owner may remain after teardown" in harness

    print("Milestone 2 scoped-optimization and Harmony teardown invariant checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
