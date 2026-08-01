# Changelog

## 0.3.1 - MCM configuration

- Replaced all packaged JSON settings and mode templates with a proper MCM v5 menu.
- Added one operating-mode selector for Normal Gameplay, Benchmark - Baseline, Benchmark - Optimized, Focused Profiler, and Custom.
- Made the baseline and optimized modes automatically enforce profiling-free comparison settings, cache state, and fixed report labels.
- Added grouped MCM controls for optimization gates, career-choice cache validation, profiler targets, sampling, reports, and diagnostics.
- Made MCM global settings authoritative and retained the old JSON loader only as an emergency fallback.
- Delayed runtime initialization until MCM settings are available after `OnSubModuleLoad`.
- Added a required `Bannerlord.MBOptionScreen` dependency without bundling MCM assemblies.
- Removed `settings.json`, `settings.profiler.json`, `settings.benchmark-baseline.json`, and `settings.benchmark-optimized.json` from the release package.
- Added release gates rejecting manual settings templates and bundled MCM runtime DLLs.
- Preserved the exact-gated career-choice cache, benchmark output, focused profiler target set, and Harmony teardown behavior.

## 0.3.0 - Whole-process campaign measurement

- Added profiler-independent whole-process benchmark sessions.
- Added process CPU time, normalized CPU percentages, campaign throughput, frame-time average/p50/p95/p99/maximum, mission counts, GC deltas, and managed-memory deltas.
- Added allocation-free fixed-histogram frame recording.
- Added JSON and CSV benchmark reports.
- Added profiling-free baseline and optimized A/B settings templates.
- Added focused attribution for `TORCharacterStatsModel.MaxHitpoints`.
- Added focused attribution for `TORMapVisibilityModel.GetPartySpottingRange`, its settlement predicate, and `TORCommon.FindSettlementsAroundPosition`.
- Added focused attribution for `TORCompanionsCampaignBehavior.WeeklyTick`.
- Added a controlled 200-campaign-hour A/B procedure.
- Kept the v0.2.1 career-choice cache as the only active gameplay optimization; all new targets remain observation-only.

## 0.2.1 - Harmony teardown fix

- Replaced the unsupported generic Harmony patch class with a non-generic runtime-emitted patch type.
- Emitted the exact TOR `CareerChoiceObject` return type for prefix and postfix `__result` parameters.
- Fixed profiler and optimization unpatch failures under runtime Harmony 2.4.2.
- Preserved the allocation-free struct state path and the existing cache algorithm, shadow threshold, audit cadence, target fingerprint, and lifecycle boundaries.
- Added an executable Harmony 2.4.2 integration harness covering cache operation, profiler removal, optimization removal, original-method restoration, and final owner cleanup.
- Recorded a successful focused validation session with 1,117,589 cache calls, 1,116,160 active hits, 49 of 49 IDs validated, 1,091 audits, zero mismatches, zero null-result changes, one promotion, and one correctly counted mission.
- Retained save/load campaign-identity transition testing as a separate runtime check because the submitted session contained one campaign cache generation.

## 0.2.0 - Measured TOR wage-path optimization

- Added a strictly MVID/signature/IL-gated cache for `TORCareerChoices.GetChoice(string)`.
- Bound cached TOR object references to the exact current campaign instance.
- Added a 256-comparison reference-identity shadow gate before activation.
- Required every career-choice ID, including IDs first seen after activation, to pass its own reference comparison before serving.
- Added one original-call audit per 1,024 validated cache-hit candidates.
- Added fail-closed fallback on mismatches, unexpected nulls, exceptions, campaign changes, game end, and module teardown.
- Refused the optimization on unknown/changed TOR builds or foreign Harmony ownership of the target.
- Added focused direct profiling for `TORCareerChoices.GetChoice`, `CareerHelper.CalculateTroopWageCareerPerkEffect`, and `TORCommon.FindSettlementsAroundPosition`.
- Added a focused profiler template with broad TOR and vanilla profiling disabled.
- Fixed mission counting by following `Mission.Current` identity transitions.
- Separated optional context metrics from `AllowUnknownProfilerTargets`.
- Added JSON and CSV cache validation statistics, including a dedicated optimization CSV.
- Preserved AI cadence, simulation cadence, mission logic, UI behavior, native systems, save format, and original TOR formulas.

## 0.1.2 - TOR startup crash fix

- Fixed profiler startup faulting `TOR_Core.Models.TORCustomResourceModel` before localized game texts were initialized.
- Deferred the complete `TORCustomResourceModel` type family, including compiler-generated nested methods, until campaign startup has completed.
- Preserved profiling coverage by attaching those deferred targets on the first campaign application tick.
- Added a release gate enforcing deferral before any Harmony patch attempt.
- Preserved every other target, sampling rate, report format, and profiler-only gameplay boundary.

## 0.1.1 - Profiler settings loading fix

- Fixed the packaged `ModuleData/BannerlordCpuOptimizer/settings.json` being ignored at runtime.
- Made the packaged module settings file authoritative when present.
- Retained the Documents configuration as a fallback when the packaged file is missing.
- Added startup logging for the exact settings path and effective profiler flags.
- Added a release gate covering the packaged-settings loading contract.

## 0.1.0 - Profiler-only milestone

- Added a Bannerlord v1.3.15 module skeleton targeting .NET Framework 4.7.2.
- Added conservative JSON configuration with profiling disabled by default.
- Added exact assembly MVID, method-signature, and IL-fingerprint validation.
- Added observation-only Harmony prefix/postfix profiling; no transpilers or original suppression.
- Added discovery of actual `CampaignBehaviorBase.RegisterEvents` delegate targets from IL.
- Added sampled elapsed-time and allocation counters with exact call counts.
- Added campaign, mission, GC, party, settlement, agent, missile, spell-session, speed, zoom, and battle context snapshots where available.
- Added CSV and JSON report writers, runtime overlay, log throttling, and lifecycle teardown.
- Added executable binary-fingerprint and profiler-only invariant tests.
- Added no gameplay optimization patches.
