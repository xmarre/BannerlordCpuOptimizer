#!/usr/bin/env python3
"""Exercise the benchmark comparison utility with valid and rejected reports."""
from __future__ import annotations

import json
import pathlib
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tools" / "compare_benchmarks.py"


def report(label: str, optimized: bool) -> dict:
    factor = 0.9 if optimized else 1.0
    return {
        "RunLabel": label,
        "ProfilingEnabled": False,
        "ProcessCpuMeasurementAvailable": True,
        "CampaignHours": 200,
        "StartCondition": "maximum-campaign-speed-stable",
        "StartTimeControlMode": "StoppableFastForward",
        "StartStabilitySeconds": 1.5,
        "ApplicationTicksPerCampaignHour": 65.0 if optimized else 66.0,
        "ProcessCpuMillisecondsPerApplicationTick": 4.0 * factor,
        "WallMillisecondsPerApplicationTick": 14.0 * factor,
        "ProcessCpuSecondsPerCampaignHour": 2.0 * factor,
        "WallSecondsPerCampaignHour": 3.0 * factor,
        "CampaignHoursPerRealMinute": 20.0 / factor,
        "AverageFrameMilliseconds": 10.0 * factor,
        "P95FrameMilliseconds": 20.0 * factor,
        "P99FrameMilliseconds": 30.0 * factor,
        "Gen0CollectionsDelta": 100 * factor,
        "Gen1CollectionsDelta": 20 * factor,
        "Gen2CollectionsDelta": 5 * factor,
        "CareerChoiceCache": {
            "RuntimeState": "Enabled" if optimized else "Disabled",
            "ActiveHits": 1000 if optimized else 0,
            "Mismatches": 0,
            "Promotions": 1 if optimized else 0,
            "DisabledReason": None,
        },
    }


def run(script: pathlib.Path, baseline: pathlib.Path, optimized: pathlib.Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(script), str(baseline), str(optimized)],
        check=False,
        capture_output=True,
        text=True,
    )


def main() -> int:
    with tempfile.TemporaryDirectory() as temporary:
        directory = pathlib.Path(temporary)
        baseline = directory / "baseline.json"
        optimized = directory / "optimized.json"
        baseline.write_text(json.dumps(report("baseline", False)), encoding="utf-8")
        optimized.write_text(json.dumps(report("optimized", True)), encoding="utf-8")

        valid = run(SCRIPT, baseline, optimized)
        assert valid.returncode == 0, valid.stderr
        assert "CPU seconds per campaign hour" in valid.stdout
        assert "Start gate:" in valid.stdout
        assert "+10.000%" in valid.stdout
        assert "state=Enabled" in valid.stdout

        invalid_data = report("profiled", True)
        invalid_data["ProfilingEnabled"] = True
        optimized.write_text(json.dumps(invalid_data), encoding="utf-8")
        invalid = run(SCRIPT, baseline, optimized)
        assert invalid.returncode != 0
        assert "profiling-free" in invalid.stderr

        invalid_data = report("wrong-start", True)
        invalid_data["StartCondition"] = "game-load"
        optimized.write_text(json.dumps(invalid_data), encoding="utf-8")
        invalid = run(SCRIPT, baseline, optimized)
        assert invalid.returncode != 0
        assert "stable maximum-campaign-speed start gate" in invalid.stderr

        invalid_data = report("different-workload", True)
        invalid_data["ApplicationTicksPerCampaignHour"] = 80.0
        optimized.write_text(json.dumps(invalid_data), encoding="utf-8")
        invalid = run(SCRIPT, baseline, optimized)
        assert invalid.returncode != 0
        assert "Application-tick workload differs" in invalid.stderr

    print("Benchmark comparison utility checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
