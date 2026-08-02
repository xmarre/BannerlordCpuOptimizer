#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "BannerlordCpuOptimizer"

runtime = (SRC / "Runtime" / "OptimizerRuntime.cs").read_text(encoding="utf-8")
map_engine = (SRC / "Optimization" / "MapVisibilityEarlyExit.cs").read_text(encoding="utf-8")
map_patch = (SRC / "Optimization" / "MapVisibilityOptimizationPatches.cs").read_text(encoding="utf-8")
race_patch = (SRC / "Optimization" / "RaceLookupOptimizationPatches.cs").read_text(encoding="utf-8")
weekly = (SRC / "Optimization" / "WeeklyCompanionLinqElision.cs").read_text(encoding="utf-8")

# Initial campaign start, in-process Campaign.Current replacement, and the exact
# stable-speed measurement boundary must reset every campaign-sensitive state set.
assert runtime.count("ResetCampaignOptimizationValidation();") == 3
reset_body = runtime.split("private static void ResetCampaignOptimizationValidation()", 1)[1].split("private static void StartSession()", 1)[0]
assert "MapVisibilityEarlyExit.ResetSession();" in reset_body
assert "RaceLookupCache.ResetSession();" in reset_body
assert "WeeklyCompanionLinqElision.Reset();" in reset_body
measurement_start = runtime.split("private static void TryStartArmedMeasurement", 1)[1].split("private static void ArmMeasurementStart", 1)[0]
assert measurement_start.index("CareerChoiceCache.BeginGameSession(currentCampaign);") < measurement_start.index("ResetCampaignOptimizationValidation();")
assert measurement_start.index("ResetCampaignOptimizationValidation();") < measurement_start.index("StartSession();")
assert measurement_start.index("ResetCampaignOptimizationValidation();") < measurement_start.index("StartBenchmark(startMode);")

# The caller, reference helper, and predicate all participate in the map replacement,
# so every one must be free of unknown Harmony owners.
assert "!ValidateForeignPatches(caller)" in map_patch
assert "!ValidateForeignPatches(find)" in map_patch
assert "!ValidateForeignPatches(predicate)" in map_patch
assert "participating method" in map_patch

# Active race-cache hits bypass FaceGen.GetRaceOrDefault itself, so both the source
# lookup and every exact caller must be free of unknown Harmony owners.
assert "MethodInfo sourceLookup = AccessTools.Method" in race_patch
assert "!HasOnlyAllowedOwners(sourceLookup)" in race_patch
assert "foreign Harmony owner on source lookup" in race_patch
assert "!HasOnlyAllowedOwners(target)" in race_patch

# A successful map unpatch releases the original delegate. A failed unpatch leaves the
# replacement disabled but retains the original delegate as a behavior-preserving fallback.
assert "bool unpatched = false;" in map_patch
assert "unpatched = true;" in map_patch
assert "MapVisibilityEarlyExit.Clear(unpatched);" in map_patch
clear_body = map_engine.split("internal static void Clear(bool releaseOriginal)", 1)[1].split("public static bool AnySettlementAroundPosition", 1)[0]
assert "if (releaseOriginal)" in clear_body
assert "_original = null;" in clear_body
fallback_body = map_engine.split("if (!_enabled || original == null)", 1)[1].split("long call", 1)[0]
assert "original(position, radius, predicate).Count > 0" in fallback_body

# Weekly materialization must preserve immediate null failures, deferred source
# enumeration, source order, and predicate behavior without per-item atomics.
assert weekly.count("throw new ArgumentNullException(nameof(source));") == 2
assert weekly.count("throw new ArgumentNullException(nameof(predicate));") == 2
assert "predicate == null ||" not in weekly
assert "var result = new List<T>();" in weekly
assert "ICollection<T>" not in weekly
loop_body = weekly.split("foreach (T item in source)", 1)[1].split("Interlocked.Add(ref _itemsVisited, visited);", 1)[0]
assert "Interlocked." not in loop_body
assert "if (predicate(item))" in loop_body

print("Milestone 4 lifecycle, measurement-boundary, foreign-owner, fallback, and weekly semantic gates passed.")
