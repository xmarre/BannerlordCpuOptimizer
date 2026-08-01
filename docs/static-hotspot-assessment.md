# Static candidate assessment

Static evidence identifies profiling candidates. It does not establish that any candidate meets the release threshold or can be changed safely.

| Candidate | Actual inspected evidence | Initial risk | Milestone 1 action |
|---|---|---:|---|
| `WaaaghMeterMapView.OnMapScreenUpdate(float)` | 22-byte body calls the base map update and then `_vm.RefreshValues()` on every rendered map frame. SHA-256 `4279fe59…71762`. | Medium | Sample call count/time/allocations. Do not add dirty-state logic yet. |
| `WaaaghMeterVM.RefreshValues()` | 129-byte body reads main-hero culture and Waaagh resource state and updates view-model values. It can trigger state text/percentage recomputation. | Medium | Profile together with the map-view caller and map camera/zoom context. |
| `AbilityHUDMissionView.OnMissionTick(float)` | 232-byte body invokes three view-model `RefreshValues()` methods per eligible mission tick: ability HUD, radial selection, and career ability HUD. | Medium | Measure per-tick cost and allocation. Input/cast activation remains untouched. |
| `StatusEffectMissionLogic.OnMissionTick(float)` | 165-byte body enumerates `Mission.Current.AllAgents` every mission tick and resolves a status-effect component for each agent before processing. | High | Measure by living/total-agent count and spell load. Future active registry requires exact start/expiry/death/removal ordering validation. |
| `TORBattleAgentLogic.OnMissionTick(float)` | 583-byte body reads and enumerates `Mission.Agents` each mission tick. It is combat-facing. | Very high | Profile only. No scan replacement until every filter, order, mutation, and random dependency is mapped. |
| `AbilityManagerMissionLogic.OnPreMissionTick(float)` | 258-byte body processes queued ability work, sessions, effect disposal, state, input, and animation work. | Very high | Profile the complete method and subordinate hot methods. Per-frame input/state cadence is invariant. |
| `Ability.TickCastingState()` | 79-byte state transition method based on casting/pending state and mission time. Per-call work is small; aggregate frequency is unknown. | Medium | Sample at 1/16 by default; retain exact call count. |
| `AgentCastingBehaviorConfiguration.FindTargets(...)` | 445-byte body contains eight LINQ calls and already calls native nearby-agent APIs. | Very high | Measure allocations and target frequency. Any future rewrite must preserve target set/order and AI decisions; replacing it with proximity APIs is not the first step because it already uses them. |
| `TORMapVisibilityModel.GetPartySpottingRange(...)` | 354-byte model method returning `ExplainedNumber`. | High | Profile by frame/campaign speed. Do not cache without proving purity and invalidation of every party/visibility dependency. |
| `TORCustomResourceModel.GetCultureSpecificCustomResourceChange(...)` | 4,176-byte body, 15 LINQ calls, closure/display-class creation, and references to global settlement and kingdom collections while modifying an `ExplainedNumber`. | Very high | High-value profiler candidate. No result cache until call scope, ordering, mutable roster/relation/resource dependencies, and `ExplainedNumber` semantics are proven. |
| `ExtendedInfoManager.HourlyTick()` | 363-byte hourly handler reads `Hero.AllAliveHeroes`. | Low–medium | Measure late-campaign cost. Hourly cadence makes it less likely to meet a frame-time threshold. |

## TOR-wide static scan

The supplied `TOR_Core.dll` contains:

- 661 methods referencing `System.Linq.Enumerable`, with 1,333 LINQ member calls;
- 7 methods referencing `MobileParty.All`;
- 14 methods referencing `Hero.AllAliveHeroes`, with 17 calls;
- 11 methods referencing `Settlement.All`;
- 3 methods referencing `Clan.All`;
- 31 methods referencing `Kingdom.All`;
- 23 methods referencing `Mission.Agents`;
- 3 methods referencing `Mission.AllAgents`;
- 42 methods calling `ViewModel.RefreshValues`.

These counts are discovery signals. A loop, LINQ call, or global collection reference is not independently sufficient evidence for an optimization.

## Alternative hypotheses requiring measurement

1. Native engine work may dominate the target scenes, making managed TOR changes insignificant. WPR/WPA and in-game managed reports must be compared.
2. A method with obvious allocations may be called infrequently and fail the 0.2 ms / 5% significance gate.
3. UI refresh calls may internally short-circuit and allocate little. Direct timing/allocation data is required.
4. Full-agent scans may be cheaper than event-maintained registries at small battle sizes or may encode observable ordering. Agent-count scaling and direct output traces are required.
5. Campaign slowdowns may originate in other compatible mods rather than TOR or native Bannerlord. Harmony owner and declaring-assembly metadata are recorded to separate them.
