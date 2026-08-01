# Milestone 3 benchmark procedure

Milestone 3 separates whole-process A/B measurement from method profiling. Configuration is performed entirely through MCM. No JSON file copying, editing, or manual campaign-hour timing is required.

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

Every setting is restart-gated. After changing the operating mode, save the MCM change, close Bannerlord completely, and relaunch it. This guarantees that each benchmark begins with a clean process and that no previous profiler or Harmony state contaminates the run.

## Automatic completion

Benchmark and Focused Profiler sessions finish automatically after exactly 200 campaign-hour callbacks. When the target is reached, the active benchmark report is written immediately. Focused Profiler mode also writes its active profile report. An in-game message confirms completion.

You do not need to watch the date, count days, or exit at an exact moment. After the completion message appears, exit normally and preserve the generated files. Two hundred campaign hours equal eight in-game days and eight hours, but that conversion is informational only.

## Controlled campaign A/B run

Use one copied save as the starting point for both runs. Keep the complete module list, game settings, process priority, resolution, frame limiter, camera position, campaign speed, and background applications unchanged.

### Baseline

1. Install v0.3.2 cleanly.
2. In the Bannerlord CPU Optimizer MCM page, set `Operating Mode` to `Benchmark - Baseline`.
3. Save the MCM settings and restart Bannerlord completely.
4. Load the copied starting save.
5. Leave the campaign camera and speed in the chosen reproducible state.
6. Let the campaign run until the optimizer displays its 200-hour completion message.
7. Exit normally.
8. Preserve the optimizer log and both `BannerlordCpuOptimizer-Benchmark-...baseline-cache-disabled` files.

This mode automatically disables method profiling and disables the career-choice cache. The comparison-safe report label is assigned automatically.

### Optimized

1. Restore the same untouched starting save.
2. In MCM, set `Operating Mode` to `Benchmark - Optimized`.
3. Save the MCM settings and restart Bannerlord completely.
4. Repeat the identical route until the optimizer displays its completion message.
5. Exit normally.
6. Preserve the optimizer log and both `BannerlordCpuOptimizer-Benchmark-...optimized-cache-enabled` files.

This mode automatically disables method profiling and enables the validated shadow-then-enable career-choice cache. The comparison-safe report label is assigned automatically.

Run baseline and optimized at least three times each when practical. Alternate the order between repetitions to reduce temperature, background-service, and cache-warmth bias.

## Primary comparison fields

- `ProcessCpuSecondsPerCampaignHour`
- `WallSecondsPerCampaignHour`
- `CampaignHoursPerRealMinute`
- `ProcessCpuPercentOfOneLogicalCore`
- `ProcessCpuPercentOfWholeMachine`
- `AverageFrameMilliseconds`
- `P95FrameMilliseconds`
- `P99FrameMilliseconds`
- `MaximumFrameMilliseconds`
- `Gen0CollectionsDelta`, `Gen1CollectionsDelta`, and `Gen2CollectionsDelta`

A valid optimized run must show cache promotion, active hits, zero mismatches, no disabled reason, and 200 recorded campaign hours.

Compare a baseline and optimized JSON report with:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

The script refuses reports created with method profiling enabled and prints percentage improvements with the correct lower-is-better or higher-is-better direction for each metric.

## Focused attribution run

1. In MCM, set `Operating Mode` to `Focused Profiler`.
2. Optionally set `Custom Run Label`.
3. Save the MCM settings and restart Bannerlord completely.
4. Load the test save.
5. Include one representative battle during the run.
6. Continue until the optimizer displays its 200-hour completion message.
7. Exit normally and preserve the profiler, benchmark, and log files.

The 200-hour duration includes at least one weekly companion tick. Focused Profiler automatically enables the focused target set, the benchmark diagnostic companion, and the validated cache. Its process-CPU values include profiler overhead and must not be compared directly with the profiling-free A/B runs.

The focused profiler targets are:

- `TORCharacterStatsModel.MaxHitpoints`
- `TORMapVisibilityModel.GetPartySpottingRange`
- the visibility settlement predicate
- `TORCommon.FindSettlementsAroundPosition`
- `TORCompanionsCampaignBehavior.WeeklyTick`
- `TORCareerChoices.GetChoice` as a low-rate control

## Normal gameplay

After testing, set `Operating Mode` to `Normal Gameplay`, save, and restart Bannerlord. This keeps the validated optimization active while disabling benchmark and profiler overhead.

## Custom mode

`Custom` uses the detailed switches in the Optimization, Profiler, and Diagnostics groups. The preset modes override settings that would invalidate their purpose—for example, Baseline always disables the cache and both benchmark modes always disable method profiling.

## Interpretation limits

The benchmark records whole-process CPU time across all Bannerlord threads. It does not isolate native engine subsystems. Frame percentiles use a fixed allocation-free 0.1 ms histogram; frame times of 250 ms or more share the final histogram bin, while the exact maximum remains separately recorded.
