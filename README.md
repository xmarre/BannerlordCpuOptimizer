# BannerlordCpuOptimizer

Measured managed-code optimization, whole-process benchmarking, and focused profiling for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

## Current status

Milestone 3 contains one active optimization: a campaign-local cache for `TORCareerChoices.GetChoice(string)`, selected from the original broad profile and validated in v0.2.x.

Every campaign starts in shadow mode. The original TOR lookup continues until 256 reference-identical comparisons have passed. Every career-choice ID must pass its own identity comparison before it can be served. One in every 1,024 validated hit candidates is audited through TOR's original method. A mismatch, unexpected null, exception, campaign replacement, game end, or module unload clears the cache and falls back to original TOR behavior.

The validation session recorded 1,117,589 calls, 1,116,160 active hits, all 49 encountered IDs validated, 1,091 audits, zero mismatches, zero null-result changes, and one successful promotion. The subsequent v0.2.1 run also confirmed save/load cache reset, revalidation, repromotion, and clean teardown.

v0.3.0 added whole-process campaign measurement and focused attribution. v0.3.1 replaces every packaged JSON mode template with a proper MCM menu. No manual configuration-file copying is required.

The implementation contains no AI throttling, simulation throttling, mission cadence changes, native pathfinding or physics patches, transpilers, background TaleWorlds access, or serialized optimizer state.

## Requirements

- Mount & Blade II: Bannerlord v1.3.15
- Bannerlord.Harmony
- Mod Configuration Menu v5 / `Bannerlord.MBOptionScreen`
- The Old Realms v1.16 for TOR-specific optimization and profiling targets

MCM is a normal module dependency. Its DLLs are not bundled with this mod.

## MCM configuration

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

The `Operating Mode` selector provides:

- `Normal Gameplay`: validated optimization enabled, benchmark and profiler disabled;
- `Benchmark - Baseline`: profiler disabled, benchmark enabled, career-choice cache disabled, fixed baseline label;
- `Benchmark - Optimized`: profiler disabled, benchmark enabled, validated cache enabled, fixed optimized label;
- `Focused Profiler`: focused method profiling and its diagnostic benchmark enabled;
- `Custom`: uses the detailed optimization, benchmark, profiler, sampling, report, and diagnostic switches.

All settings are restart-gated. Save the MCM change, close Bannerlord completely, and relaunch it. This is deliberate: benchmark and profiler modes must begin from a clean process without leftover Harmony or measurement state.

MCM global settings are authoritative. The old packaged files are no longer included:

```text
settings.json
settings.profiler.json
settings.benchmark-baseline.json
settings.benchmark-optimized.json
```

The legacy JSON loader remains only as an emergency fallback if MCM settings cannot be resolved.

## Whole-process benchmark

Benchmarking is independent of Harmony method profiling. It records:

- wall-clock duration;
- total Bannerlord process CPU time;
- CPU usage relative to one logical processor and the whole machine;
- CPU and wall seconds normalized per campaign hour;
- campaign hours and campaign-hours-per-real-minute;
- application-tick count;
- average, p50, p95, p99, and maximum frame interval;
- mission count;
- Gen0, Gen1, and Gen2 collection deltas;
- managed-memory start, end, and delta;
- career-choice cache state and counters.

Frame percentiles use a fixed allocation-free 0.1 ms histogram. Whole-process reports are written as JSON and one-row CSV files.

For controlled A/B testing, choose `Benchmark - Baseline`, restart and run the route, then restore the same starting save, choose `Benchmark - Optimized`, restart and repeat the same route. Follow `docs/milestone3-benchmark-procedure.md`.

## Focused Milestone 3 profiling

Choose `Focused Profiler` in MCM. It measures:

- `TORCharacterStatsModel.MaxHitpoints`;
- `TORMapVisibilityModel.GetPartySpottingRange`;
- its settlement predicate;
- `TORCommon.FindSettlementsAroundPosition`;
- `TORCompanionsCampaignBehavior.WeeklyTick`;
- `TORCareerChoices.GetChoice` at a reduced control sampling rate.

The benchmark generated during a profiled run is diagnostic only and must not be compared directly with profiling-free A/B reports.

Reports are written under:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

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

The module targets .NET Framework 4.7.2 and pinned Bannerlord v1.3.15 references. It compiles against Bannerlord.MCM 5.10.1 with compile-only assets and depends on the player's standalone MCM module at runtime. The integration harness runs against Harmony v2.4.2. No game, TOR, Harmony, MCM, or test runtime DLL is bundled.

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
```

Expected files:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
artifacts\BannerlordCpuOptimizer-v0.3.1-mcm.zip
artifacts\SHA256SUMS.txt
```

## Validation policy

Gameplay optimization requires the known TOR module MVID, exact declaring type, exact signature, exact raw-IL SHA-256, acceptable Harmony ownership, shadow validation, campaign-bound state, audits, and automatic fallback. Profiler and MCM settings never relax an optimization gate.

## Tests

```powershell
python tests\check_profiler_only_invariants.py
python tests\check_source_structure.py
python tests\check_benchmark_compare.py
dotnet run --project tests\BannerlordCpuOptimizer.HarmonyTeardownHarness\BannerlordCpuOptimizer.HarmonyTeardownHarness.csproj --configuration Release
```

GitHub Actions runs the Milestone 3 MCM gates, compiles for .NET Framework 4.7.2, executes the Harmony 2.4.2 teardown harness, verifies the release package, and rejects manual settings templates and bundled third-party or test runtime assemblies.
