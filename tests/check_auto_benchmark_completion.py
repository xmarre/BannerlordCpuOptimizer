#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
runtime = (ROOT / "src" / "BannerlordCpuOptimizer" / "Runtime" / "OptimizerRuntime.cs").read_text(encoding="utf-8")

assert "private const int AutomaticBenchmarkTargetHours = 200;" in runtime
assert "if (_benchmarkSession == null)" in runtime
assert "_benchmarkSession?.CampaignHourElapsed();" in runtime
assert "_benchmarkCampaignHours++;" in runtime
assert "_benchmarkCampaignHours < AutomaticBenchmarkTargetHours" in runtime
assert "WriteBenchmark(completionReason);" in runtime
assert "if (ProfilingEnabled)" in runtime
assert "WriteSession(completionReason);" in runtime
assert "InformationManager.DisplayMessage" in runtime
assert "Reports were written; you can exit normally." in runtime
assert runtime.index("_benchmarkSession?.CampaignHourElapsed();") < runtime.index("WriteBenchmark(completionReason);")
assert runtime.index("WriteBenchmark(completionReason);") < runtime.index("InformationManager.DisplayMessage")
assert "_benchmarkCampaignHours = 0;" in runtime

print("Automatic 200-campaign-hour benchmark completion gates passed.")
