# Milestone 3 benchmark procedure

Milestone 3 separates whole-process A/B measurement from method profiling. The A/B templates keep profiling disabled so profiler overhead cannot contaminate the CPU and frame-time result.

## Controlled campaign A/B run

Use one copied save as the starting point for both runs. Keep the complete module list, game settings, process priority, resolution, frame limiter, camera position, campaign speed, and background applications unchanged.

### Baseline

1. Install v0.3.0 cleanly.
2. Copy `settings.benchmark-baseline.json` over `settings.json`.
3. Launch the game and load the copied starting save.
4. Leave the campaign camera and speed in the chosen reproducible state.
5. Advance exactly 200 campaign hours, without opening menus or entering a mission unless that is part of the fixed route.
6. Exit normally so the report is written.
7. Preserve the optimizer log and both `BannerlordCpuOptimizer-Benchmark-...baseline-cache-disabled` files.

### Optimized

1. Restore the same untouched starting save.
2. Copy `settings.benchmark-optimized.json` over `settings.json`.
3. Repeat the identical route for exactly 200 campaign hours.
4. Exit normally.
5. Preserve the optimizer log and both `BannerlordCpuOptimizer-Benchmark-...optimized-cache-enabled` files.

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

The campaign-hour-normalized fields remain comparable if one run advances a slightly different number of campaign hours. A valid optimized run must also show cache promotion, active hits, zero mismatches, and no disabled reason.

Compare a baseline and optimized JSON report with:

```powershell
python tools\compare_benchmarks.py <baseline.json> <optimized.json>
```

The script refuses reports created with method profiling enabled and prints percentage improvements with the correct lower-is-better or higher-is-better direction for each metric.

## Focused attribution run

Copy `settings.profiler.json` over `settings.json` only for method attribution. That template enables the benchmark as a diagnostic companion, but its process-CPU values include profiler overhead and must not be compared directly with the profiling-free A/B runs.

The focused profiler targets are:

- `TORCharacterStatsModel.MaxHitpoints`
- `TORMapVisibilityModel.GetPartySpottingRange`
- the visibility settlement predicate
- `TORCommon.FindSettlementsAroundPosition`
- `TORCompanionsCampaignBehavior.WeeklyTick`
- `TORCareerChoices.GetChoice` as a low-rate control

Advance far enough to execute at least one weekly companion tick. Include one representative battle if mission counting and mixed campaign/mission context are required.

## Interpretation limits

The benchmark records whole-process CPU time across all Bannerlord threads. It does not isolate native engine subsystems. Frame percentiles use a fixed allocation-free 0.1 ms histogram; frame times of 250 ms or more share the final histogram bin, while the exact maximum remains separately recorded.
