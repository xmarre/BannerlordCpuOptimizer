# BannerlordCpuOptimizer

Measured managed-code optimization, whole-process benchmarking, and focused profiling for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

## v0.4.1 status

v0.4.1 retains the four released TOR campaign optimizations from v0.4.0 and fixes benchmark startup contamination:

1. a campaign-bound reference cache for `TORCareerChoices.GetChoice(string)`;
2. an early-exit settlement existence check inside `TORMapVisibilityModel.GetPartySpottingRange`;
3. an audited cache for fixed `FaceGen.GetRaceOrDefault(string)` lookups used by TOR health and race classification paths;
4. exact-order manual loops replacing two temporary LINQ iterator chains in `TORCompanionsCampaignBehavior.WeeklyTick`.

Benchmark and focused-profile counters no longer begin when the save starts loading. The mod waits until maximum campaign speed has remained active continuously for 1.5 real seconds, then resets every measurement and optimization counter at one common boundary.

Final hit-point values are never cached. The weekly companion schedule, randomization, spawn logic, movement, ordering, and save data are unchanged. There are no native engine patches, background TaleWorlds access, AI throttles, or simulation-cadence changes.

## Safety model

Every released optimization is fail-closed:

- exact `TOR_Core` MVID;
- exact declaring type, method signature, and raw-IL SHA-256;
- refusal when an unknown Harmony owner modifies the target;
- reference shadowing before cache activation where applicable;
- periodic original-method audits;
- immediate fallback on any mismatch;
- no serialized optimizer state;
- explicit teardown.

Map visibility validates the exact caller, settlement helper, and predicate before replacing only the adjacent `FindSettlementsAroundPosition(...).Any()` pair. During shadow mode it returns TOR's original result. After activation it periodically audits the early-exit result against the original list-building path.

The fixed race cache validates each encountered race ID repeatedly before serving it and continues periodic original lookups. It caches only the integer race-ID resolution, never a character, hero, equipment state, `ExplainedNumber`, or final hit-point result.

The weekly companion patch recognizes exactly two `WhereQ` terminal chains in the fingerprinted `WeeklyTick` body. It preserves source order and predicates and does not replace any randomization or gameplay method.

## Requirements

- Mount & Blade II: Bannerlord v1.3.15
- Bannerlord.Harmony
- Mod Configuration Menu v5 / `Bannerlord.MBOptionScreen`
- The Old Realms v1.16 for TOR-specific targets

MCM and all game/TOR dependencies are external. No third-party runtime DLL is bundled.

## MCM configuration

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

The `Operating Mode` selector provides:

- `Normal Gameplay`: selected validated optimizations enabled; benchmark and profiler disabled;
- `Benchmark - Baseline`: every released optimizer patch disabled, profiler disabled, automatic 200-hour benchmark enabled;
- `Benchmark - Optimized`: every released safe optimizer patch enabled, profiler disabled, automatic 200-hour benchmark enabled;
- `Focused Profiler`: released optimizations plus focused attribution and a diagnostic benchmark;
- `Custom`: detailed control of optimization gates, validation thresholds, profiler targets, sampling, and reporting.

All settings are restart-gated. Save the MCM change, close Bannerlord completely, and relaunch it.

The TOR Campaign section exposes:

- map visibility early exit;
- visibility shadow-comparison and audit intervals;
- fixed race lookup cache;
- race lookup shadow-comparison and audit intervals;
- weekly companion LINQ elision.

No manual settings-file copying is required. Legacy JSON loading remains only as an emergency fallback if MCM cannot be resolved.

## Automatic whole-process benchmark

After a benchmark save loads, the run is armed but no counters have started. An in-game message asks the user to select maximum campaign speed. The start gate accepts Bannerlord's supported maximum fast-forward modes only when campaign time is unlocked and no mission is active.

Maximum speed must remain continuous for 1.5 real seconds. Any loss of maximum speed resets the pre-start timer. When the gate opens:

- benchmark and profiler sessions begin;
- process CPU, wall time, application ticks, GC, and managed-memory baselines are captured;
- career-choice, map-visibility, fixed-race, and weekly-companion states reset;
- an in-game message confirms that the 200-hour run started.

This excludes save loading, manual fast-forward selection, and initial stabilization from the measurement.

Baseline, Optimized, and Focused Profiler sessions finish automatically after exactly 200 campaign-hour callbacks recorded after that boundary. The mod writes reports immediately and displays an in-game completion message.

Reports contain:

- start condition, time-control mode, and stability interval;
- process CPU and wall time;
- CPU and wall seconds per campaign hour;
- campaign hours per real minute;
- application ticks per second and per campaign hour;
- process CPU and wall milliseconds per application tick;
- application-tick average, p50, p95, p99, and exact maximum interval;
- mission count;
- Gen0, Gen1, and Gen2 collection deltas;
- managed-memory start, end, and delta;
- career-choice cache counters;
- map visibility, fixed-race, and weekly-companion optimization states.

For a controlled A/B test, use the same untouched starting save and identical module list, camera, frame limiter, game settings, and background load. Load Baseline, select maximum speed when prompted, and wait for the start and completion messages. Restore the starting save and repeat in Optimized mode.

Primary comparison fields:

```text
ProcessCpuSecondsPerCampaignHour
WallSecondsPerCampaignHour
CampaignHoursPerRealMinute
ApplicationTicksPerCampaignHour
ProcessCpuMillisecondsPerApplicationTick
WallMillisecondsPerApplicationTick
AverageFrameMilliseconds
P95FrameMilliseconds
P99FrameMilliseconds
```

Compare reports with:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

The comparison utility refuses mismatched start modes or stability intervals and rejects application-tick workloads that differ by more than 5% per campaign hour.

## Focused profiler

v0.4.1 attributes:

- `TORCharacterStatsModel.MaxHitpoints`;
- `CalculateHitPoints`;
- `CalculateHeroHealth`;
- `CalculateTroopHealth`;
- `TORMapVisibilityModel.GetPartySpottingRange`;
- its settlement predicate and original settlement helper;
- `TORCompanionsCampaignBehavior.WeeklyTick`;
- `SpawnWanderer`;
- `DisableWanderer`;
- `UnregisterWandererObject`;
- `TORCareerChoices.GetChoice` as a reduced-rate control.

A profiled run includes instrumentation overhead and must not be compared directly with profiling-free A/B reports.

Reports are written under:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

## Build and validation

Requirements:

- .NET 8 SDK
- Python 3
- NuGet access

The module targets .NET Framework 4.7.2 with pinned Bannerlord v1.3.15 references. CI runs:

- Milestone 4 static safety gates;
- lifecycle and stable measurement-boundary checks;
- structural checks;
- benchmark-comparison tests;
- automatic-completion checks;
- a zero-warning .NET Framework build;
- the Harmony 2.4.2 exact-result teardown harness;
- release-package and forbidden-DLL verification.

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
```

Expected outputs:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
artifacts\BannerlordCpuOptimizer-v0.4.1-stable-fast-forward-start.zip
artifacts\SHA256SUMS.txt
```
