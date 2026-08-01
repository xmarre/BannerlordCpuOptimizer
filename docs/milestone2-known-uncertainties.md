# Milestone 2 known uncertainties

- Runtime equivalence on the user's complete mod stack is not proven until the v0.2.0 shadow gate completes across normal play, save/load, and campaign restart.
- Focused profiler timings include profiler and cache-patch overhead. A profiler-off route is required for final performance claims.
- Foreign patches added dynamically after this cache attaches are not proactively detected.
- Map zoom remains optional reflection-based data and is disabled by default.
- The measured map-visibility and weekly-companion candidates remain unmodified.
