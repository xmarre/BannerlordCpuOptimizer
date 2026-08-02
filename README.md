# Bannerlord CPU Optimizer

A conservative CPU-usage optimizer for **Mount & Blade II: Bannerlord v1.3.15** and **The Old Realms v1.16**.

The mod targets specific measured TOR campaign-map hotspots. It does **not** reduce CPU usage by slowing campaign simulation, skipping AI work, lowering update frequency, changing randomization, or altering battle behavior.

## Current result

In the tested late-game TOR save, the full optimization set reduced Bannerlord process CPU demand by roughly **45–60%** while campaign time advanced at effectively the same rate.

| Test order | Baseline CPU / campaign hour | Optimized CPU / campaign hour | CPU reduction | Campaign throughput change |
|---|---:|---:|---:|---:|
| Baseline → Optimized | 0.3845 s | 0.2023 s | **47.4%** | **+0.2%** |
| Optimized → Baseline | 0.5449 s | 0.2077 s | **61.9%** | **+0.1%** |

Bannerlord's campaign simulation is not fully deterministic. Loading the same save can produce different AI movement, encounters, visibility checks, pathfinding work, and application-tick counts. That explains why the two baseline runs differed and why the second pair had a larger workload mismatch.

The important result is that the reduction remained large in both run orders. Even after normalizing by application tick, optimized CPU time was about **46–58% lower**. The exact percentage therefore varies with campaign activity, but the overall CPU-demand reduction is clear in this tested workload.

This mainly provides:

- lower CPU load;
- lower power use and temperature;
- more system headroom;
- potentially better high-percentile frame consistency.

It does not currently produce a large increase in maximum-speed campaign throughput, because that workload appears limited by more than the optimized managed-code paths alone.

## Optimizations

### Career-choice lookup cache

Optimizes repeated calls to:

```text
TOR_Core.CharacterDevelopment.TORCareerChoices.GetChoice(string)
```

The same career-choice identifiers are resolved extremely often. The mod stores the exact TOR object reference for the current campaign after validation.

Properties:

- campaign-bound cache;
- per-identifier validation;
- reference-identity comparisons against TOR's original result;
- periodic original-method audits;
- null results are never cached;
- automatic reset on save or campaign replacement;
- immediate fallback on mismatch.

Observed active-hit avoidance is above **99.8%** in the tested saves.

### Map-visibility settlement early exit

Optimizes this path:

```text
TORMapVisibilityModel.GetPartySpottingRange(...)
    -> TORCommon.FindSettlementsAroundPosition(...)
    -> Any()
```

TOR builds a complete nearby-settlement collection even though the caller only needs to know whether one matching settlement exists. The mod replaces that exact operation with an early-exit existence scan.

It does not cache spotting range or any dynamic party state.

Properties:

- exact caller, helper, and predicate validation;
- 512 original-result comparisons before activation;
- periodic audits against TOR's original list-building path;
- automatic disable and fallback on any mismatch.

### Fixed race-ID lookup cache

Optimizes repeated fixed-string calls to:

```text
TaleWorlds.Core.FaceGen.GetRaceOrDefault(string)
```

TOR health and race-classification code repeatedly resolves a small set of race strings. The mod caches only the resulting integer race ID for validated fixed callers.

It never caches:

- characters or heroes;
- equipment or enchantments;
- `ExplainedNumber` values;
- final hit points;
- other dynamic combat state.

In the latest 200-hour optimized run, this path served more than **3.26 million** validated cache hits with zero mismatches.

### Weekly companion allocation reduction

Optimizes two temporary LINQ-style filter/search chains inside:

```text
TORCompanionsCampaignBehavior.WeeklyTick()
```

They are replaced with direct loops that preserve:

- source order;
- predicates;
- first-match behavior;
- random selection and random-number consumption;
- wanderer spawning, disabling, movement, and unregistering;
- save data and campaign side effects.

This targets the measured weekly allocation spike without changing the weekly schedule or gameplay logic.

## Safety

Every optimization is fail-closed.

Activation requires:

- the exact validated `TOR_Core` build;
- the expected module MVID;
- exact declaring types and method signatures;
- exact raw-IL fingerprints for fragile targets;
- the expected patch pattern;
- no unknown Harmony owner on methods whose behavior would be bypassed.

Runtime safety includes:

- shadow comparisons against original results;
- periodic original-path audits;
- campaign-bound state;
- immediate disable on mismatch or exception;
- original-method fallback;
- explicit cleanup when the campaign or module ends.

No optimizer state is written into the save.

## Requirements

- Mount & Blade II: Bannerlord **v1.3.15**
- The Old Realms **v1.16** for the current gameplay optimizations
- Bannerlord.Harmony
- Mod Configuration Menu v5 / `Bannerlord.MBOptionScreen`

The archive does not bundle Harmony, MCM, TaleWorlds, or TOR DLLs.

## Installation

Extract the archive into the Bannerlord installation so the module is located at:

```text
Mount & Blade II Bannerlord\Modules\BannerlordCpuOptimizer
```

Enable the module in the launcher.

## MCM settings

Open:

```text
Options > Mod Options > Bannerlord CPU Optimizer
```

All settings require a full game restart.

### Normal Gameplay

Recommended for ordinary play.

- optimizations enabled;
- benchmark disabled;
- profiler disabled.

### Benchmark - Baseline

- all released optimizations disabled;
- profiler disabled;
- automatic 200-campaign-hour benchmark.

### Benchmark - Optimized

- all released safe optimizations enabled;
- profiler disabled;
- automatic 200-campaign-hour benchmark.

### Focused Profiler

- optimizations enabled;
- managed-method attribution enabled;
- automatic 200-hour diagnostic run;
- intended for hotspot analysis, not direct A/B comparison because profiling adds overhead.

### Custom

Provides individual control over:

- career-choice cache behavior;
- map-visibility optimization;
- race-ID cache;
- weekly companion optimization;
- validation and audit intervals;
- profiler targets and sampling;
- benchmark and diagnostic output.

## Automatic benchmark

Benchmark modes do not start measuring during save loading.

1. Load the save.
2. Select maximum campaign speed when prompted.
3. Keep maximum speed active for 1.5 continuous real seconds.
4. The mod resets all counters and confirms the start.
5. The report completes automatically after exactly 200 campaign hours.

This removes save-loading time, manual fast-forward click timing, and initial stabilization from the result.

A popup, menu, pause, alt-tab interruption, or other event that stops campaign time after the start confirmation can still invalidate the run.

Reports are written to:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

Important report fields:

- `ProcessCpuSecondsPerCampaignHour`
- `WallSecondsPerCampaignHour`
- `CampaignHoursPerRealMinute`
- `ApplicationTicksPerCampaignHour`
- `ProcessCpuMillisecondsPerApplicationTick`
- average, p50, p95, p99, and maximum application-tick intervals
- GC and managed-memory deltas
- optimization runtime states, hits, audits, mismatches, and disable reasons

`ApplicationTicksPerCampaignHour` is used as a workload-equivalence check. The included comparison tool rejects pairs that differ by more than 5%:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

Because Bannerlord campaigns are nondeterministic, stronger testing should alternate run order and compare medians:

```text
Baseline → Optimized → Optimized → Baseline → Baseline → Optimized
```

## Focused profiler

The profiler can attribute managed time and estimated allocations for:

- `TORCharacterStatsModel.MaxHitpoints`;
- `CalculateHitPoints`;
- `CalculateHeroHealth`;
- `CalculateTroopHealth`;
- `TORMapVisibilityModel.GetPartySpottingRange`;
- `TORCommon.FindSettlementsAroundPosition`;
- the spotting-range settlement predicate;
- `TORCompanionsCampaignBehavior.WeeklyTick`;
- wanderer spawn, disable, and unregister methods;
- `TORCareerChoices.GetChoice`.

Outputs include call counts, sampled and estimated elapsed time, allocation estimates, campaign/mission context, and optimization state.

## Current scope and limitations

- Current released gameplay optimizations are TOR-specific and campaign-focused.
- No general battle CPU reduction is claimed.
- No native engine, pathfinding, rendering, physics, or networking code is patched.
- Focused profiling measures managed code and adds overhead.
- Performance depends on save age, party count, installed mods, hardware, frame cap, thermal conditions, and current campaign activity.
- A changed TOR binary intentionally disables incompatible optimizations until it is inspected and validated.

## Build

Requirements:

- .NET 8 SDK
- Python 3
- NuGet access

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
```

The module targets .NET Framework 4.7.2 with pinned Bannerlord v1.3.15 references. CI validates exact-build gates, lifecycle resets, Harmony compatibility, fallback behavior, benchmark rules, teardown, compilation, and package contents.