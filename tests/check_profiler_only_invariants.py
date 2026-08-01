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
    path_provider = (SRC / "Runtime" / "PathProvider.cs").read_text(encoding="utf-8")
    runtime = (SRC / "Runtime" / "OptimizerRuntime.cs").read_text(encoding="utf-8")
    patches = (SRC / "Profiling" / "HarmonyProfilerPatches.cs").read_text(encoding="utf-8")

    assert settings["Profiling"]["Enabled"] is False
    assert settings["General"]["ExperimentalNativePatches"] is False
    assert "HarmonyMethod transpiler" not in sources
    assert ".Transpiler" not in sources
    assert "return false;" not in patches
    assert not re.search(r"\b(Task|Thread|ThreadPool)\s*\.", sources), "No background TaleWorlds access or workers in Milestone 1"
    assert "SyncData(IDataStore dataStore)" in sources
    assert "Profiler state is intentionally never serialized" in sources
    assert "UnpatchAll" in sources
    assert "LifecycleManager.OnMissionEnded" in sources
    assert "MethodFingerprint.ComputeSha256" in sources

    assert "ModuleSettingsPath" in path_provider
    assert "ResolveSettingsPath" in path_provider
    assert "File.Exists(moduleSettingsPath)" in path_provider
    assert "PathProvider.ResolveSettingsPath()" in runtime
    assert "Settings loaded from:" in runtime
    assert "Effective profiler configuration:" in runtime

    # TORCustomResourceModel resolves localized GameText values in its static initializer.
    # Harmony must not touch that type while submodules are still loading.
    assert 'DeferredTorResourceModelType = "TOR_Core.Models.TORCustomResourceModel"' in patches
    assert "MustDeferUntilCampaignReady(specification)" in patches
    assert 'specification.TypeName.StartsWith(DeferredTorResourceModelType + "+"' in patches
    assert "internal int ApplyDeferredTargets()" in patches
    apply_start = patches.index("internal int Apply()")
    defer_check = patches.index("if (MustDeferUntilCampaignReady(specification))", apply_start)
    early_patch = patches.index("TryPatch(specification);", apply_start)
    assert defer_check < early_patch, "Unsafe type-family deferral must happen before Harmony patching"

    assert "GameCampaign.Current != null" in runtime
    assert "_session != null" in runtime
    assert "_profilerPatches?.ApplyDeferredTargets();" in runtime
    campaign_ready = runtime.index("GameCampaign.Current != null")
    deferred_apply = runtime.index("_profilerPatches?.ApplyDeferredTargets();")
    assert campaign_ready < deferred_apply, "Deferred targets may only attach after campaign startup"

    print("Milestone 1 profiler-only invariant checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
