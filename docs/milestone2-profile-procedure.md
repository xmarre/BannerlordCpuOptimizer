# Milestone 2 profile procedure

Copy `settings.profiler.json` over `settings.json`. This enables only the three focused targets and leaves broad TOR/vanilla profiling disabled.

Run the same save and 200-campaign-hour route as the baseline. Include one representative battle, save, reload, continue until the cache promotes again, and exit normally.

A valid run must show zero cache mismatches, at least one independently validated ID, at least one promotion, active cache hits after promotion, and periodic audits on a sufficiently long run. A mismatch or fallback reason invalidates the optimization gate even when gameplay continues through the original TOR method.

The report set includes JSON plus methods, context, and optimization CSV files. A profiler-off repeat of the same route remains required for a final CPU claim.
