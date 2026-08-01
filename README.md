# BannerlordCpuOptimizer

Measured managed-code optimization and profiling support for Mount & Blade II: Bannerlord v1.3.15 and The Old Realms v1.16.

## Current status

Milestone 2 contains one active optimization: a session-local cache for `TORCareerChoices.GetChoice(string)`, selected from a 200-campaign-hour profile. The patch applies only to the exact verified TOR v1.16 assembly, method signature, and IL body.

Every campaign starts in shadow mode. The original TOR lookup continues to run until the cache has observed 256 reference-identical results. Each career-choice ID must also pass its own reference-identity comparison before that ID can be served. One in every 1,024 validated hit candidates is audited through TOR's original method. A mismatch, unexpected null, exception, campaign replacement, game end, or module unload clears the cache and returns execution to the original TOR method.

The focused v0.2.0 validation session recorded 1,117,589 cache calls, 1,116,160 active cache hits, 49 of 49 encountered IDs validated, 1,091 original-call audits, zero mismatches, zero null-result changes, and one successful promotion. Mission identity counting also recorded the included battle correctly. That session contained one campaign cache generation, so an explicit save/load campaign-identity transition remains a separate runtime check.

v0.2.1 replaces the generic Harmony patch class used by v0.2.0 with a non-generic runtime-emitted patch type whose `__result` parameters use the exact TOR return type. This fixes the Harmony 2.4.2 teardown failure found after the validation report was written. CI executes a Harmony 2.4.2 integration harness that applies the cache and profiler owners to the same method, removes them in production order, verifies that the cache survives profiler removal, restores the original method after cache removal, and confirms that no owner remains.

The implementation contains no AI throttling, simulation throttling, mission-logic optimization, UI throttling, native pathfinding/physics patch, transpiler, background TaleWorlds access, or serialized optimizer state.

The profiler remains available and is disabled by default. The supplied profiler template measures only the three focused Milestone 2 targets:

- `TORCareerChoices.GetChoice`
- `CareerHelper.CalculateTroopWageCareerPerkEffect`
- `TORCommon.FindSettlementsAroundPosition`

Broad TOR and vanilla discovery are disabled in that template to minimize measurement overhead.

`TORCustomResourceModel` profiler targets remain deferred until campaign startup has completed, preventing early initialization of TOR's localized-text model.

## Project layout

- `src/BannerlordCpuOptimizer`: module source
- `module/BannerlordCpuOptimizer`: distributable Bannerlord module layout
- `docs/milestone2-design.md`: cache invariant and activation sequence
- `docs/milestone2-profile-procedure.md`: focused validation procedure
- `docs/milestone2-regression-checklist.md`: lifecycle and equivalence checks
- `docs/milestone2-known-uncertainties.md`: remaining runtime uncertainties
- `docs/assembly-inspection.md`: supplied-binary inspection
- `tests/BannerlordCpuOptimizer.HarmonyTeardownHarness`: executable Harmony 2.4.2 teardown integration test
- `tests`: executable release gates

## Build

Requirements:

- Windows, Linux, or macOS
- .NET 8 SDK
- Python 3
- NuGet access

The project builds against pinned Bannerlord v1.3.15 reference assemblies and `Lib.Harmony` v2.3.3. The teardown integration harness runs against runtime Harmony v2.4.2. No game or TOR binaries are committed or bundled.

```powershell
.\build.ps1 -Configuration Release
.\package.ps1 -Configuration Release
```

Expected files:

```text
module\BannerlordCpuOptimizer\bin\Win64_Shipping_Client\BannerlordCpuOptimizer.dll
artifacts\BannerlordCpuOptimizer-v0.2.1-milestone2.zip
artifacts\SHA256SUMS.txt
```

## Configuration

The packaged configuration is authoritative when present:

```text
Modules\BannerlordCpuOptimizer\ModuleData\BannerlordCpuOptimizer\settings.json
```

The Documents configuration is used only when the packaged file is missing:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\settings.json
```

The relevant optimization settings are:

```json
"CareerChoiceCacheMode": "ShadowThenEnable",
"CareerChoiceShadowComparisons": 256,
"CareerChoiceMinimumDistinctIds": 1,
"CareerChoiceAuditEvery": 1024
```

Modes:

- `Disabled`: do not attach the cache patch.
- `ShadowOnly`: collect reference-equivalence evidence without serving cached results.
- `ShadowThenEnable`: enable each validated ID after the global shadow threshold is met.

Profiling is independent of the optimization. Use the default:

```json
"Profiling": {
  "Enabled": false
}
```

for normal gameplay. The cache remains active when profiling is disabled.

## Focused profiling

Copy `settings.profiler.json` over `settings.json` only for a deliberate measurement run. Reports are written under:

```text
%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\BannerlordCpuOptimizer\reports
```

The report set contains:

```text
BannerlordCpuOptimizer-Profile-<session>.json
BannerlordCpuOptimizer-Profile-<session>-methods.csv
BannerlordCpuOptimizer-Profile-<session>-context.csv
BannerlordCpuOptimizer-Profile-<session>-optimization.csv
```

A valid optimization run has zero mismatches, at least one independently validated ID, at least one promotion, active cache hits, and no fallback reason. Mission counts are derived from `Mission.Current` identity transitions. Optional context metrics are controlled separately by `EnableOptionalContextMetrics` and do not relax method validation.

## Validation policy

The optimization requires the known TOR module MVID, exact declaring type, exact signature, and exact raw-IL SHA-256. Unknown or changed builds are refused. The optimization is also refused when another Harmony owner already modifies the target, excluding this module's observation-only profiler.

`AllowUnknownProfilerTargets` affects profiler attachment only. It never relaxes an optimization gate.

## Tests

```powershell
python tests\check_profiler_only_invariants.py
python tests\check_source_structure.py
dotnet run --project tests\BannerlordCpuOptimizer.HarmonyTeardownHarness\BannerlordCpuOptimizer.HarmonyTeardownHarness.csproj --configuration Release
```

GitHub Actions runs the static gates, restores pinned references, compiles for .NET Framework 4.7.2, executes the Harmony 2.4.2 teardown harness, packages the module, and rejects bundled Harmony, TaleWorlds, TOR, or test runtime DLLs.

The focused cache-equivalence gate passed for the submitted campaign session. Explicit save/load campaign-identity transition testing remains open because that session recorded one cache generation. No complete process-CPU or frame-time percentage is claimed from the focused managed-method report alone.
