# Enemy Vision, Attack and Eject

Beta rules for how Guards and Heavies see, chase and attack the Spirit, and where the Spirit lands when it leaves a body. They apply in every level and the tutorial. The rules live in the shared enemy code, so no scene or prefab needs editing to get them.

## Vision cone

- Each enemy sees through a cone in front of it: `visionAngle` (default 45°) out to its `detectionRange`, with a clear line of sight. `EnemyBase.CanSee` checks all three.
- Detection, chase and attack all go through this one check. An enemy ignores a Spirit that is beside or behind it.
- The cone is drawn flat on the ground under each enemy (`VisionConeView`, added at runtime by `VisionConeInstaller`). It is clipped where walls block the view, faint red while patrolling, stronger red while chasing or hostile, and hidden while the enemy is possessed.

## Possess from behind

- E possesses only when the Spirit is in range, has line of sight to the enemy, and is outside that enemy's cone. `PossessionController` checks range and line of sight, and `EnemyBase.CanBePossessedFrom` checks the cone. Approach from behind or the side.
- The green "possessable" ring and "E" badge (`EnemyReadability`) follow the same rule, so the cue only shows when E will work.
- In Level 2 the Spirit also needs a charge (see `Level02-Charge-Maze.md`).

## Attack and search

- A patrolling enemy that spots the Spirit chases it and attacks after a short warning blink (`EnemyContactDamage.spotReactionTime`, 0.6 s). It does not have to be hostile first.
- A Guard shoots, a Heavy winds up and smashes, and a body with neither ability uses a touch hit.
- When the enemy loses sight, it walks to the last place it saw the Spirit for `loseSightSearchTime` (1.5 s), then returns to its nearest patrol waypoint. Seeing the Spirit again during the search restarts the chase.
- Forced-eject hostility still works as before. The host chases and attacks for `hostileTime`, then becomes possessable again.

## Eject (PR #52)

- On eject, the Spirit is placed behind the body first, then at the sides, and in front only when nothing else is free. Where there is room, it lands outside the cone of the body it left.
- The body the Spirit just left waits `ejectGraceTime` (2 s) before it can attack. This applies to manual and forced ejects. A forced eject uses `firstAttackDelay` instead when that is longer. Other enemies are not affected.
- A chasing or hostile enemy turns to face the Spirit even when it is already within its stop distance, so it can aim at a Spirit standing right behind it.
- If a body is walked into the exit and then ejected, the Spirit can land just behind the exit trigger and needs one step forward.

## Tuning

| Field | Where | Default |
| --- | --- | --- |
| `visionAngle` | `EnemyBase` | 45° |
| `detectionRange` | `EnemyBase` (set per enemy in the scenes) | 10 |
| `loseSightSearchTime` | `EnemyBase` | 1.5 s |
| `spotReactionTime` | `EnemyContactDamage` | 0.6 s |
| `firstAttackDelay` | `EnemyContactDamage` (set per scene) | 0.75 s |
| `ejectGraceTime` | `EnemyContactDamage` | 2 s |

Cone width and detection range can be tuned per enemy in the Level and Tutorial scenes. Those are scene edits, so they belong to whoever owns that scene.

## Tests

- `VisionConeTests`: cone gating, possess-from-behind, and line of sight.
- `EnemyAttackTests`: attack on sight, no attack without sight, no attack while possessed, lose-sight search and resume, eject placement behind the body, and the post-eject grace.
- Any test that expects an enemy to notice the Spirit must turn the enemy to face it first, because vision is directional.
