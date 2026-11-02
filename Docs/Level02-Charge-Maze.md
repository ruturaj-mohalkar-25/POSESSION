# Level 2: Hunted — Charge Maze

This replacement Level 2 uses seven main rooms, three short dead ends, and two independent routes through a dim, blue-gray single-floor compound. The original scene path and campaign slot are preserved. Levels 1 and 3 are unchanged.

## Open and play

Use Unity 6000.3.22f1 and open `Assets/Scenes/Level_02.unity`, then press Play. The scene is also available through the existing campaign menu. To regenerate the authored scene, stop Play Mode and select **Possession → Level 2 → Build Hunted Charge Maze**. The regular Level 2 builder delegates to this builder too.

Move with WASD, press E to possess/eject, and use LMB for the host ability. Start with 3 HP, 240 seconds, and no possession charge. The existing possession timer remains 10 seconds.

Enter the courtyard to enable **C · Retry Courtyard**. C reloads the attempt at the courtyard entrance, before its shard; it restores all enemies, sources, gates, and walls and starts the Spirit empty. R restarts from the original entrance. Both actions are also buttons; Menu returns to the shared main menu.

## Charge rules

- Only a living, free Spirit can collect a green shard. Capacity is one charge.
- E spends the charge only on a successful possession of a valid, living host within range and line of sight. Failed input, cooldown, and blocked targets do not spend it.
- Movement, shooting, and smashing cost nothing more while in the body.
- Manual ejection, expiry, and host defeat return an empty Spirit. Collect again before taking another body.
- C1 in the courtyard, C2 in the north transfer room, and C3 in the south transfer room reform eight seconds after collection. C4 in the archive dead end is a one-use bonus per attempt. Full or occupied players cannot waste a source.
- Open gates and broken walls stay changed during the attempt. Either completed route enables the exit, which accepts the actual player in any form without another charge.

## Routes

| Route | Sequence |
| --- | --- |
| North | Courtyard C1 → Guard → shoot TN → gate GN opens → north transfer C2 → Heavy → smash BN → exit |
| South | Courtyard C1 → Heavy → smash BS → south transfer C3 → Guard → shoot TS → gate GS opens → exit |

Each route needs at least two successful possessions and a recharge between hosts. The other route is optional. There is no transfer-room shortcut between the routes. Dead ends provide cover, misleading footprints, an optional bonus, and a sunlit hiding alcove; entering a dead end does not itself consume a charge.

Five Guards and three Heavies use the shared patrol, chase, possession, ability, damage, and visual systems. Normal pursuing enemies can damage the free Spirit in this level after a 1.2-second uninterrupted windup. They wait 3.2 seconds between their own hits and share a three-second contact-damage grace period across the level, so nearby ordinary enemies cannot stack immediate hits. Retreating or breaking line of sight cancels the windup. Forced-eject hostility remains active, with a two-second initial delay, three-second shot/smash cooldowns, and a 1.1-second Heavy windup. Enemy bullets cannot solve target puzzles.

## Integration scope

Level-specific runtime code is in `Assets/Scripts/Level02`; the builder is in `Assets/Editor/Level02`. Shared changes are limited to optional charge validation, the existing possession cue, dead-host ejection clearance, hostile-projectile target protection, and protecting the new placement from legacy setup tools. Levels without a charge component retain their possession rules.

The maze reuses the shared HUD, health/time display, following camera and wall occlusion, Guard/Heavy actors, shooting/smash abilities, target/door/wall visuals, analytics, completion screen, and scene flow. A small local status strip explains charge readiness without a popup. The camera retains its previous 57-degree angle, (0, 17, -11) follow offset, and 45-degree field of view. It uses the normal shared following behavior without room masking. Blue-gray walls, charcoal floors, and softer cool lighting replace the pale daytime palette; interactive puzzle colors and charge cues stay recognizable. The shared objective counter is hidden because either route can finish the level.

## Verification

The final dedicated PlayMode run passed all 12 Level02ChargeMazeTests, including the attack windup/shared grace period. Coverage includes layout and actor counts, invalid possession and charge handling, pickup cooldowns, actual expiry and host defeat, ordinary enemy damage, hostile projectile rejection, both timed routes using the actual shooting/smash abilities, checkpoint restoration, and exit validation. Route tests use the real possession duration and movement speed, including the shared possessed-Heavy slowdown.

The full PlayMode run reported 140 passes out of 145. Five existing integration failures remain: the repeated bar-chart display test, the Heavy movement-ratio test, and the HUD color checks in Levels 1–3. These were also reproduced on the unchanged integration baseline. The dedicated suite was rerun after the camera and combat tuning.

Scripted visual captures were checked at 16:9 and 4:3, including charge states, the following camera, wall occlusion, host abilities, and the shared completion screen. This is functional verification, not a novice-player balance study. Human playtesting should still check pursuit pressure, shard visibility, wrong-turn recovery, and the four-minute limit. The new pressure values are a playtest starting point, not a claim that the difficulty is finalized.

## Branch and integration

The replacement is on `Omkar/level-two-charge-maze`, based on integration commit `f0d62d3`. The separate tutorial revision is not included. The Level 2 scene keeps its existing asset GUID and campaign slot. Teammates can use the scene directly or regenerate it through the Level 2 builder.
