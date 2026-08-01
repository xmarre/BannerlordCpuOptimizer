# BannerlordCpuOptimizer

Measured managed-code optimization, whole-process benchmarking, and focused profiling for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

## Current status

Milestone 3 contains one active optimization: a campaign-local cache for `TORCareerChoices.GetChoice(string)`, selected from the original broad profile and validated in v0.2.x.

Every campaign starts in shadow mode. The original TOR lookup continues until 256 reference-identical comparisons have passed. Every career-choice ID must pass its own identity comparison before it can be served. One in every 1,024 validated hit candidates is audited through TOR's original method. A mismatch, unexpected null, exception, campaign replacement, game end, or module unload clears the cache and falls back to original TOR behavior.

The validation session recorded 1,117,589 calls, 1,116,160 active hits, all 49 encountered IDs validated, 1,091 audits, zero mismatches, zero null-result changes, and one successful promotion. The subsequent v0.2.1 run also confirmed save/load cache reset, revalidation, repromotion, and clean teardown.

v0.3.0 adds measurement, not another gameplay patch. The new TOR candidates remain observation-only until runtime evidence supports an equivalence-safe optimization.

The implementation contains no AI throttling, simulation throttling, mission cadence changes, native pathfinding/physics patches, transpilers, background TaleWorlds access, or serialized optimizer state.

## Whole-process benchmark

Benchmarking is independent of Harmony method profiling. It records:

- wall-clock duration;
- total Bannerlord process CPU time;
- CPU usage relative to one logical processor and the whole machine;
- campaign hours and campaign-hours-per-real-minute;
- application-tick count;
- average, p50, p95, p99, and maximum frame interval;
- mission count;
- Gen0, Gen1, and Gen2 collection deltas;
- managed-memory start, end, and delta;
- career-choice cache state and counters.

Frame percentiles use a fixed allocation-free 0.1 ms histogram. Whole-process reports are written as JSON and one-row CSV files.

The packaged A/B templates are:

```text
settings.benchmark-baseline.json
settings.benchmark-optimized.json
```

Both keep method profiling disabled. The baseline template disables the career-choice cache; the optimized template enables its normal shadow-then-enable mode. Follow `docs/milestone3-benchmark-procedure.md` and use the same copied save, module list, route, camera state, campaign speed, and duration.

## Focused Milestone 3 profiling

`settings.profiler.json` measures:

- `TORCharacterStatsModel.MaxHitpoints`;
- `TORMapVisibilityModel.GetPartySpottingRange`;
- its settlement predicate;
- `TORCommon.FindSettlementsAroundPosition`;
- `TORCompanionsCampaignBehavior.WeeklyTick`;
- `TORCareerChoices.GetChoice` at a reduced control sampling rate.

Broad TOR and vanilla discovery remain disabled in the template to minimize observer overhead. The benchmark generated during a profiled run is diagnostic only and must not be compared directly with profiling-free A/B reports.

## Project layout

- `src/BannerlordCpuOptimizer`: module source
- `module/BannerlordCpuOptimizer`: distributable module layout
- `docs/milestone2-design.md`: cache invariants
- `docs/milestone3-benchmark-procedure.md`: controlled A/B and attribution procedure
- `tests/BannerlordCpuOptimizer.HarmonyTeardownHarness`: Harmony 2.4.2 teardown integration test
- `tests`: static and structural release gates

## Build

Requirements:

- .NET 8 SDK
- Python 3
- NuGet access

The module targets .NET Framework 4.7.2 and pinned Bannerlord v1.3.15 references. The integration harness runs against Harmony v2.4.2. No game, TOR, Harmony, or test runtime DLL is bundled.

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
```

Expected files:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
artifacts\BannerlordCpuOptimizer-v0.3.0-campaign-measurement.zip
artifacts\SHA256SUMS.txt
```

## Configuration

The packaged configuration is authoritative when present:

```text
Modules\BannerlordCpuOptimizer\ModuleData\BannerlordCpuOptimizer\settings.json
```

The Documents configuration is fallback-only:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\settings.json
```

Normal gameplay uses:

```json
"Profiling": {
  "Enabled": false
},
"Benchmark": {
  "Enabled": false
}
```

The optimization remains active independently through:

```json
"CareerChoiceCacheMode": "ShadowThenEnable"
```

Reports are written under:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

## Validation policy

Gameplay optimization requires the known TOR module MVID, exact declaring type, exact signature, exact raw-IL SHA-256, acceptable Harmony ownership, shadow validation, campaign-bound state, audits, and automatic fallback. Profiler settings never relax an optimization gate.

## Tests

```powershell
python tests\check_profiler_only_invariants.py
python tests\check_source_structure.py
dotnet run --project tests\BannerlordCpuOptimizer.HarmonyTeardownHarness\BannerlordCpuOptimizer.HarmonyTeardownHarness.csproj --configuration Release
```

GitHub Actions runs the Milestone 3 gates, compiles for .NET Framework 4.7.2, executes the Harmony 2.4.2 teardown harness, verifies the release package, and rejects bundled third-party or test runtime assemblies.
