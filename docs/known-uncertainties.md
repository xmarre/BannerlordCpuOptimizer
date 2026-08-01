# Known uncertainties and regression risks

## Unverified in this workspace

- A C# compiler, .NET SDK, Mono, and Bannerlord's missing `TaleWorlds.Core`, SandBox, and Harmony binaries were unavailable. Source compilation and game launch could not be executed locally.
- The exact SandBox map camera/zoom member for v1.3.15 was not present in the supplied assemblies. Zoom collection is reflection-based and optional.
- TOR active spell sessions are confirmed by field inspection. A universal active-effect registry field was not confirmed; the optional metric reports discoverable sessions/effect collections only.
- Static inspection cannot establish method wall-time significance, profiler overhead, cache safety, deterministic equivalence, or end-to-end CPU reduction.
- Large Bannerlord battles can diverge from engine nondeterminism. Future validation must compare the optimized query/result path directly.

## Milestone 1 risks

- Prefix/postfix instrumentation adds call-count and sampling overhead. Very small high-frequency methods can be distorted; compare profiler-off and profiler-on runs and increase `HighFrequencySampleEvery` when needed.
- Timing includes nested calls and time spent in foreign Harmony patches according to patch ordering. Harmony ownership is logged to aid interpretation.
- A postfix does not execute when the original throws. This avoids changing exception behavior; that invocation remains counted but unsampled in elapsed time.
- Reflection context metrics may become unavailable on another version. They fail independently and do not alter gameplay.
- Report writing occurs at game end/module unload and can take time proportional to captured samples. It does not run every frame.

## Future optimization risks

- Stable indexes can change iteration order, mutation behavior, random-number consumption, and lifecycle semantics.
- UI dirty-state caching can miss camera, selection, inspected entity, ownership, faction, resource, or configuration invalidation.
- Model result caching can accidentally share mutable `ExplainedNumber` values or ignore roster/relation/resource revisions.
- Active-effect registries can retain removed agents or miss effects created/expired during reentrant callbacks.
- Native nearby-agent APIs may produce a different initial ordering than existing scans; exact final ordering must be reconstructed when observable.
- Harmony transpilers against already modified IL have high compatibility risk and remain outside Milestone 1.
