# Test checklist

## Milestone 1 gates

- [x] Supplied assembly MVIDs verified by executable test.
- [x] Eleven fragile TOR method IL hashes verified by executable test.
- [x] Profiling disabled in default configuration.
- [x] No transpiler in source tree.
- [x] No Harmony prefix that suppresses the original.
- [x] No background thread/task worker.
- [x] Profiler state not serialized.
- [x] Mission end/removal teardown path present.
- [x] Harmony unpatch on module unload present.
- [ ] Compile with installed Bannerlord v1.3.15 and Bannerlord.Harmony references.
- [ ] Launch to main menu with profiler disabled.
- [ ] Launch TOR campaign with profiler disabled.
- [ ] Verify no Harmony patches are applied when profiler is disabled.
- [ ] Enable profiler and verify report generation.
- [ ] Measure profiler-off versus profiler-on overhead.
- [ ] Verify no retained `Agent`, `Mission`, `Campaign`, party, hero, or settlement instances after teardown.

## Campaign profiling matrix

- [ ] Early campaign at 1×.
- [ ] Late campaign at 1×.
- [ ] Late campaign at 2×.
- [ ] Late campaign at maximum speed.
- [ ] Fully zoomed out.
- [ ] Continuous pan/zoom.
- [ ] Many active parties and wars.
- [ ] TOR resource-heavy faction.
- [ ] Settlement ownership change.
- [ ] Selected/inspected/tracked entity changes.
- [ ] Save loading.
- [ ] Repeated save/load cycles.

Record process CPU, main-thread frame time, p50/p95/p99, campaign time per in-game hour, method time/calls, Gen 0/1/2, and managed memory.

## Battle profiling matrix

- [ ] 200-agent field battle.
- [ ] 500-agent field battle.
- [ ] Maximum practical battle size.
- [ ] Cavalry-heavy battle.
- [ ] Missile-heavy battle.
- [ ] Siege attack.
- [ ] Siege defence.
- [ ] TOR spell-heavy battle.
- [ ] Multiple simultaneous active effects.
- [ ] Reinforcement waves.
- [ ] Mission restart cycles.

Record method time/calls by agent/spell/missile count, p50/p95/p99, GC, and teardown state.

## Future optimization regression gates

No future patch ships until it has:

- [ ] a measured hotspot above the significance threshold;
- [ ] complete code-path inspection;
- [ ] documented symptom, trigger, contributing factors, root cause, and invariant;
- [ ] at least one alternative hypothesis tested;
- [ ] direct output/set/order shadow validation;
- [ ] save/load survival;
- [ ] mission restart survival;
- [ ] no retained gameplay objects;
- [ ] no repeatable GC regression;
- [ ] no p95/p99 regression;
- [ ] individual fail-closed fallback.
