#!/usr/bin/env python3
"""Compare one baseline and one optimized Bannerlord CPU Optimizer benchmark report."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

MAX_APPLICATION_TICK_WORKLOAD_DIFFERENCE_PERCENT = 5.0
EXPECTED_START_CONDITION = "maximum-campaign-speed-stable"


def load(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8-sig") as handle:
        value = json.load(handle)
    if not isinstance(value, dict):
        raise ValueError(f"{path} does not contain a benchmark object")
    return value


def number(report: dict[str, Any], name: str) -> float:
    value = report.get(name)
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"Missing numeric field {name!r}")
    return float(value)


def text(report: dict[str, Any], name: str) -> str:
    value = report.get(name)
    if not isinstance(value, str) or not value:
        raise ValueError(f"Missing text field {name!r}")
    return value


def lower_is_better(base: float, optimized: float) -> float:
    return 0.0 if base == 0.0 else (base - optimized) / base * 100.0


def higher_is_better(base: float, optimized: float) -> float:
    return 0.0 if base == 0.0 else (optimized - base) / base * 100.0


def absolute_percent_difference(base: float, other: float) -> float:
    return 0.0 if base == 0.0 else abs(other - base) / abs(base) * 100.0


def validate_pair(baseline: dict[str, Any], optimized: dict[str, Any]) -> None:
    if baseline.get("ProfilingEnabled") or optimized.get("ProfilingEnabled"):
        raise ValueError("A/B comparison requires profiling-free benchmark reports")
    if not baseline.get("ProcessCpuMeasurementAvailable") or not optimized.get("ProcessCpuMeasurementAvailable"):
        raise ValueError("Process CPU measurement was unavailable in one or both reports")
    if number(baseline, "CampaignHours") <= 0.0 or number(optimized, "CampaignHours") <= 0.0:
        raise ValueError("Both reports must contain campaign-hour observations")

    baseline_condition = text(baseline, "StartCondition")
    optimized_condition = text(optimized, "StartCondition")
    if baseline_condition != EXPECTED_START_CONDITION or optimized_condition != EXPECTED_START_CONDITION:
        raise ValueError("Both reports must use the stable maximum-campaign-speed start gate")

    baseline_mode = text(baseline, "StartTimeControlMode")
    optimized_mode = text(optimized, "StartTimeControlMode")
    if baseline_mode != optimized_mode:
        raise ValueError(
            "Start time-control modes differ: "
            f"baseline={baseline_mode}, optimized={optimized_mode}"
        )

    baseline_stability = number(baseline, "StartStabilitySeconds")
    optimized_stability = number(optimized, "StartStabilitySeconds")
    if abs(baseline_stability - optimized_stability) > 0.000001:
        raise ValueError(
            "Start-gate stability intervals differ: "
            f"baseline={baseline_stability}, optimized={optimized_stability}"
        )

    baseline_ticks_per_hour = number(baseline, "ApplicationTicksPerCampaignHour")
    optimized_ticks_per_hour = number(optimized, "ApplicationTicksPerCampaignHour")
    workload_difference = absolute_percent_difference(baseline_ticks_per_hour, optimized_ticks_per_hour)
    if workload_difference > MAX_APPLICATION_TICK_WORKLOAD_DIFFERENCE_PERCENT:
        raise ValueError(
            "Application-tick workload differs by "
            f"{workload_difference:.3f}% per campaign hour; maximum allowed is "
            f"{MAX_APPLICATION_TICK_WORKLOAD_DIFFERENCE_PERCENT:.3f}%"
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("baseline", type=Path)
    parser.add_argument("optimized", type=Path)
    args = parser.parse_args()

    baseline = load(args.baseline)
    optimized = load(args.optimized)
    validate_pair(baseline, optimized)

    fields = [
        ("CPU seconds per campaign hour", "ProcessCpuSecondsPerCampaignHour", lower_is_better),
        ("Wall seconds per campaign hour", "WallSecondsPerCampaignHour", lower_is_better),
        ("Campaign hours per real minute", "CampaignHoursPerRealMinute", higher_is_better),
        ("Application ticks per campaign hour", "ApplicationTicksPerCampaignHour", lower_is_better),
        ("CPU ms per application tick", "ProcessCpuMillisecondsPerApplicationTick", lower_is_better),
        ("Wall ms per application tick", "WallMillisecondsPerApplicationTick", lower_is_better),
        ("Average application-tick interval", "AverageFrameMilliseconds", lower_is_better),
        ("P95 application-tick interval", "P95FrameMilliseconds", lower_is_better),
        ("P99 application-tick interval", "P99FrameMilliseconds", lower_is_better),
        ("Gen0 collections", "Gen0CollectionsDelta", lower_is_better),
        ("Gen1 collections", "Gen1CollectionsDelta", lower_is_better),
        ("Gen2 collections", "Gen2CollectionsDelta", lower_is_better),
    ]

    print(f"Baseline:  {baseline.get('RunLabel', args.baseline.name)}")
    print(f"Optimized: {optimized.get('RunLabel', args.optimized.name)}")
    print(
        "Start gate: "
        f"condition={baseline['StartCondition']} "
        f"mode={baseline['StartTimeControlMode']} "
        f"stable_seconds={number(baseline, 'StartStabilitySeconds'):.3f}"
    )
    print()
    print("Metric | Baseline | Optimized | Improvement")
    print("--- | ---: | ---: | ---:")
    for label, key, formula in fields:
        base_value = number(baseline, key)
        optimized_value = number(optimized, key)
        improvement = formula(base_value, optimized_value)
        print(f"{label} | {base_value:.6f} | {optimized_value:.6f} | {improvement:+.3f}%")

    cache = optimized.get("CareerChoiceCache") or {}
    print()
    print(
        "Optimized cache: "
        f"state={cache.get('RuntimeState', 'unknown')} "
        f"hits={cache.get('ActiveHits', 0)} "
        f"mismatches={cache.get('Mismatches', 0)} "
        f"promotions={cache.get('Promotions', 0)} "
        f"disabled_reason={cache.get('DisabledReason') or '<none>'}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
