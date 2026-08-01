# v0.4.0 benchmark and focused-profile procedure

Configuration is entirely through MCM. No JSON copying, manual date counting, or stopwatch timing is required.

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

All settings are restart-gated. After changing the operating mode, save, close Bannerlord completely, and relaunch it.

## Automatic completion

Benchmark and Focused Profiler sessions finish automatically after exactly 200 campaign-hour callbacks. Reports are written immediately and an in-game message confirms completion. Battles do not advance the counter.

## Controlled A/B run

Use the same untouched starting save for both runs. Keep the module list, game settings, resolution, frame limiter, camera position, campaign speed, and background applications unchanged.

### Baseline

1. Select `Benchmark - Baseline`.
2. Save MCM settings and restart Bannerlord.
3. Load the copied starting save.
4. Use the chosen reproducible campaign speed and camera state.
5. Wait for the automatic completion message.
6. Exit normally and preserve the log and both benchmark files.

Baseline disables profiling and every released optimizer patch. Its fixed label is:

```text
baseline-all-optimizations-disabled
```

### Optimized

1. Restore the same untouched starting save.
2. Select `Benchmark - Optimized`.
3. Save MCM settings and restart Bannerlord.
4. Repeat the identical campaign conditions.
5. Wait for the automatic completion message.
6. Exit normally and preserve the log and both benchmark files.

Optimized disables profiling and enables every released safe optimization. Its fixed label is:

```text
optimized-all-safe-optimizations-enabled
```

The optimized report must show:

- career-choice cache promotion and zero mismatches;
- active map-visibility calls with zero mismatches;
- active fixed-race entries with zero mismatches;
- no disabled reason for an optimization intended to activate;
- exactly 200 campaign hours.

## Primary comparison fields

- `ProcessCpuSecondsPerCampaignHour`
- `WallSecondsPerCampaignHour`
- `CampaignHoursPerRealMinute`
- `AverageFrameMilliseconds`
- `P95FrameMilliseconds`
- `P99FrameMilliseconds`
- `MaximumFrameMilliseconds`
- GC collection deltas

Compare reports with:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

## Focused attribution run

1. Select `Focused Profiler`.
2. Save MCM settings and restart Bannerlord.
3. Load the test save.
4. Travel on the campaign map and include one representative battle before completion.
5. Continue until the automatic 200-hour message appears.
6. Exit normally and preserve the profile, benchmark, and log files.

Focused Profiler measures the hit-point parent and child paths, map visibility and its reference helper, the weekly companion parent and major child methods, and the career-choice control. Its process-CPU figures include profiler overhead and are diagnostic only.

## Normal gameplay

After testing, select `Normal Gameplay`, save, and restart. This enables the selected validated optimizations without benchmark or profiler overhead.

## Interpretation limits

Whole-process CPU includes all Bannerlord threads and does not isolate native engine subsystems. Frame percentiles use a fixed allocation-free 0.1 ms histogram; values at or above 250 ms share the last percentile bin, while the exact maximum remains separate.
