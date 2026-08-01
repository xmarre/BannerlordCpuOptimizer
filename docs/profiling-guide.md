# Profiling guide

## Purpose

Milestone 1 established the initial managed CPU and allocation profile. Milestone 2 uses a focused profiler to validate the measured career-choice optimization with substantially less instrumentation overhead.

## Configuration

The active packaged configuration is:

```text
Modules\BannerlordCpuOptimizer\ModuleData\BannerlordCpuOptimizer\settings.json
```

`settings.profiler.json` is the focused enabled template. Copy it over `settings.json` for the Milestone 2 validation run. It enables only:

- `TORCareerChoices.GetChoice`
- `CareerHelper.CalculateTroopWageCareerPerkEffect`
- `TORCommon.FindSettlementsAroundPosition`

Broad TOR campaign, mission, model, and vanilla profiling are disabled in the template. `RuntimeOverlay` remains disabled.

The packaged file is authoritative when present. Its fallback is:

```text
Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\settings.json
```

Startup logs print the selected path, effective profiler flags, and effective cache-validation settings.

## Focused 200-hour procedure

Use the same module list, save, graphics settings, camera route, campaign speed, and approximate real-time duration used for the original 200-hour profile.

1. Install v0.2.0 cleanly.
2. Copy `settings.profiler.json` over `settings.json`.
3. Load the same campaign save.
4. Run 200 campaign hours with the same map route and speed behavior.
5. Include one representative battle so mission counting is exercised.
6. Save and reload once.
7. Continue after reload until the cache reports another shadow promotion.
8. Exit normally so every report is finalized.
9. Repeat the route with profiling disabled for the final performance comparison.

A valid focused run requires:

- zero cache mismatches;
- no disabled/fallback reason;
- at least one independently validated ID;
- at least one promotion per campaign instance;
- active cache hits after promotion;
- mission count greater than zero when a mission occurred;
- periodic audits during a sufficiently long run.

The profiler records exact call counts. Timing and allocation are sampled. Call-count-scaled estimates remain estimates and must be interpreted alongside profiler-off measurements.

## Cache report interpretation

The JSON report and `-optimization.csv` include:

- configured mode and current runtime state;
- campaign binding and session generation;
- cached and independently validated IDs;
- calls, active hits, misses, and stores;
- reference-identical shadow comparisons;
- per-ID validations;
- mismatches and unexpected null results;
- promotions and original-call audits;
- fallback reason.

A cache hit is possible only after the global shadow threshold and the individual ID validation have both completed.

## Context metrics

`EnableOptionalContextMetrics` controls reflection-based optional metrics such as map zoom. It is independent of `AllowUnknownProfilerTargets`.

Missing optional metrics do not affect method profiling or optimization gates.

## Fail-closed behavior

Profiler targets are skipped when their assembly is absent, their signature or IL changed, the module MVID is unknown without explicit profiler permission, or Harmony patching fails. A profiler exception disables only that profiler record.

The active optimization has a stricter policy. It never allows unknown modules. It is refused when the MVID, signature, or IL fingerprint differs, and when another Harmony owner already modifies the target. Runtime mismatches clear and disable the cache for that campaign while preserving the original TOR method.

## Output

```text
Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

Files:

- `...Profile-<session>.json`
- `...Profile-<session>-methods.csv`
- `...Profile-<session>-context.csv`
- `...Profile-<session>-optimization.csv`

A session is finalized on game end or module unload.
