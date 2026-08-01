# Milestone 2 regression checklist

- Clean startup with profiler disabled.
- Focused startup attaches exactly the three focused targets.
- Unknown MVID, changed IL, signature mismatch, or foreign Harmony owner skips the cache patch.
- First result for an ID is non-null before storage.
- Each ID passes `ReferenceEquals` validation before serving.
- New IDs after global promotion remain shadow-only until validated.
- Audit mismatch, unexpected null, prefix error, or postfix error clears and disables the cache for the campaign.
- Save/load and campaign replacement restart shadow validation with no stale object references.
- Game end and module unload clear all cache state.
- Mission count increments for each distinct non-null `Mission.Current` instance.
- Party wages and explanations remain identical.
- No optimizer state appears in save data.
