# Changelog

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
