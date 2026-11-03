# Level 3 — Alarm Compound

A local replacement for the old linear Level 3, based on the two-floor charge design. Use Unity 6000.3.22f1. Open `Assets/Scenes/Level_03.unity` and press Play; rebuild with **Possession → Level 3 → Build Alarm Compound**. The original scene GUID and campaign slot are retained.

## Goal and controls

Reach upper-floor extraction through any valid route. WASD moves, Space jumps as Spirit, E possesses/ejects, and LMB uses the current host's shared ability. Collect a green shard before each possession. Capacity is one; only a free Spirit collects. Ejecting, expiry, and host defeat leave an empty Spirit. Failed possession does not spend the charge.

The relaxed Level 3 configuration gives **12 seconds per possession and eight minutes per attempt**. Medal thresholds are 240/360/450 seconds. Guard player speed remains 3.25 and Heavy base speed 2.2 before the shared possessed-Heavy slowdown; health stays three/five. These are playtest values, not final difficulty targets.

Only patrol, chase, and hostile pursuit use a 0.4 movement multiplier: about 1.3 m/s for Guards and 0.88 m/s for Heavies. Player-controlled bodies keep their normal speed. Enemies need two seconds of continuous visible contact before ordinary pursuit, with shorter detection ranges (Guard 3.2 m, Heavy 2.8 m). Breaking sight resets notice time. Contact attacks have a 2.5-second windup and a shared five-second damage gap. The initial contact grace period is twelve seconds. Forced-eject hostility still works, with slower attacks and enough time to leave the danger area.

## Layout

Two stacked floors contain twelve main zones, four optional dead ends, five Guards, four Heavies, eight reforming charge sources (K1–K8), and two one-use bonus caches (K9/K10). Three reversible physical stair flights connect matching footprints across floors. Visual steps sit over continuous collision ramps. Full-height sidewalls and upper guards seal the stair shafts; structural walls and ability barriers are six metres high, matching the existing levels and preventing ordinary crate-jump bypasses. The service stair and extraction edge have wider landings to let the Heavy turn without clipping the sidewalls. The layout unit is now 2.2 m, about 38% wider than the first version: typical rooms are 17.6 m wide and the smallest passages are 4.4 m. Cover keeps roughly its original footprint rather than scaling up with the architecture, and is lowered to two metres. The intake and cargo courtyard are clear of enemies for planning; essential puzzle hosts remain in their encounter rooms. Enemy spawns and short patrols are validated after the stairs and puzzle geometry are built, with small clearance adjustments where needed.

The normal shared camera follows the active body and blends height as the player climbs. Overhead renderers are hidden while downstairs; their collision remains active. Floors block sight, possession, and projectiles. Shared shooting and its aim cue use the player's current floor height in Level 3. There is no current-room camera mask.

Night cues use lanterns, colored circuit wires, linked-enemy antennas, and readable charge silhouettes. Small labels identify puzzle components; there are no floor instructions or tutorial popups. The shared HUD keeps health, time, possession, ammunition, and completion results. A small status strip gives a short next-step objective, charge state, and circuit state. Blue Main A and gold Service B signs distinguish the initial choices without automatic popups.

## Optional planning map

Press **M** or click Map to see both floors, room names, stairs A/B/C, puzzle labels, charge sources, current player position, locked/open gates, and extraction. The map is optional and never opens automatically. **M, Escape, or Resume** closes it. While it is open, the level and possession timers, physics, enemy attacks, and player controls are paused. Charge inventory is preserved. Shared gameplay does not consume ammunition when a menu or map button is clicked.

The map summarizes A as Guard → T1, B as Heavy → B1, and C as R → B3, with the records/extraction options upstairs. It supports route planning rather than adding another timed instruction popup.

## Four strategies

| Strategy | Required sequence | Typical shown possessions |
| --- | --- | --- |
| Main | K1 → Guard shoots T1 → stair A → K7 → Heavy smashes PW → K5 → Guard shoots T3 → extraction | 3 |
| Service | K3 → Heavy smashes B1 → stair B → K5 → Guard shoots T3 → extraction | 2 |
| Shutdown | K3 → Heavy smashes B1 → stair B → K5 → Guard shoots SE → K6 → Heavy smashes PE → extraction | 3 |
| Maintenance | K1 → Guard shoots T1 → stair A → K4 → Guard shoots R → return downstairs → K8 → Heavy smashes B3 → stair C behind the extraction doors | 3 |

These counts describe the shown transfers, not a claim of minimum charge use. Opened gates and broken walls persist in the attempt. Using stairs, retreating, and exiting cost no extra charge. Exit accepts the actual player in any form; NPCs cannot finish the level.

## Alarms

West controls stair A and the upper bridge. East controls the two ordinary extraction approaches. Linked enemies have a matching blue or purple antenna and wires. A linked watcher must be alive, unoccupied, nearby, facing the suspect, and have real unobstructed sight of a linked possessed host or a recently witnessed ejection. Continuous suspicion warns before a circuit latches. Walls and slabs block observation.

JW and JE disconnect their circuits and prevent future alarms, but do not clear a latched alarm. Heavy smash panels PW and PE clear and disable their respective circuits. PW also releases A and the bridge; PE releases normal extraction access. Shooting T3 does not override an active East lockdown.

SE permanently opens the service latch before PE. K6 and the East Heavy are outside that gate. R permanently opens the lower C shutter; B3 must also be smashed. Alarms do not erase charges or progress, disable pickups, relock SE/R, or repair B1/B3. The outer upper loop and service stair provide recovery routes when the usual gates are locked. Hostile projectiles cannot activate the shared targets, and one-damage bullets cannot replace Heavy smash damage.

## Checkpoints and restart

Two blue beacons offer safe-area checkpoints: the main-stair shelter downstairs and the records shelter upstairs. Approach as the free Spirit and press **F** when its charge source is ready and no enemy is close. Press **C** or Retry to reload the saved checkpoint; **R** or Restart begins a fresh attempt. Menu returns to the shared main menu.

Checkpoints preserve target and relay latches, broken walls/panels, alarm flags, pickup cooldowns, consumed bonus caches, and host positions. Reload recreates usable living hosts, including required hosts that were killed after saving. It starts an empty Spirit at the beacon with a reachable ready source. The native attempt health, time, and result counters restart; checkpoint recovery is not a continuation of the same scored timer. Checkpoints are for the current Play session, not disk saves.

## Implementation boundaries

Level-specific code lives in `Assets/Scripts/Level03`, its builder in `Assets/Editor/Level03`, and its layout/material/geometry assets in `Assets/Level03`. The builder delegates from the existing Level 3 menu and protects its placement from legacy Guard/Heavy setup tools. Actors, movement, abilities, damage, targets, walls, HUD, analytics, and scene flow remain shared.

Minimal shared hooks provide reversible security-door state, floor-aware aim, pickup state capture/restore plus vertical proximity validation, slope-aware ejection clearance, and optional enemy awareness/pursuit tuning. The latter defaults to instant notice and normal movement in existing levels; the relaxed settings are applied only to Level 3. Shared ability input also ignores pointer clicks over UI. Security-controlled doors ignore the automatic target listener, so unrelated targets do not create false unlock messages or camera pans. Flat-floor ejection behavior is preserved. Levels without this multi-floor layout keep their existing aim height. Level 1 and Level 2 scenes and configurations are unchanged.

## Local branch

Work is on `Omkar/level-three-alarm-compound`, based on the Level 2 commit `6058ec6`. It depends on the charge mechanic in Level 2 PR #46. The Level 2 remote branch and PR have not been modified by this work. This Level 3 work is local until a separate publish request.

## Verification

The revised PlayMode checks cover all four puzzle strategies with real shared shooting/smash abilities, each staircase in both directions with Spirit/Guard/Heavy bodies, host and pickup counts, checkpoint restoration, cross-floor protection, correct gate events, and grounded empty-Spirit ejection/expiry/host defeat. Added checks verify that the map preserves health, time and charge while suspending input, and that Guards need sustained visible contact before slower pursuit. Patrol checks use the authored enlarged layout.

Scripted visual captures are reviewed for room space, signs, both floors, and the paused route map. The optional map has concise labels rather than tutorial popups. Human playtesting is still required to tune difficulty; the aim of this revision is navigable space and time to plan.

Earlier full-suite failures already reproduced on integration are bar-chart cleanup, the Heavy movement-ratio test, and three HUD color assertions. They are unrelated to this playability revision and remain documented.

Final relaxed-build verification: **18/18 Level 3 checks pass; 156/161 across the full PlayMode suite**. The five remaining failures are the same integration-baseline failures listed above. Visual checks confirm the expanded start area, route signs, and concise paused map without text overflow.
