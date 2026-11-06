# Warehouse — local Level 3 preview

Use Unity **6000.3.22f1**. Open `Assets/Level03Warehouse/Level_03_Warehouse.unity` and press Play. If the level was already open when its saved scene changed, stop Play Mode and reopen it; accept Reload if Unity asks. The scene is intentionally absent from the campaign build list while shared gameplay updates are pending.

## Campaign plan

| New slot | Source |
| --- | --- |
| Level 1 | Current displayed Level 2 (legacy `Assets/Scenes/Level_03.unity`) |
| Level 2 | Current displayed Level 3, Hunted maze (legacy `Assets/Scenes/Level_02.unity`) |
| Level 3 | This Warehouse design |

The current campaign scenes, menu and build settings are unchanged. Apply the campaign order after the team's shared changes merge.

## Expanded layout

Fifteen rooms on one floor, with the widened existing rooms retained in their world positions. The former Exit is now a Transfer Lobby. Two new final branches extend beyond it: Records/Control and Workshop/Machine. Both lead to the relocated extraction area. Sixteen cover blocks and eight solid partition segments have been added, bringing the layout to 29 cover blocks. Camera cutaway affects rendering while the partitions still block movement, vision and shots.

Totals: **5 Guards, 3 Wall Breakers, 5 target doors, 16 targets, 5 breakable barriers, 13 reforming possession-charge sources and 1 health-placement socket**.

## Initial wings

- **Left:** Entry → Sorting → Guard Room. Shoot its three cyan targets to open Inspection. Shoot Inspection's three violet targets to open Exit Hall → Transfer Lobby. The original right wing and middle walls can stay closed.
- **Right:** Shoot Sorting's three pink targets to open Cargo. Shoot Cargo's three amber targets to open Loading. Use the right-junction Wall Breaker to smash Loading's cracked exit wall → Exit Hall → Transfer Lobby. The original left wing and middle walls can stay closed.
- **Optional recovery crossing:** Service Lobby → left cracked barrier → health chamber → right cracked barrier → right junction. Either wall can be opened independently and remains open for retreat. Both broken entrances allow a wing change, without solving target doors.

The new partitions in Guard Room and Cargo require changing firing angles. Inspection and Loading have more offset cover. Both initial wings remain complete routes to Transfer Lobby.

## Final branches

Choose either after reaching Transfer Lobby; completing both is optional.

- **Records → Control:** Records has opposing wall baffles with broad zigzag gaps. Control has a local Guard, recharge source, partial wall and four gold targets. All four distinct targets open its sliding Extraction door; four lamps show progress. Three hits do not open it, and duplicate hits never count again. Shared Guard ammo is three shots per possession, so eject, recharge from the local source and possess again to finish the fourth target. Earlier hits persist.
- **Workshop → Machine:** Workshop has a vertical baffle with two approaches. Machine has a local Wall Breaker and recharge source before an internal cracked divider. Break it, then recharge and use a fresh possession to break the cracked exit wall. Shared Heavy rules retain one smash per possession. The final exit wall sits entirely beyond the divider, preventing a bypass through its near end. Both broken walls remain open.

The new Exit is a checkered deck and illuminated arch. It requires **one initial wing AND one final branch**, accepts Spirit or the currently possessed living body, and rejects unoccupied enemies. Transfer Lobby has no completion trigger. No additional charge or enemy-clear task is required at the new Exit.

## Space and tuning

The coordinate scale remains 0.65 metres. Main existing wing rooms are 22.1 × 14.3 m and Sorting is 16.9 × 13 m. New challenge rooms are 20.8 × 13 m. Main passages and final door openings remain 6.5–7.8 m wide. The Records baffles each leave a 7.8 m gap. Crates, characters, attack ranges and camera settings retain their physical size so the larger rooms provide usable movement space. The small Guard Room patrol is kept clear of its new partition.

Seven-minute level clock, twelve seconds per possession; tuning remains in `Data/warehouse-layout.json`. Combat, movement, vision, health, abilities, charge rules and ejection use the merged integration systems and copied Level 1 actor settings. Human playtesting is still needed to judge final pacing and combat difficulty.

## Controls and retries

WASD move, Space jump, E possess/eject, LMB shoot/smash. Collect a green crystal before possessing, and approach outside the enemy vision cone. Sources reform after eight seconds. **R** or Retry resets the whole preview, including defeated hosts, targets, walls and sources. The shared result screen's next button replays this local scene. Charge regeneration does not replace a defeated required host; use Retry in that case.

## Ejection protection and enemy rooms

Manual ejection, timer expiry and a completed return from a defeated host grant the Spirit **two seconds of damage protection** in this scene. A blue bubble, upright arch and draining ground ring show the remaining grace period. Damage and health/damage events are ignored during that window; ordinary damage resumes afterward. Movement and collision remain enabled.

All eight NPCs are assigned the floor bounds of their room (the right-junction Heavy uses its junction area). Enemies stop pursuing when the Spirit leaves that area, cancel attacks across its boundary and keep autonomous movement inside. The rule applies to patrol detection and post-eject hostility. Possessed bodies can still cross rooms to solve puzzles. If the player leaves a body outside its assigned home, it stops pursuing there and resumes returning to its own patrol once its hostile recovery ends.

The shared `PossessionController` grants protection directly after finding a valid eject location, before any eject callbacks. It no longer depends on a visual component or possession-event subscription. The two-second rule applies to every successful ejection using that controller, including older campaign scenes. `WarehouseEjectShield` only displays the actual protection state.

Ejection prefers the enemy's rear, then rear diagonals, sides and finally its front if needed. Ground checks, capsule clearance and a swept path check prevent spawning inside or through a wall. If the rear is blocked, a safe alternative is chosen.

The shared enemy and attack scripts have optional home-bound checks, enabled only in Warehouse. `WarehouseSceneSafety` also repairs missing Warehouse visuals and room assignments on Play/scene load, covering older open scenes whose in-memory setup predates the saved builder changes. It is idempotent and leaves other scenes' room rules unchanged. These hooks use the shared health and enemy systems, rather than duplicating them.

## Health dependency

The green cross remains a **placement socket, not a functioning heal**. Current integration has no shared healing API. Bind the team's incoming component here after merge. Intended rule: once per run, restore 30% of maximum Spirit/player HP, capped at maximum, persistent after ejecting, collectible in either form, not consumed at full HP. Do not introduce a second local HP pool or implement healing through negative damage.

## Rebuild and merge boundary

Use **Possession → Level Design → Level 3 Warehouse → Build and Open**. The builder copies the latest Level 1 shell and actors, then generates this scene from the authored JSON. Rebuilding replaces edits made only in the generated scene; keep durable layout changes in that JSON. Boundary generation supports the new negative plan coordinates, so the original built rooms do not shift to make room for the extension.

After the shared gameplay work merges, bring integration into this local branch and rebuild to inherit current shared components. Resolve any changed APIs and retest before registering the campaign order. Future merge conflicts cannot be guaranteed absent. The ejection-protection API, rear spawn search and optional room checks touch the shared Spirit, PossessionController, EnemyBase and enemy-attack scripts. Existing prefabs, campaign scenes and build settings are unchanged.

## Validation

The complete PlayMode regression suite passes **195/195 tests**, including the original scenes and Warehouse.

**33 Warehouse PlayMode checks pass**: expected shared components/counts; structural edge sealing across the extension; wider-room and doorway clearance; all distinct hits with animated leaves; each original route; recovery entrance sealing and retreat; real Guard shots to all sixteen targets; hostile-shot rejection; shared damage/smash rules; forced-eject persistence; four-target progress at three hits; all four initial/final route combinations; Machine divider bypass prevention; real local-charge retries for Control and Machine; and shared completion, occupied-body acceptance and replay reset.

Additional checks verify manual/forced-eject protection, actual hostile bullets during the shield, damage after expiry, no shield on spawn or an invalid ejection, all host room assignments, chase termination at a doorway, hostile Heavy attack cancellation and player-controlled movement outside home bounds, clear rear spawn for both host types, blocked rear fallback without teleporting through geometry, protection without a visual component and recovery of older open-scene setup.

Screenshots inspect the entry, existing wings, recovery, Transfer Lobby, both new final branches and the relocated extraction area. These automated and visual checks do not replace human combat testing. Work stays local; no remote push, PR or deployment is created.
