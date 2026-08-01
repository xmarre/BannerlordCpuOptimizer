# Changelog

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
