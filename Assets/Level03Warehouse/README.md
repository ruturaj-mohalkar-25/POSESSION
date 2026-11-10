# Warehouse — Level 3, original route with wider spacing

Use Unity **6000.3.22f1**. Stop Play Mode, reopen `Assets/Scenes/MainMenu.unity`, then Play → Level 3. Direct scene: `Assets/Level03Warehouse/Level_03_Warehouse.unity`. Reopen an already-open scene rather than saving its older in-memory objects over the updated asset.

## Campaign

The campaign order is unchanged: previous displayed Level 2 (`Assets/Scenes/Level_03.unity`) → Hunted (`Assets/Scenes/Level_02.unity`) → Warehouse. Expanded Levels 1 and 2 retain their authored dimensions. Scene names and GUIDs remain stable.

## Original layout

The authored JSON is based on the original ten-room route with the wider room and passage dimensions: **10 rooms, 10 connecting halls, 13 cover blocks, 4 Guards, 2 Wall Breakers, 4 target doors, 12 targets, 3 breakable barriers and 9 reforming charge sources**.

Records, Control, Workshop, Machine, their connecting halls, the four-target final door, the two additional cracked walls and the two extra enemies are removed. The original northern Exit room is the finish again; there is no transfer lobby or final extension objective.

- **Left route:** Entry → Sorting → Guard Room. Shoot its three cyan targets to open Inspection. Shoot Inspection's three violet targets to open Exit Hall → Exit. The right wing and recovery barriers can stay closed.
- **Right route:** Shoot Sorting's three pink targets to open Cargo. Shoot Cargo's three amber targets to open Loading. Possess the right-junction Wall Breaker and smash Loading's cracked exit wall → Exit Hall → Exit. The left wing can stay closed.
- **Optional middle crossing:** Service Lobby → left recovery barrier → health-placement chamber → right recovery barrier → right junction. Barriers stay broken, allowing retreat or a wing change. The green cross is still a placement socket awaiting the shared healing API, not a functioning heal.

Completing either original wing unlocks the exit. It accepts the living Spirit or the currently possessed living body; unoccupied enemies cannot finish the level. Warehouse remains the final campaign stage, with the native Finish → Game Complete → Play Again flow.

## Dimensions and current gameplay

The retained wider layout uses **0.65 metres per plan unit**. Entry is 16.9 × 11.7 m; Sorting is 16.9 × 13 m; Guard/Cargo rooms are 22.1 × 14.3 m; Inspection/Loading are 22.1 × 15.6 m; Recovery is 23.4 × 7.8 m; Service Lobby is 11.05 × 7.8 m. Main connecting passages are 6.5–7.8 m wide. Cover blocks keep their existing compact physical sizes; characters, wall heights and gameplay tuning are unchanged. Only the additional sections and their objectives are removed.

The shared [gameplay consistency rules](../../Docs/Campaign-Gameplay-Consistency.md) apply to jumping, ejection and shield feedback. The current [character/combat tuning](../../Docs/Campaign-Character-Combat-Tuning.md) is retained: characters 25% larger, 75° vision cones, Guard detection/shooting at 9/8 m and Heavy detection/attack reach at 7/3.5 m. The merged camera/HUD, shared five HP, charge rules, shooting/smashing and two-second eject protection are retained. The level clock remains seven minutes with twelve seconds per possession.

All six Warehouse NPCs use authored four-point loops with smooth turns and brief look-around pauses. Guards patrol at 2.5 m/s and chase at 5 m/s; Wall Breakers patrol at 2 m/s and chase at 4 m/s. Detection remains 9 m for Guards and 7 m for Wall Breakers, matching the other campaign stages.

Warehouse NPCs retain their home-room bounds for autonomous pursuit; possessed bodies can cross rooms. Safe ejection prefers behind the host, checks capsule clearance and avoids crossing solid walls. R or Retry reloads the current scene and restores its targets, barriers, charge sources and hosts. Required defeated hosts are restored by Retry.

## Authoring and checks

Use **Possession → Level Design → Level 3 Warehouse → Build and Open**. The builder copies the shared shell/actors, reads `Data/warehouse-layout.json`, then applies the current campaign character/combat profile. Durable layout edits belong in the JSON, because rebuilding replaces scene-only edits.

**232/232 full PlayMode checks pass** across the current campaign and tutorial. Checks cover the retained wider dimensions and object counts, physical routes for larger bodies, absence of the removed rooms/objects, both independent wings, all remaining target shots and barriers, original exit conditions, NPC rejection/occupied-body completion, retry, campaign Finish/Play Again, rear-ejection safety, shield duration and current enemy pressure.

Human playtesting is still required to judge timing and difficulty.
