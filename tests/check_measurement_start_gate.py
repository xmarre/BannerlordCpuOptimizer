#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "BannerlordCpuOptimizer"

gate = (SRC / "Benchmarking" / "MeasurementStartGate.cs").read_text(encoding="utf-8")
runtime = (SRC / "Runtime" / "OptimizerRuntime.cs").read_text(encoding="utf-8")
report = (SRC / "Benchmarking" / "BenchmarkReport.cs").read_text(encoding="utf-8")
session = (SRC / "Benchmarking" / "BenchmarkSession.cs").read_text(encoding="utf-8")
writer = (SRC / "Benchmarking" / "BenchmarkReportWriter.cs").read_text(encoding="utf-8")

# The gate uses real elapsed time, requires an unlocked campaign map, excludes missions,
# and resets its continuous-stability window whenever maximum speed is lost.
assert "RequiredStableSeconds = 1.5" in gate
assert "Stopwatch.GetTimestamp()" in gate
assert "campaign.TimeControlModeLock" in gate
assert "Mission.Current != null" in gate
assert "_maximumSpeedSinceTimestamp = 0L;" in gate
for mode in (
    "CampaignTimeControlMode.StoppableFastForward",
    "CampaignTimeControlMode.UnstoppableFastForward",
    "CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime",
):
    assert mode in gate
assert "CampaignTimeControlMode.FastForwardStop" not in gate

# Game load arms the gate instead of starting either report immediately. The application
# tick opens it only after stable maximum speed, then resets every measurement boundary.
on_game_started = runtime.split("internal static void OnGameStarted()", 1)[1].split("internal static void OnGameEnded()", 1)[0]
assert "ArmMeasurementStart();" in on_game_started
assert "StartSession();" not in on_game_started
assert "StartBenchmark(" not in on_game_started
assert "TryStartArmedMeasurement(currentCampaign);" in runtime
assert "gate.TryOpen(currentCampaign, out CampaignTimeControlMode startMode)" in runtime
assert "set campaign speed to maximum" in runtime
assert "measurement started at stable maximum campaign speed" in runtime

# The report proves that the gate was used and exposes workload-normalization fields.
for field in (
    "StartCondition",
    "StartTimeControlMode",
    "StartStabilitySeconds",
    "ApplicationTicksPerCampaignHour",
    "ProcessCpuMillisecondsPerApplicationTick",
    "WallMillisecondsPerApplicationTick",
):
    assert field in report
    assert field in session
assert "start_condition" in writer
assert "application_ticks_per_campaign_hour" in writer

print("Stable maximum-speed measurement start gates passed.")
