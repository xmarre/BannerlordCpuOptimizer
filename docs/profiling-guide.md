# Profiling guide

## Purpose

Milestone 1 establishes where managed CPU time and allocations occur. It does not modify gameplay logic.

## Baseline procedure

Use the same module list, save, graphics settings, battle size, camera route, campaign speed, and test duration for every comparison.

1. Keep `Profiling.Enabled=false` and collect an external baseline.
2. Enable profiling with `RuntimeOverlay=false`; repeat the identical scenario.
3. Enable the overlay only for diagnostic use, not final overhead or performance measurements.
4. Compare profiler-off and profiler-on frame-time p50/p95/p99, CPU use, campaign-hours-per-minute, and GC counts.
5. Reject or increase sampling for any target whose instrumentation materially changes the scenario.
6. Use WPR/WPA or another external sampler to determine whether native engine time or managed code dominates.

The profiler records exact call counts. Timing and allocation are sampled. The report labels sampled totals and call-count-scaled estimates separately.

## Recommended sessions

### Campaign map

- Early campaign, 1× speed, stationary camera, 5 minutes.
- Late campaign, maximum speed, zoomed out, 5 minutes.
- Late campaign, continuous camera movement and zoom changes, 5 minutes.
- TOR resource-heavy faction with Waaagh/resource UI visible.
- Settlement ownership change and selected/inspected party changes.
- Save/load and repeated load/save cycles.

### Battle

- 200-agent field battle.
- 500-agent field battle.
- Maximum practical battle size.
- Missile-heavy battle.
- Cavalry-heavy battle.
- Siege attack and defence.
- TOR spell-heavy battle with multiple simultaneous effects.
- Reinforcement waves and mission restart cycles.

## Report interpretation

Prioritize methods that satisfy at least one initial gate:

```text
>= 0.2 ms in a relevant frame
or
>= 5% of measured managed workload in the scenario
```

Also require that a prospective optimization does not worsen p95/p99 frame time or move work into a different frame.

High call count alone is insufficient. A tiny method may appear frequently and remain irrelevant. A large model method may be expensive per call and still be irrelevant if called once per day.

### Allocation values

`GetAllocatedBytesForCurrentThread` is resolved by reflection. When unavailable, allocation fields remain zero and `AllocationCounterAvailable=false` is written to JSON. Allocation deltas include allocations made by the profiled method and nested calls on the same thread during the sample.

### Context values

Some context fields are optional and may be `-1`/null:

- map zoom depends on the loaded SandBox view implementation;
- missile collections vary by game version;
- active spell/effect count depends on TOR field availability;
- campaign speed and battle type are read from available runtime members.

Missing optional metrics do not disable method profiling.

## Fail-closed behavior

A profiler target is skipped when:

- its assembly is absent;
- its MVID is unknown and unknown profiler targets are not explicitly allowed;
- its declaring type, method, parameter, or return type changed;
- a known IL fingerprint changed;
- the method has no managed body;
- Harmony patching throws.

A profiling prefix/postfix that throws disables only that method's profiler record and logs one error. The original method is never skipped.

## Output

```text
Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

Files:

- `...Profile-<session>.json`
- `...Profile-<session>-methods.csv`
- `...Profile-<session>-context.csv`

A session is finalized on game end or module unload.
