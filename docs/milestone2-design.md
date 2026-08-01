# Milestone 2 design

## Measured trigger

The 200-campaign-hour profile identified repeated `TORCareerChoices.GetChoice(string)` resolution inside the party-wage path as the first low-risk optimization target.

## Preserved invariant

For a given career-choice ID and one `Campaign.Current` instance, the optimized path may return only the same object reference returned by TOR's original lookup.

## Activation sequence

1. Validate TOR assembly MVID, exact method signature, and raw IL SHA-256.
2. Refuse activation when another Harmony owner modifies the target, excluding this mod's observation-only profiler.
3. Bind cache lifetime to the current campaign object.
4. Call the original method during shadow validation.
5. Require 256 successful reference comparisons before global activation.
6. Require every individual ID to pass its own reference comparison before serving it.
7. Audit one in every 1,024 validated hit candidates through the original method.
8. Clear and disable the cache for the session on mismatch, unexpected null, or patch exception.

Null results are never cached. Cache state is never serialized. Campaign identity changes invalidate every cached object before it can be served.
