# Assembly inspection assessment

## Scope and method

The supplied PE/CLI assemblies were parsed directly. Metadata tables, method signatures, IL bodies, referenced members, module MVIDs, and SHA-256 hashes of raw IL were inspected. No conclusion below is based only on a method name.

The workspace contained no existing source repository. It contained the eight runtime assemblies, `TOR_Core.pdb`, and the implementation brief.

## Supplied build identity

| Assembly | Runtime | Types | Methods | MVID |
|---|---:|---:|---:|---|
| TOR_Core | v4.0.30319 | 1,483 | 10,129 | `933e2f8b-ec65-4b67-8a1d-153bcda29363` |
| TaleWorlds.CampaignSystem | v4.0.30319 | 2,297 | 26,911 | `4b87d2d0-89dd-4989-adb4-69b0cca71136` |
| TaleWorlds.Engine | v4.0.30319 | 230 | 4,187 | `3fb4feb9-c797-40f2-945f-74bbcd9e1994` |
| TaleWorlds.InputSystem | v4.0.30319 | 24 | 427 | `73047213-64e8-486f-bb38-7a6bbf4bb4e3` |
| TaleWorlds.Library | v4.0.30319 | 255 | 2,026 | `f951690e-4797-446e-a601-97418ef60bf5` |
| TaleWorlds.Localization | v4.0.30319 | 103 | 701 | `d438bef0-6afc-4c84-86a7-1b2871b20c62` |
| TaleWorlds.MountAndBlade | v4.0.30319 | 1,730 | 16,840 | `97564b07-7ad8-4ccd-9234-6076ec5623fe` |
| TaleWorlds.ObjectSystem | v4.0.30319 | 27 | 231 | `a5961eb4-58ed-4baa-8baf-9af3384e1a58` |

All supplied assembly versions report `1.0.0.0`, so assembly version alone cannot safely identify the target build. The profiler gate therefore uses MVID, exact signature, and IL fingerprint.

## Relevant lifecycle APIs confirmed

`MBSubModuleBase` exposes the required module/game/mission lifecycle hooks, including `OnSubModuleLoad`, `OnSubModuleUnloaded`, `OnGameStart`, `OnGameEnd`, `OnApplicationTick`, and `OnMissionBehaviorInitialize`.

`CampaignGameStarter.AddBehavior(CampaignBehaviorBase)` and `Mission.AddMissionBehavior(MissionBehavior)` are present.

`MissionBehavior` exposes the mission lifecycle callbacks required for future indexing and teardown, including agent creation/build/team/controller/removal/deletion events and mission end/removal callbacks. No indexes are implemented in Milestone 1.

Campaign event subscription uses `IMbEvent.AddNonSerializedListener(object, Action)` and generic variants. Milestone 1 reads each derived campaign behavior's actual `RegisterEvents` IL and profiles methods loaded by `ldftn`/`ldvirtftn`, avoiding reliance on handler naming conventions.

## Architecture consequences

- Runtime references must come from the installed game. `TaleWorlds.Core.dll`, SandBox view assemblies, and `0Harmony.dll` were not supplied in the workspace.
- TOR is handled through reflection and optional module dependency so the profiler can load without a compile-time TOR reference.
- MVID/signature/IL gates are mandatory because nominal file and assembly versions are not discriminating.
- All TaleWorlds state sampling executes from `OnApplicationTick` on the game thread.
- Only primitive snapshots, reflection metadata, and method metadata are retained. No `Agent`, `Mission`, `Campaign`, party, hero, or settlement instance is retained after a sample.
- The first milestone uses Harmony prefixes/postfixes only. There are no transpilers, finalizers, skipped originals, replacement results, or background workers.

## Missing inspection scope

The supplied set did not include `TaleWorlds.Core.dll`, `SandBox.dll`, `SandBox.View.dll`, or other optional modules. Their method bodies could not be inspected here. Runtime method discovery remains fail-closed, and the initial build does not patch those unknown bodies by default.
