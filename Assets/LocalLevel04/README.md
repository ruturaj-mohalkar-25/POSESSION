# Level 04 — The Relay Run

A 66 × 40 metre 3D maze built from Omkar's layout sketch. The scene uses the same gameplay components as Levels 01–03: SpiritController, PossessionController, GuardEnemy, HeavyEnemy, ShootAbility, SmashAbility, Target, Door, BreakableWall, LevelExit, GameManager, AnalyticsTracker and HUDManager.

## Play

Use Unity **6000.3.22f1**. Stop Play mode, select **Possession → Local Experiments → Open Level 04**, then press Play. The scene is `Assets/LocalLevel04/Level_04_Local.unity`.

WASD moves, Space jumps as Spirit, E possesses/ejects, mouse aims and LMB shoots as Guard. Face the wall and LMB smashes as Heavy. Use the shared end-screen buttons to restart or return to the main menu. The previous custom minimap, pause screen and R shortcut have been removed.

## Route

1. Possess the west Guard and shoot the north-west target to open the first Door.
2. Possess the central Heavy and smash the cache BreakableWall.
3. Bring a Guard into the cache and shoot its target to open the east relay Door.
4. Follow the long route around the east baffle, possess the second Heavy and smash the final BreakableWall.
5. Eject and enter the finish zone as a living Spirit.

Shared waypoint patrol and chase behavior replaces grid navigation. Enemies use their normal damage, death, ability limits and forced-ejection hostility. They can be killed; use the shared restart button if a required body is lost. There are no local enemy, ability, gate, navigation or HUD runtime classes.

## Configuration and rebuilding

Edit `Assets/Config/LevelConfig_Level_04.asset`, then choose **Possession → Local Experiments → Rebuild Level 04** to bake its values into the scene, following the existing P4 configuration workflow. Existing config values are preserved when rebuilding. Initial tuning is 120 seconds, 10-second possession, 10-metre Guard detection, and 60/85/110-second par times. Enemies inherit the shared prefab defaults (Guard speed 3, Heavy speed 1.8, Heavy HP 5).

The builder copies the HUD from Level_01 and configures its optional shared objective list for the four puzzle gates. Levels without that list retain the existing three-step HUD behavior. The muzzle flash remains enabled; SimpleFX now disables the decorative burst collider immediately instead of waiting for deferred destruction.

Level_04 is registered at the end of Build Settings so shared restart and level flow work in Editor and builds. It remains outside the main-menu level selector while under review. Building the scene does not regenerate Levels 01–03. The proposal targets the separate Omkar/level04-review branch; it does not merge into integration.

## Verification

Focused PlayMode coverage exercises the actual shared shooting/smash route, objective and HUD events, analytics, enemy damage/death, patrol/chase/possession, forced-ejection attacks, safe ejection, Spirit-only completion, normal scene restart, and the decorative collider fix. Controlled actor placement checks integration; human playtesting is still needed to tune the larger route's time budget.
