# P4: Level 01

## Scope

One graybox level: Spirit → Guard → shoot target → open door → eject → Heavy → smash wall → eject → Spirit exit.

Work starts from `integration` on `feature/level-puzzles`. The saved scene is `Assets/Scenes/Level_01.unity`. P4 owns this scene. Existing enemy, ability, possession and HUD scripts remain owned by their authors.

## Layout

The interior is 12 m wide and 36 m long. Coordinates below use Unity's world X/Z plane. Floor surface is Y = 0.1 m.

| Area | Position / bounds | Purpose |
| --- | --- | --- |
| Room 1 | Z = 0–12 | Movement instructions; Spirit starts at (0, 1.15, 8) |
| Room 2 | Z = 12–24 | Red Guard patrols between (-3, 16) and (3, 20); yellow target at (3.5, 21) |
| Target door | Z = 24 | A 3.2 m passage blocked until the target is hit |
| Room 3 | Z = 24–33 | Blue Heavy patrols between (-3, 28) and (3, 32) |
| Wall puzzle | Z = 33 | Brown breakable wall fills the central passage; solid side walls prevent walking around it |
| Exit alcove | Z = 33–36 | Spirit-only exit trigger at Z = 35.5 |

See [layout sketch](P4-layout-sketch.svg). This sketch describes the P4 layout proposal; it is not a historical screenshot.

Partitions and perimeter walls are 6 m high to prevent Spirit's jump from skipping them. Openings are 3.2 m wide to accommodate Heavy and give ejection clearance. The target intersects the Guard's projectile height. Start-room floor instructions explain controls without adding a new UI system.

`LevelExit` requires the target door to be open and the wall to have zero HP. A living Spirit must enter while unpossessed. Enemy entry cannot win. Completion fires once through the existing EventBus. Trigger stay also handles ejecting inside the exit.

## Editing

Open the project with Unity **6000.3.22f1**, then open `Level_01` and press Play. Scene changes are saved; no setup menu is required to play.

`Possession → P4 → Refine Existing Level_01` reapplies P4's layout to the integrated scene. It saves immediately, so commit scene edits first. It preserves existing prefabs and HUD. The original `Build Level_01 Scene` menu rebuilds the skeleton and removes scene refinements; run P4's refinement afterward if rebuilding is intentional.

Keep `.meta` files. Do not commit Library, build output or local editor upgrades. No new ScriptableObject configuration layer is needed for this Alpha; existing serialized scene and component values remain tunable in the Inspector.

## Manual acceptance

1. Start a fresh run. Check Spirit spawn, start-room instructions, camera and HUD.
2. Move through the first opening. Approach red Guard; check `[E] POSSESS`.
3. Press E. Move with WASD, aim toward the yellow target with the mouse, and click LMB. Check the door turns green and allows passage.
4. Pass the open door and press E to eject. Approach blue Heavy and possess it.
5. Walk toward the brown wall, face it, and click LMB within 2 m. Check the wall disappears and the passage opens.
6. Move into the exit alcove as Heavy; check the level does not complete yet. Eject and enter the exit as Spirit. Check Level Complete appears once.
7. Restart. Try walking around and jumping over both closed puzzle passages. Try reaching the exit before completing puzzles; it must not win.
8. Check forced ejection after 15 seconds, the 0.5-second cooldown, and restart. Repeat the intended flow in the final WebGL build on another computer.

Automated PlayMode coverage is in `Assets/Tests/PlayMode/LevelPuzzleTests.cs`. It exercises projectile collision, target/door wiring, Heavy's real smash ability, passage geometry, and exit rules. Automated calls do not replace a keyboard/mouse playtest or browser verification.

Validation on 2026-10-10 with Unity 6000.3.22f1: **26/26 PlayMode tests passed**, comprising 8 P4 checks and 18 existing Heavy regression tests. The editor compiled and saved the refined scene successfully. Full keyboard/mouse and WebGL playtests remain for team integration.

## Existing integration issues to coordinate

- Guard movement uses `Transform.Translate`, so it does not enforce solid-wall collision. Guard's owner should handle collision-safe movement.
- Projectiles damage any `IDamageable`, including BreakableWall. If Heavy must be the only way to break that wall, ability/puzzle owners need to agree on damage-source filtering.
- CameraFollow targets Spirit's transform, which stays behind during possession. Possession/camera owners should test visibility while controlling enemies far from the possession point.

These concern existing teammate scripts. P4 does not change their APIs or silently take over their files.

## GDD draft

### Logline

In this isometric puzzle game, players control a spirit that possesses enemies for up to 15 seconds to borrow their shooting and smashing abilities and open a path to the exit.

### Game description

Possession is an isometric puzzle game built around temporarily controlling enemies. The player starts as a spirit and moves through one short graybox level. Pressing E near an available enemy transfers control for up to 15 seconds; pressing E again returns control to the spirit.

The first puzzle uses the Guard's shooting ability to hit a target and unlock a door. The second uses the Heavy's smash ability to break a wall. Players switch back to Spirit form and enter the exit after both puzzles are complete. The Alpha demonstrates two borrowed abilities, sequential puzzle interactions, and a complete start-to-finish loop.

### Contribution draft

- Refined Level_01 into three connected rooms with a Spirit start and exit alcove.
- Retained integrated Guard and Heavy patrol routes and connected the target door.
- Sealed paths around the breakable wall and raised barriers to prevent jumping past puzzles.
- Added start-room instructions and a final exit gate requiring both puzzles and Spirit form.
- Added focused PlayMode checks; record actual results and manual playtest findings before submission.

Add actual commit links and screenshots after testing. Do not present planned or untested work as verified. Mechanic research still needs a game different from the other four team members' choices.

### External References entry

OpenAI ChatGPT / Codex was used to assist with P4 level-layout planning, C# scene-refinement and exit logic, test drafting, and GDD wording. The level uses Unity primitive geometry and built-in text; no downloaded art assets were added. Follow the assignment's staff-approval requirement for external tools and assets.
