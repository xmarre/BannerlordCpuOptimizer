#!/usr/bin/env python3
"""Compare one baseline and one optimized Bannerlord CPU Optimizer benchmark report."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


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


def lower_is_better(base: float, optimized: float) -> float:
    return 0.0 if base == 0.0 else (base - optimized) / base * 100.0


def higher_is_better(base: float, optimized: float) -> float:
    return 0.0 if base == 0.0 else (optimized - base) / base * 100.0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("baseline", type=Path)
    parser.add_argument("optimized", type=Path)
    args = parser.parse_args()

    baseline = load(args.baseline)
    optimized = load(args.optimized)
    if baseline.get("ProfilingEnabled") or optimized.get("ProfilingEnabled"):
        raise ValueError("A/B comparison requires profiling-free benchmark reports")

    fields = [
        ("CPU seconds per campaign hour", "ProcessCpuSecondsPerCampaignHour", lower_is_better),
        ("Wall seconds per campaign hour", "WallSecondsPerCampaignHour", lower_is_better),
        ("Campaign hours per real minute", "CampaignHoursPerRealMinute", higher_is_better),
        ("Average frame interval", "AverageFrameMilliseconds", lower_is_better),
        ("P95 frame interval", "P95FrameMilliseconds", lower_is_better),
        ("P99 frame interval", "P99FrameMilliseconds", lower_is_better),
        ("Gen0 collections", "Gen0CollectionsDelta", lower_is_better),
        ("Gen1 collections", "Gen1CollectionsDelta", lower_is_better),
        ("Gen2 collections", "Gen2CollectionsDelta", lower_is_better),
    ]

    print(f"Baseline:  {baseline.get('RunLabel', args.baseline.name)}")
    print(f"Optimized: {optimized.get('RunLabel', args.optimized.name)}")
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
