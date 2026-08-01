#!/usr/bin/env python3
"""Static release gate for the Milestone 1 source tree."""
from __future__ import annotations

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "BannerlordCpuOptimizer"


def main() -> int:
    sources = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))
    settings = json.loads((ROOT / "module" / "BannerlordCpuOptimizer" / "ModuleData" / "BannerlordCpuOptimizer" / "settings.json").read_text())

    assert settings["Profiling"]["Enabled"] is False
    assert settings["General"]["ExperimentalNativePatches"] is False
    assert "HarmonyMethod transpiler" not in sources
    assert ".Transpiler" not in sources
    assert "return false;" not in (SRC / "Profiling" / "HarmonyProfilerPatches.cs").read_text()
    assert not re.search(r"\b(Task|Thread|ThreadPool)\s*\.", sources), "No background TaleWorlds access or workers in Milestone 1"
    assert "SyncData(IDataStore dataStore)" in sources
    assert "Profiler state is intentionally never serialized" in sources
    assert "UnpatchAll" in sources
    assert "LifecycleManager.OnMissionEnded" in sources
    assert "MethodFingerprint.ComputeSha256" in sources
    print("Milestone 1 profiler-only invariant checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
