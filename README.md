# Bannerlord CPU Optimizer

Measured, fail-closed managed-code optimization, whole-process benchmarking, and focused profiling for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

The project does not reduce CPU use by slowing the simulation, skipping AI work, lowering update frequency, or changing battle behavior. Every released optimization targets a specific measured TOR managed-code hotspot and refuses to activate unless the installed binary and participating methods match the validated build exactly.

## Current release: v0.4.1

v0.4.1 contains four released TOR campaign optimizations and a controlled automatic benchmark system:

1. campaign-bound caching for `TORCareerChoices.GetChoice(string)`;
2. an early-exit settlement-existence check inside `TORMapVisibilityModel.GetPartySpottingRange`;
3. an audited cache for fixed `FaceGen.GetRaceOrDefault(string)` lookups used by TOR health and race-classification paths;
4. allocation-reducing direct loops for two temporary LINQ chains in `TORCompanionsCampaignBehavior.WeeklyTick`.

v0.4.1 also removes save-load and manual fast-forward timing from measurements. Benchmark and focused-profile counters start only after maximum campaign speed has remained stable continuously for 1.5 real seconds.

### Current scope

- The released optimizations target TOR campaign-map code.
- There is no released native engine, pathfinding, physics, rendering, or battle-simulation optimization.
- The focused profiler can measure battle-related managed methods, but profiling is diagnostic and adds overhead.
- The mod can load without TOR because `TOR_Core` is an optional module dependency, but the current gameplay optimizations have nothing to patch when TOR is absent.
- Bannerlord v1.3.15 and the supplied TOR v1.16 build are the validated targets.

## Measured performance findings

The current evidence comes from a late-game TOR save on a system with 28 logical processors. Results describe that save, module set, hardware, and test procedure; they are not universal performance guarantees.

### Summary

Across both run orders, enabling all released optimizations repeatedly produced a large reduction in Bannerlord process CPU time while campaign-time throughput remained essentially unchanged.

The supported interpretation is:

> In the supplied late-game TOR campaign, the current optimization set materially reduced process CPU demand—observed in the approximate 45–60% range across the cleanest all-on comparisons—while advancing campaign time at effectively the same rate.

This is primarily a CPU-load, power, temperature, and system-headroom optimization in this workload. It is not currently demonstrated as a large campaign-simulation-speed or FPS increase.

### Benchmark results

| Build and test | Baseline CPU seconds per campaign hour | Optimized CPU seconds per campaign hour | CPU change | Campaign-hours-per-minute change | Application-tick workload difference | Interpretation |
|---|---:|---:|---:|---:|---:|---|
| v0.3.2, career-choice cache only | 0.49336 | 0.47719 | **−3.28%** | **+2.10%** | −1.04% | Clean 200-hour profiling-free pair; validates the original cache independently. |
| v0.4.0, all optimizations, baseline → optimized | 0.38445 | 0.20227 | **−47.39%** | **+0.21%** | −1.94% | Closely matched all-on pair; predates the v0.4.1 stable-start gate but manual click timing was too small to explain the CPU result. |
| v0.4.1, all optimizations, optimized → baseline | 0.54492 | 0.20766 | **−61.89%** | **+0.07%** | −9.37% | Confirms the direction in reverse order, but exceeds the 5% workload-equivalence threshold and is supporting evidence rather than an exact final percentage. |

Additional frame/tick observations:

- v0.4.0 matched pair: process CPU per application tick fell from about 5.821 ms to 3.123 ms, a **46.35% reduction**.
- v0.4.1 reverse-order pair: process CPU per application tick fell from 5.583 ms to 2.347 ms, a **57.95% reduction**.
- v0.4.0 matched pair: p99 application-tick interval improved from 28.7 ms to 20.2 ms.
- v0.4.1 reverse-order pair: p99 improved from 24.0 ms to 19.5 ms.
- These interval values are `MBSubModuleBase.OnApplicationTick` callback intervals, not guaranteed GPU-present or display-frame times.

### Latest v0.4.1 optimized validation state

The reverse-order optimized run completed exactly 200 campaign hours with all four optimization groups active:

| Optimization | Calls or operations | Optimized hits/work | Mismatches | Disabled reason |
|---|---:|---:|---:|---|
| Career-choice cache | 541,296 calls | 540,491 active hits | 0 | none |
| Map-visibility early exit | 62,123 calls | 61,611 active results | 0 | none |
| Fixed-race lookup cache | 3,263,481 calls | 3,261,945 active hits | 0 | none |
| Weekly companion loops | 56 filtered-list calls, 119 first-match calls | 1,209 items visited | n/a | none |

The fixed-race path is the highest-frequency new optimization in this save, bypassing more than 3.26 million repeated fixed-string race lookups during one 200-hour run.

### Excluded contaminated result

One earlier v0.4.0 optimized run displayed a `deserter wants to join the party` popup that did not occur in the baseline run. Campaign time stopped while wall time, process CPU time, and application ticks continued. That pair is excluded from performance conclusions.

v0.4.1 removes startup contamination, but it cannot automatically remove interruptions that happen after measurement begins. A popup, menu, alt-tab pause, or other campaign-time interruption after the start message invalidates that run and the report should be discarded.

### Bannerlord nondeterminism

Bannerlord campaign runs are not deterministic enough for two loads of the same save to execute exactly identical work. Small timing differences can alter:

- AI party movement and route selection;
- encounters and battles;
- settlement decisions;
- visibility checks;
- random campaign events;
- the timing and count of application ticks needed to advance 200 campaign hours.

For that reason, the project does not treat one unusually high or low result as a universal percentage. The recommended final methodology is alternating order with medians:

```text
Baseline → Optimized → Optimized → Baseline → Baseline → Optimized
```

Use the median of the three baseline reports and the median of the three optimized reports. The included comparison tool rejects pairs whose application ticks per campaign hour differ by more than 5%.

## Why these targets were selected

A focused mixed campaign-and-battle profile identified the following managed hotspots:

| Target | Calls | Estimated elapsed time | Estimated allocated bytes | Finding |
|---|---:|---:|---:|---|
| `TORCharacterStatsModel.MaxHitpoints` | 648,455 | 1.769 s | 386.2 MB | Largest measured allocation target; final values remain uncached because inputs are dynamic. |
| `TORMapVisibilityModel.GetPartySpottingRange` | 54,724 | 1.333 s | 271.9 MB | High-frequency campaign path with repeated nearby-settlement list construction. |
| `TORCommon.FindSettlementsAroundPosition` | 109,937 | 1.146 s | 108.7 MB | Child of the spotting-range path; elapsed time overlaps the parent and must not be added to it. |
| `TORCompanionsCampaignBehavior.WeeklyTick` | 1 | 245.4 ms | 66.7 MB | Infrequent but large periodic hitch and allocation burst. |
| `TORCareerChoices.GetChoice` | 1,070,800 | 278 ms sampled estimate | 127 KB | Extremely frequent stable lookup already suitable for a campaign-bound reference cache. |

Profiler time and allocation values are sampled/estimated managed-code attribution. Parent and child method times can overlap.

## Released optimizations in detail

### 1. TOR career-choice lookup cache

Target:

```text
TOR_Core.CharacterDevelopment.TORCareerChoices.GetChoice(string)
```

TOR repeatedly resolves the same career-choice identifiers. The optimizer stores the exact returned TOR object reference for the current campaign.

Safety and behavior:

- starts each campaign in shadow mode;
- requires 256 reference-identical original comparisons before global promotion;
- validates every encountered identifier independently before serving it;
- new identifiers remain original-only until individually validated;
- periodically audits validated hits through TOR's original method;
- never caches null results;
- binds all entries to the exact current `Campaign` instance;
- clears and revalidates after campaign replacement, save loading, game end, or teardown;
- disables immediately on reference mismatch, unexpected null, exception, changed binary, or foreign Harmony ownership.

Measured cache behavior has consistently exceeded 99.8% active-hit avoidance after promotion with zero observed mismatches.

### 2. TOR map-visibility settlement early exit

Target path:

```text
TORMapVisibilityModel.GetPartySpottingRange(...)
    -> TORCommon.FindSettlementsAroundPosition(...)
    -> Any()
```

The original path builds a complete nearby-settlement list even though the caller only needs to know whether at least one matching settlement exists. The optimizer replaces that exact adjacent list-building/`Any()` pair with a direct early-exit existence scan.

Safety and behavior:

- validates the exact spotting-range caller, settlement helper, and generated predicate;
- refuses activation when any participating method has an unknown Harmony owner;
- performs 512 original-result shadow comparisons before activation;
- periodically compares the early-exit result against TOR's original list-building path;
- disables and returns to the original path on any mismatch or exception;
- does not cache spotting range, party position, weather, perks, explanations, or final `ExplainedNumber` results;
- preserves the original predicate and boolean outcome.

### 3. Fixed race-ID lookup cache

Target source:

```text
TaleWorlds.Core.FaceGen.GetRaceOrDefault(string)
```

TOR health and race-classification methods repeatedly resolve a small set of fixed race strings. The optimizer caches only the resulting integer race ID for those fixed strings in exact validated callers.

Safety and behavior:

- validates the source lookup and every patched caller;
- refuses activation if the source or a caller has an unknown Harmony patch owner;
- validates each race string repeatedly before serving it;
- periodically audits active entries through the original lookup;
- disables immediately on shadow or audit mismatch;
- currently observed six active race-string entries in the supplied save;
- never caches a character, hero, equipment state, enchantment, `ExplainedNumber`, hit-point result, or other dynamic combat state.

Final hit points are deliberately not cached.

### 4. Weekly companion LINQ elision

Target:

```text
TOR_Core.CampaignMechanics.Companions.TORCompanionsCampaignBehavior.WeeklyTick()
```

The validated TOR method contains two recognized `WhereQ` terminal chains that create temporary iterator/list state. The optimizer replaces only those exact chains with direct loops.

Preserved semantics:

- weekly execution schedule;
- source enumeration and source order;
- predicates;
- immediate null-failure behavior;
- first-match behavior;
- random selection and random-number consumption;
- wanderer spawning and disabling;
- unregistering;
- settlement movement;
- save data and campaign side effects.

The patch requires the expected caller fingerprint and exactly two recognized patterns. A changed method body is rejected rather than patched approximately.

## Safety model

Every released gameplay optimization is fail-closed.

### Exact-build gates

- exact assembly identity;
- exact known `TOR_Core` module MVID;
- exact declaring type and method signature;
- exact raw-IL SHA-256 for every fragile participating method;
- expected patch-pattern count;
- refusal on unknown or changed builds.

### Harmony compatibility gates

The optimizer checks participating methods for Harmony ownership. An optimization refuses activation when another unknown owner modifies a method whose behavior would be bypassed or replaced.

### Runtime equivalence validation

Depending on the target, the optimizer uses:

- reference-identity shadow comparisons;
- per-key validation;
- original-result shadow comparisons;
- periodic original-path audits;
- immediate disable and original fallback on mismatch.

### Lifecycle boundaries

Campaign-sensitive caches and counters reset when:

- a campaign starts;
- `Campaign.Current` changes inside the same process;
- another save replaces the current campaign;
- the game ends;
- the module shuts down.

No optimizer state is serialized into the save.

### Explicit non-goals

The released mod does not:

- throttle campaign AI;
- skip campaign events;
- reduce simulation cadence;
- lower agent update frequency;
- patch native pathfinding, physics, rendering, or networking;
- run TaleWorlds APIs from background threads;
- create background workers or timers;
- alter randomization;
- cache final dynamic combat values;
- bundle Harmony, MCM, TaleWorlds, or TOR DLLs.

## Requirements and installation

Required:

- Mount & Blade II: Bannerlord v1.3.15;
- Bannerlord.Harmony;
- Mod Configuration Menu v5 / `Bannerlord.MBOptionScreen`;
- The Old Realms v1.16 for the released TOR optimizations.

Install by extracting the release archive into the Bannerlord installation so the module appears at:

```text
Mount & Blade II Bannerlord\Modules\BannerlordCpuOptimizer
```

The package contains the module descriptor and optimizer DLL only. All framework, game, and TOR dependencies remain external.

## MCM configuration

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

All settings are restart-gated. After changing the operating mode or optimization configuration, save the MCM setting, close Bannerlord completely, and relaunch it.

### Operating modes

#### Normal Gameplay

Recommended for ordinary play.

- benchmark disabled;
- profiler disabled;
- selected validated optimizations enabled;
- only normal validation/audit overhead remains.

#### Benchmark - Baseline

Controlled all-off comparison mode.

- profiler disabled;
- every released gameplay optimization disabled;
- fixed report label: `baseline-all-optimizations-disabled`;
- automatic stable-start gate;
- automatic completion at exactly 200 campaign hours.

#### Benchmark - Optimized

Controlled all-on comparison mode.

- profiler disabled;
- every released safe optimization enabled;
- fixed report label: `optimized-all-safe-optimizations-enabled`;
- automatic stable-start gate;
- automatic completion at exactly 200 campaign hours.

#### Focused Profiler

Diagnostic attribution mode.

- released optimizations enabled;
- focused managed-method profiling enabled;
- diagnostic whole-process benchmark enabled;
- stable-start gate shared by profiler and benchmark;
- automatic completion at 200 campaign hours;
- profiler overhead makes its CPU results unsuitable for comparison with profiling-free Baseline/Optimized reports.

#### Custom

Advanced mode exposing individual controls for:

- career-choice cache mode and validation thresholds;
- map-visibility early exit;
- map shadow comparisons and audit cadence;
- fixed-race lookup cache;
- race shadow comparisons and audit cadence;
- weekly companion LINQ elision;
- profiler target groups and sampling;
- benchmark/report behavior;
- diagnostics.

Legacy JSON loading remains only as an emergency fallback when MCM cannot be resolved. No manual configuration template is packaged.

## Automatic benchmark system

### Stable measurement start

When a benchmark or focused-profile save loads, the run is armed but no measurement session exists yet.

1. An in-game message asks the user to select maximum campaign speed.
2. A supported maximum fast-forward mode must remain active continuously for 1.5 real seconds.
3. Pausing, leaving maximum speed, entering a mission, or a campaign time-control lock resets the pre-start timer.
4. When the gate opens, benchmark, profiler, process CPU, wall time, application ticks, GC, managed memory, and all optimization counters reset together.
5. A second in-game message confirms that the 200-hour measurement has started.

This excludes save loading, the manual speed-selection delay, and initial stabilization from the report.

### Automatic completion

The run completes after exactly 200 campaign-hour callbacks recorded after the start boundary.

```text
200 campaign hours = 8 campaign days and 8 campaign hours
```

Battles do not advance the campaign-hour counter. Reports are written immediately and an in-game message confirms completion.

### Report location

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

Logs are written under the sibling `logs` directory.

### Whole-process report fields

Primary fields:

- `ProcessCpuSecondsPerCampaignHour`: total Bannerlord process CPU time normalized by simulated campaign hours;
- `WallSecondsPerCampaignHour`: real elapsed time normalized by campaign hours;
- `CampaignHoursPerRealMinute`: maximum-speed simulation throughput;
- `ApplicationTicksPerCampaignHour`: workload-equivalence control metric;
- `ProcessCpuMillisecondsPerApplicationTick`: process CPU normalized by application callbacks;
- `WallMillisecondsPerApplicationTick`: wall time normalized by application callbacks;
- `ApplicationTicksPerSecond`;
- average, p50, p95, p99, and exact maximum application-tick intervals;
- mission count;
- Gen0, Gen1, and Gen2 collection deltas;
- managed heap start, end, and delta;
- start condition, time-control mode, and stability duration;
- runtime state and counters for every released optimization.

Interpretation limits:

- process CPU includes every Bannerlord thread, not only the campaign main thread;
- application-tick intervals are callback intervals, not guaranteed display-frame times;
- managed heap delta is live-memory change, not total allocated bytes;
- CPU percentage of the whole machine depends on logical-processor count and is less portable than CPU seconds per campaign hour;
- campaign nondeterminism can change workload even from the same starting save.

### Comparison utility

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

The comparator rejects incompatible reports when:

- profiling is enabled;
- process CPU measurement is unavailable;
- stable-start metadata is missing;
- start conditions differ;
- start time-control modes differ;
- stability intervals differ;
- application ticks per campaign hour differ by more than 5%.

### Recommended controlled procedure

Use the same untouched starting save for every run. Keep unchanged:

- module list and load order;
- frame limiter and VSync;
- resolution and graphics settings;
- campaign camera position and zoom;
- foreground/alt-tab state;
- process priority;
- background applications;
- campaign speed after the start prompt.

Discard any run interrupted after the start confirmation by a popup, menu, pause, unexpected interaction, or other event that stops campaign time.

For stronger evidence, alternate order and use medians rather than relying on one pair.

## Focused profiler

Focused Profiler measures the current high-value target set:

- `TORCharacterStatsModel.MaxHitpoints`;
- `CalculateHitPoints`;
- `CalculateHeroHealth`;
- `CalculateTroopHealth`;
- `TORMapVisibilityModel.GetPartySpottingRange`;
- its settlement predicate;
- `TORCommon.FindSettlementsAroundPosition`;
- `TORCompanionsCampaignBehavior.WeeklyTick`;
- `SpawnWanderer`;
- `DisableWanderer`;
- `UnregisterWandererObject`;
- `TORCareerChoices.GetChoice` as a reduced-rate control.

Method reports include:

- exact call count;
- sampled call count and sampling interval;
- sampled and estimated total elapsed time;
- sampled average and maximum elapsed time;
- calls per application frame, campaign hour, and mission;
- sampled and estimated managed allocations where runtime support is available;
- disabled/unsupported target state.

Context reports can include:

- campaign speed;
- rendered/application frame count;
- party and settlement counts;
- living and total agents when in a mission;
- missiles;
- battle type;
- GC collection counts;
- managed memory;
- other discoverable TOR context.

A representative focused run should travel around the campaign map, include one battle, return to the campaign map, and continue until automatic completion.

## Reports and logging

The project writes human-readable CSV and machine-readable JSON reports. Depending on the selected mode, outputs include:

- whole-process benchmark JSON;
- one-row benchmark summary CSV;
- focused method CSV;
- focused context CSV;
- focused JSON report;
- optimization validation counters;
- lifecycle and teardown logs.

Reports explicitly identify:

- optimizer version;
- session ID;
- run label;
- completion reason;
- profiling state;
- campaign-hour count;
- start-gate metadata;
- optimization runtime states;
- mismatches and disable reasons.

## Build and validation

Build requirements:

- .NET 8 SDK;
- Python 3;
- NuGet access.

The module itself targets .NET Framework 4.7.2 with pinned Bannerlord v1.3.15 reference assemblies.

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

CI and release validation cover:

- exact-build and IL-fingerprint safety gates;
- MCM preset isolation;
- campaign replacement and lifecycle resets;
- stable measurement-boundary ordering;
- foreign Harmony-owner refusal;
- map fallback and failed-unpatch behavior;
- weekly-loop semantic preservation;
- benchmark comparison rejection rules;
- automatic 200-hour completion;
- source-structure invariants;
- zero-warning .NET Framework compilation;
- Harmony 2.4.2 patch teardown and original-method restoration;
- package-content verification;
- rejection of bundled third-party/game/TOR/test DLLs.

## Project layout

```text
src/BannerlordCpuOptimizer
    Module source, runtime, optimization, benchmark, profiler, and configuration code.

module/BannerlordCpuOptimizer
    Distributable Bannerlord module layout.

docs
    Design notes, profiling procedures, benchmark procedure, and safety documentation.

tests
    Static safety gates, comparison tests, source invariants, and Harmony teardown harness.

tools
    Assembly inspection and benchmark comparison utilities.
```

## Known limitations

- Current released gameplay optimizations are TOR-specific and campaign-focused.
- No general battle CPU reduction is claimed.
- No native engine CPU time is directly attributed by the managed profiler.
- A single A/B pair cannot eliminate Bannerlord campaign nondeterminism.
- Popup or menu interruptions after measurement begins are not automatically subtracted.
- Focused profiling changes performance and is diagnostic only.
- Exact-build safety means a TOR update can intentionally disable an optimization until the changed binary is inspected and revalidated.
- Performance gains vary with save age, party count, installed mods, hardware, frame cap, thermal state, and campaign activity.

## Release principle

No optimization is released because it merely looks plausible in decompiled code. The project follows this sequence:

1. profile a real workload;
2. identify a measured managed hotspot;
3. design the narrowest behavior-preserving replacement;
4. gate it to the exact validated binary and methods;
5. shadow or audit it against the original where possible;
6. benchmark all-off versus all-on;
7. disable automatically on uncertainty or mismatch.
