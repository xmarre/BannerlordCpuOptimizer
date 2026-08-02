# v0.4.1 benchmark and focused-profile procedure

Configuration is entirely through MCM. No JSON copying, manual date counting, stopwatch timing, or precisely timed fast-forward click is required.

Open:

```text
Main Menu or Pause Menu > Options > Mod Options > Bannerlord CPU Optimizer
```

All settings are restart-gated. After changing the operating mode, save, close Bannerlord completely, and relaunch it.

## Automatic start and completion

After the save loads, measurement is armed but no benchmark or profiler counter has started yet. An in-game message asks you to select maximum campaign speed.

The measurement starts automatically only after a supported maximum fast-forward mode remains active continuously for 1.5 real seconds. Losing maximum speed, entering a mission, or encountering a campaign time-control lock resets that pre-start stability timer. When the boundary is reached, all benchmark, profiler, and optimization counters reset together and a second message confirms that the 200-hour run has begun.

This excludes save loading, manual speed selection, and initial stabilization from the report.

Benchmark and Focused Profiler sessions finish automatically after exactly 200 campaign-hour callbacks recorded after that boundary. Reports are written immediately and an in-game message confirms completion. Battles do not advance the counter.

## Controlled A/B run

Use the same untouched starting save for both runs. Keep the module list, game settings, resolution, frame limiter, camera position, background applications, and foreground state unchanged.

### Baseline

1. Select `Benchmark - Baseline`.
2. Save MCM settings and restart Bannerlord.
3. Load the copied starting save.
4. When prompted, select maximum campaign speed.
5. Do not treat the run as started until the optimizer displays its stable-speed start message.
6. Wait for the automatic completion message.
7. Exit normally and preserve the log and both benchmark files.

Baseline disables profiling and every released optimizer patch. Its fixed label is:

```text
baseline-all-optimizations-disabled
```

### Optimized

1. Restore the same untouched starting save.
2. Select `Benchmark - Optimized`.
3. Save MCM settings and restart Bannerlord.
4. Load the save and select maximum campaign speed when prompted.
5. Wait for the stable-speed start message, then leave the campaign conditions unchanged.
6. Wait for the automatic completion message.
7. Exit normally and preserve the log and both benchmark files.

Optimized disables profiling and enables every released safe optimization. Its fixed label is:

```text
optimized-all-safe-optimizations-enabled
```

The optimized report must show:

- `StartCondition` equal to `maximum-campaign-speed-stable`;
- the same `StartTimeControlMode` and `StartStabilitySeconds` as the baseline;
- career-choice cache promotion and zero mismatches;
- active map-visibility calls with zero mismatches;
- active fixed-race entries with zero mismatches;
- no disabled reason for an optimization intended to activate;
- exactly 200 campaign hours.

## Primary comparison fields

- `ProcessCpuSecondsPerCampaignHour`
- `WallSecondsPerCampaignHour`
- `CampaignHoursPerRealMinute`
- `ApplicationTicksPerCampaignHour`
- `ProcessCpuMillisecondsPerApplicationTick`
- `WallMillisecondsPerApplicationTick`
- `AverageFrameMilliseconds`
- `P95FrameMilliseconds`
- `P99FrameMilliseconds`
- `MaximumFrameMilliseconds`
- GC collection deltas

Compare reports with:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

The comparison utility refuses reports that did not use the stable start gate, used different start modes or stability intervals, enabled profiling, lacked process CPU measurement, or differed by more than 5% in application ticks per campaign hour.

## Focused attribution run

1. Select `Focused Profiler`.
2. Save MCM settings and restart Bannerlord.
3. Load the test save.
4. Select maximum campaign speed and wait for the stable-speed start message.
5. Travel on the campaign map and include one representative battle before completion.
6. Continue until the automatic 200-hour message appears.
7. Exit normally and preserve the profile, benchmark, and log files.

Focused Profiler measures the hit-point parent and child paths, map visibility and its reference helper, the weekly companion parent and major child methods, and the career-choice control. Its process-CPU figures include profiler overhead and are diagnostic only.

## Normal gameplay

After testing, select `Normal Gameplay`, save, and restart. This enables the selected validated optimizations without benchmark or profiler overhead.

## Interpretation limits

The start gate standardizes only the beginning of the run. Interruptions after the start message, including decision popups or menus that stop campaign time, still contaminate whole-process wall and CPU normalization and should be avoided or recorded.

Whole-process CPU includes all Bannerlord threads and does not isolate native engine subsystems. Frame percentiles use a fixed allocation-free 0.1 ms histogram; values at or above 250 ms share the last percentile bin, while the exact maximum remains separate.
