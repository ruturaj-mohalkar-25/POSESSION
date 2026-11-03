# Level 3 — Alarm Compound

A local replacement for the old linear Level 3, based on the two-floor charge design. Use Unity 6000.3.22f1. Open `Assets/Scenes/Level_03.unity` and press Play; rebuild with **Possession → Level 3 → Build Alarm Compound**. The original scene GUID and campaign slot are retained.

## Goal and controls

Reach upper-floor extraction through any valid route. WASD moves, Space jumps as Spirit, E possesses/ejects, and LMB uses the current host's shared ability. Collect a green shard before each possession, then approach a host from behind or the side, outside its visible vision cone. Capacity is one; only a free Spirit collects. Ejecting, expiry, and host defeat leave an empty Spirit. Failed possession, including a front-facing attempt, does not spend the charge.

The relaxed Level 3 configuration gives **12 seconds per possession and eight minutes per attempt**. Medal thresholds are 240/360/450 seconds. Guard player speed remains 3.25 and Heavy base speed 2.2 before the shared possessed-Heavy slowdown; health stays three/five. These are playtest values, not final difficulty targets.

Only patrol, chase, and hostile pursuit use a 0.4 movement multiplier: about 1.3 m/s for Guards and 0.88 m/s for Heavies. Player-controlled bodies keep their normal speed. P1's 45-degree cone is shown on the floor, clipped by walls, and hidden while possessed. Enemies need two seconds of continuous visible contact before ordinary pursuit, with shorter detection ranges (Guard 3.2 m, Heavy 2.8 m). They pause to observe during this notice period. Breaking sight resets initial notice.

P2's shared attack system handles combat: Guards shoot real projectiles and Heavies telegraph a smash. Ordinary attacks have a 1.2-second warning after detection; Heavy windup lasts 2.2 seconds, and attacks have five-second shot/smash cooldowns. Losing sight cancels the Heavy windup. Enemies search the last seen position for 1.5 seconds, reacquire immediately during that search, then return to patrol if the Spirit stays hidden. Forced-eject hostility remains active with its existing escape delay. The old extra contact-attack component is removed.

The Spirit has 3 HP. All damage in this level shares a five-second recovery window, with twelve seconds of protection at the start of a fresh attempt or checkpoint retry. Other levels retain their existing damage timing. Actual attack distance includes height, and Heavy impacts require clear sight, so an enemy cannot smash the Spirit through the other floor.

Enemy bullets use one damage path for swept raycasts and trigger contacts. A swept hit now applies damage before the bullet is removed, and duplicate callbacks cannot apply the same hit twice. This corrects the shared projectile behavior instead of replacing the shooting system locally.

## Layout

Two stacked floors contain twelve main zones, a power-service bay, a dedicated extraction bay, four optional dead ends, five Guards, four Heavies, eight reforming charge sources (K1–K8), and two one-use bonus caches (K9/K10). Three reversible physical stair flights connect matching footprints across floors. Visual steps sit over continuous collision ramps. Full-height sidewalls and upper guards seal the stair shafts; structural walls and ability barriers are six metres high, matching the existing levels and preventing ordinary crate-jump bypasses. The service stair and extraction edge have wider landings to let the Heavy turn without clipping the sidewalls. The layout unit is 2.2 m: typical rooms are 17.6 m wide and the smallest passages are 4.4 m. Cover keeps roughly its original footprint rather than scaling up with the architecture, and is lowered to two metres. The intake and cargo courtyard are clear of enemies for planning; essential puzzle hosts remain in their encounter rooms. Enemy spawns and short patrols are validated after the stairs and puzzle geometry are built, with small clearance adjustments where needed.

The camera inherits the team's current Level 1 angle, field of view and follow offset on rebuild. It follows the active body and blends height as the player climbs. Overhead renderers are hidden while downstairs; their collision remains active. Floors block sight, possession, and projectiles. Shared shooting and its aim cue use the player's current floor height in Level 3. There is no current-room camera mask.

## Heavy breach routes

All four Heavy objectives are real, full-height walls using the shared BreakableWall and SmashAbility. B1 opens service stair B; B3 opens maintenance stair C after its shutter is released. PW is a brick partition inside the Workshop: smashing it opens a walkable service passage into Records and shuts down the West circuit. PE is a brick partition behind the SE-controlled entrance: smashing it opens the power-service corridor and bay, and shuts down the East circuit. Fixed wall sections seal the sides, including the route around the rear of the stair, so the power-bay passage cannot be entered by walking around the intact divider. These are physical collider openings, not isolated smashable boxes.

## Extraction bay

Upper-floor extraction is a separate room reached from the C landing and ordinary extraction routes. Green route lights lead through a lit gateway to a raised deck, pulsing ring and vertical beacon. The bay displays amber lights and a restore-access label while locked, then turns green and displays EXTRACTION READY after a valid route is completed. Ordinary access requires the extraction gates to be released; the maintenance alternative requires both R and B3. Entering without solving a route does not win. Only the actual player can complete the level, using the shared LevelCompleted event, GameManager, analytics and results HUD.

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

West controls stair A and the upper bridge. East controls the two ordinary extraction approaches. Linked enemies have a matching blue or purple antenna and wires. A linked watcher must be alive and unoccupied, and see a linked possessed host or a recently witnessed ejection through the same P1 cone and detection range displayed on the floor. There is no wider invisible alarm cone. Continuous suspicion warns for three seconds before a circuit latches. Walls and slabs block observation.

JW and JE disconnect their circuits and prevent future alarms, but do not clear a latched alarm. Heavy breach walls PW and PE clear and disable their respective circuits. PW also releases A and the bridge; PE releases normal extraction access. Shooting T3 does not override an active East lockdown.

SE permanently opens the service latch before PE. K6 and the East Heavy are outside that gate. R permanently opens the lower C shutter; B3 must also be smashed. Alarms do not erase charges or progress, disable pickups, relock SE/R, or repair B1/B3. The outer upper loop and service stair provide recovery routes when the usual gates are locked. Hostile projectiles cannot activate the shared targets, and one-damage bullets cannot replace Heavy smash damage.

## Checkpoints and restart

Two blue beacons offer safe-area checkpoints: the main-stair shelter downstairs and the records shelter upstairs. Approach as the free Spirit and press **F** when its charge source is ready and no enemy is close. Press **C** or Retry to reload the saved checkpoint; **R** or Restart begins a fresh attempt. Menu returns to the shared main menu.

Checkpoints preserve target and relay latches, broken walls/panels, alarm flags, pickup cooldowns, consumed bonus caches, and host positions. Reload recreates usable living hosts, including required hosts that were killed after saving. It starts an empty Spirit at the beacon with a reachable ready source. The native attempt health, time, and result counters restart; checkpoint recovery is not a continuation of the same scored timer. Checkpoints are for the current Play session, not disk saves.

## Implementation boundaries

Level-specific code lives in `Assets/Scripts/Level03`, its builder in `Assets/Editor/Level03`, and its layout/material/geometry assets in `Assets/Level03`. Every rebuild refreshes the scene shell and authored actor/puzzle templates from the team's current Level 1 scene while preserving the Level 3 asset GUID. This brings across player, camera, manager, HUD and component updates instead of reusing an older Level 3 snapshot. Actors, movement, possession, abilities, damage, targets, walls, HUD, analytics and scene flow remain shared. Level-specific difficulty and layout settings are applied afterward. The builder delegates from the existing Level 3 menu and protects its placement from legacy Guard/Heavy setup tools.

Minimal shared hooks provide reversible security-door state, floor-aware aim, pickup state capture/restore plus vertical proximity validation, slope-aware ejection clearance, optional Spirit damage protection, and optional enemy awareness/pursuit tuning. These default to the existing damage timing, instant notice, and normal movement in other levels; the relaxed settings are applied only to Level 3. Shared ability input also ignores pointer clicks over UI. Security-controlled doors ignore the automatic target listener, so unrelated targets do not create false unlock messages or camera pans. Flat-floor ejection behavior is preserved. Levels without this multi-floor layout keep their existing aim height. Level 1 and Level 2 scenes and configurations are unchanged.

## Local branch

Work is on `Omkar/level-three-alarm-compound`, synchronized with merged integration at `3b0ffaa`, including PR #46, the P1/P2 updates and shared regression fixes. The Level 2 remote branch and PR have not been modified by this work. This Level 3 work is local until a separate publish request.

## Verification

The revised PlayMode checks cover all four puzzle strategies with real shared shooting/smash abilities, each staircase in both directions with Spirit/Guard/Heavy bodies, host and pickup counts, checkpoint restoration, cross-floor protection, correct gate events, and grounded empty-Spirit ejection/expiry/host defeat. Added checks verify that the map preserves health, time and charge while suspending input, and that Guards need sustained visible contact before slower pursuit. Patrol checks use the authored enlarged layout.

Scripted visual captures are reviewed for room space, signs, both floors, and the paused route map. The optional map has concise labels rather than tutorial popups. Human playtesting is still required to tune difficulty; the aim of this revision is navigable space and time to plan.

Additional integration checks cover front-facing possession rejection with charge retention, actual Guard shots on both floors, Heavy windup cancellation, immediate search reacquisition, alarm cone consistency, shared damage recovery, and protection from attacks across floors. The earlier integration chart/HUD/movement regression fixes are included.

New checks compare the current team player/enemy/HUD component types and saved camera settings, drive the actual Heavy through both breached passages into their destination rooms, verify sealed side access, and check the extraction bay's locked/ready/completed states. Level 1 and Level 2 scenes/configurations remain unchanged. Difficulty still needs human playtesting.

Final verification in Unity **6000.3.22f1**: **190/190 PlayMode tests passed, zero failed and zero skipped**, including **27/27 Level 3 checks**. The current Level 1 camera settings (40-degree field of view and follow offset `(0, 25, -13)`) match in Level 3. Visual captures confirm the walkable breach openings, service bay, readable map labels, lit extraction gateway and shared completion screen.
