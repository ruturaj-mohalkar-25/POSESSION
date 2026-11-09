# Campaign gameplay consistency

All three playable campaign stages share movement input, buffered Spirit jumping, charge validation, rear ejection, shield timing and visuals, combat pause rules and the common HUD. Existing level geometry, targets, walls and authored puzzle routes are retained.

## Shared rules

| Mechanic | All three campaign levels |
| --- | --- |
| Spirit movement / jump launch speed | 8 m/s horizontally / 8 m/s vertically |
| Jump input | Space in Spirit form; fixed-step movement, ground probe, 0.15 s input buffer and 0.1 s edge grace |
| Guard / Heavy patrol speed | 2.5 / 2 m/s |
| Autonomous patrol turns / pauses | 240 degrees/s; look-around pauses averaging 0.9 s with slight variation |
| Guard / Heavy base movement | 5 / 4 m/s; the existing Heavy weight cue applies its same 0.65 possession factor |
| Spirit / Guard / Heavy size | 1.25 / 1.25 / 1.625 root scale |
| Spirit health / charge capacity | 5 HP / 3 charges, 1 starting charge |
| Possession range / eject cooldown | 5 m / 2 s |
| Eject protection | Exactly 2 s of game time after manual, timed or host-loss ejection; one cyan countdown ring/arch/bubble |
| Rear ejection | Prefer a clear rear position, then safe sides; ground the actual Spirit capsule and reject solid-wall crossings |
| Guard / Heavy detection | 9 / 7 m, 75-degree full cone; green cue uses the same target selection as E |
| Guard attack / Heavy reach | 8 m shots / 3.5 m smash; existing reaction delays, windup and attack cooldowns retained |
| Enemy pursuit | Bounded by each enemy's authored room or patrol area; player-controlled bodies may cross rooms |
| Camera | Shared merged offset (0, 25, -18) |
| Pause and completion | Movement/possession input and attacks stop; Hunted contact pressure follows the same rule |

Level clocks and possession durations remain authored challenge settings: Level 1 uses 180 s / 8 s, Level 2 uses 240 s / 10 s and Warehouse uses 420 s / 12 s. The tutorial keeps its smaller practice layout and coaching pauses while using the shared jump/eject/shield code.

## Fixes

Spirit grounding no longer depends on a single CollisionEnter contact. Ground is checked using the scaled body every physics step, including after restore/eject. Jump presses near landing are buffered; holding a body or pausing cannot queue an unintended jump. Visibility changes clear old velocity and input.

Shield appearance and damage protection now use the same protection deadline rather than possession cooldown. The shared effect is installed for every Spirit. Warehouse's old component delegates to it. The ghost visual synchronizer controls only the authored ghost model, preventing initialization-order-dependent reactivation of shields or carried-charge renderers.

Host defeat/deactivation and destruction return a visible, dynamic Spirit using the cached host pose. Input and green possession cues share range, wall, cone, charge and cooldown checks. Enemy facing normalization retains tiny movement steps while removing zero-vector rotation warnings.

Saved scenes now receive the movement/camera settings already merged in PR 69. The campaign authoring pass recomputes Level 1/2 patrol areas, including when Hunted clones actors from another stage. Warehouse retains its explicit home areas. Reapply through **Possession → Level Design → Tune Campaign Characters and Enemy Cones**.

The final Level 1 Guard previously received the smaller of two overlapping floor areas. Its last waypoint lay on that area's edge, beyond the body clearance needed to reach it, and the same boundary prevented detection farther down the corridor. Patrol area selection now fits the full route and body radius. Warehouse uses authored four-point loops around cover, smooth turns and look-around pauses rather than short left-right shuttles.

## Verification

Unity **6000.3.22f1**: **229/229 full PlayMode tests passed, 0 failed, 0 skipped**. This includes all campaign stages and the tutorial; actual shots/smashes and puzzle routes; retries, Next Level and final replay; repeated landings and buffered jumping; manual/forced Guard and Heavy ejection in every stage; blocked rear positions; shield expiry/damage; lost hosts; pause/completion and ghost-renderer ownership.

The initial baseline was 187/212 passing. Outdated fixtures were updated for current HUD placement, starting charges, ranges, expanded spawn markers and movement values. Tutorial fixtures now enter real possession range and use the validated action. Existing behavioral checks remain active; ten checks brought the total to 222. Seven further checks cover the final Guard's repeated patrol cycles and real shooting, reachable home boundaries, shared patrol settings, smooth turns and waits, clear Warehouse loops and actual full-loop completion by all six Warehouse enemies, bringing the total to 229.

A scripted graphic preview additionally verified post-eject jumping, both shield modes, restored damage/HP feedback and the native campaign completion/replay flow in all three stages. It pauses enemy scripts, relocates hosts and shortens the possession timer inside the temporary preview only; it is not a human difficulty playtest. Screenshots and logs are retained in local QA artifacts.

A further scripted preview observed the final Guard and Warehouse patrols for 16 seconds each, and capsule sweeps confirmed all 18 campaign enemies' route segments clear of static blockers, allowing valid low steps. The graphic editor logged an unrelated UnityEditor.Search indexing exception; the full PlayMode run completed without test failures.

## Local playtest

Stop Play Mode, reopen MainMenu, enter Play and click inside Game view. In each stage: jump repeatedly as Spirit, possess a Guard from outside its cone, shoot a target, manually eject and immediately jump. Repeat with Heavy and smash a wall. Let possession expire, check the two-second cyan shield and then restored damage, and try ejection beside a wall. Verify charges, HP, retry and stage progression. Watch the final Level 1 Guard complete a return trip in the right corridor and check that Warehouse patrols circle their rooms, pause and turn smoothly. Hunted's C checkpoint and R restart remain available.

Changes are local on Omkar/objective-visibility-ui. No push or PR update was made.
