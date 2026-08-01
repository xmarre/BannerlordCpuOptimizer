# BannerlordCpuOptimizer

Profiler-only Milestone 1 for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

## Current status

This version does not optimize or replace gameplay code. Profiling is disabled by default. When enabled, it adds observation-only Harmony prefixes/postfixes to validated managed methods, records sampled elapsed time and approximate allocations, and writes reports during game/module teardown.

The implementation deliberately contains no AI throttling, simulation throttling, native pathfinding/physics patches, transpilers, background TaleWorlds access, or serialized optimizer state.

## Project layout

- `src/BannerlordCpuOptimizer`: module source
- `module/BannerlordCpuOptimizer`: distributable Bannerlord module layout
- `docs/assembly-inspection.md`: actual supplied-binary inspection
- `docs/static-hotspot-assessment.md`: candidate list and risk ranking
- `docs/profiling-guide.md`: profiler operation and baseline procedure
- `docs/test-checklist.md`: campaign/battle regression matrix
- `tests`: executable profiler-only release gates

## Build

Requirements:

- Windows, Linux, or macOS
- .NET 8 SDK
- Python 3
- NuGet access

The project builds against the pinned `Bannerlord.ReferenceAssemblies.Core` v1.3.15.110062 package and `Lib.Harmony` v2.3.3. No game binaries are committed or copied into the release.

```powershell
.\build.ps1 -Configuration Release
```

Expected output:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
```

Package after a successful build:

```powershell
.\package.ps1
```

Expected package:

```text
artifacts\BannerlordCpuOptimizer-v0.1.0-profiler-only.zip
```

## Enable profiling

On first launch, the module creates:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\settings.json
```

Set `Profiling.Enabled` to `true`, or copy the values from:

```text
Modules\BannerlordCpuOptimizer\ModuleData\BannerlordCpuOptimizer\settings.profiler.json
```

Reports are written to the `reports` directory beside the user configuration. Logs are written to the sibling `logs` directory.

## Reports

The method report records exact call count, sampled call count and ratio, sampled and estimated elapsed time, maximum and sampled-average elapsed time, calls per frame/hour/mission, and sampled/estimated allocations where available.

Context reports sample campaign speed, optional map zoom, party/settlement counts, agent counts, missiles, discoverable TOR spell sessions/effects, battle type, GC collections, and managed memory.

## Validation policy

Known fragile targets require exact assembly name, known module MVID, exact signature, and exact IL SHA-256. Unknown builds are skipped by default. `AllowUnknownProfilerTargets` relaxes only the MVID requirement for profiling; it never enables a gameplay optimization.

## Tests

```powershell
python tests\check_profiler_only_invariants.py
python tests\check_source_structure.py
```

GitHub Actions builds and packages every pull request. A real game run is still required to establish profiler overhead and hotspot significance. No performance or equivalence claim is made from static inspection alone.
