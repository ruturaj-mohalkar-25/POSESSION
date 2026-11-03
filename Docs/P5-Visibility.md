# P5: wall visibility

## Local setup

- Unity: **6000.3.22f1**.
- Project: `/Users/omkarshinde/Documents/POSESSION-P5`.
- Branch: `Omkar/p5-visibility-data`.
- Current integration base: `8bdc3dc`.

The separate checkout preserves the Ghost/Flying prototype in the original POSESSION project. Wall visibility adds new files only. It does not require rebuilding levels or editing scene, prefab, camera-follow, HUD, enemy, or possession files. The separate floor-text task removes generation from four builder files; see [P5 timing and floor-text notes](P5-Timing-and-Floor-Text.md).

## Behavior

`WallOcclusionInstaller` adds `WallOcclusionFader` to the main camera when a gameplay scene loads. The fader follows the Spirit, or the current possessed host. Structural walls that block the body become transparent and return to their original materials when the view clears. Disabling the component restores all materials immediately.

The camera pose, colliders, layers, and movement remain unchanged. A transparent wall still blocks movement and projectiles. Doors, targets, breakable walls, and characters keep their existing materials so their gameplay feedback remains under the shared systems' control.

Existing geometry is recognized through wall, divider, cover, pillar, or column names, including a parent group such as `Walls`. For custom geometry, add `VisibilityOccluder` to the collider object or a parent. Turn off its **Fade When Blocking** option to exclude it. Non-trigger colliders are required for obstruction detection.

Defaults on the runtime camera component:

| Setting | Value |
| --- | --- |
| Obstructing opacity | 0.18 |
| Fade duration | 0.12 seconds |
| Restore duration | 0.20 seconds |
| Probe radius | 0.25 metres |

The shader targets the project's built-in render pipeline and copies wall color and texture into temporary materials. Those materials are released after restoration. Custom effects such as emission or normal mapping are not reproduced during the short fade.

## Test in Unity

1. Add **POSESSION-P5** to Unity Hub and open it with **6000.3.22f1**. This is a different project folder from the Ghost/Flying prototype.
2. Open `Assets/Scenes/Level_01.unity` and press Play.
3. Walk close to a wall or divider on the camera-facing side of a room. The wall should fade when it hides the Spirit and become opaque again after moving away.
4. Possess a Guard, then a Heavy where available. Repeat near walls; the fade should track the body you control. Check manual ejection and timer-driven ejection.
5. Walk into a faded wall and shoot toward it. Its collision should still block movement and shots.
6. During Play Mode, select **Possession → P5 → Visibility → Toggle Wall Fade (Play Mode)** to compare enabled and disabled views. Turning it off should restore walls immediately.
7. Repeat in Level_02 and Level_03, including tight corners. Check any camera pan and restart/scene transition alongside your teammates' changes.

The component appears on **Main Camera** during Play Mode. There is no extra scene to open.

## Verification

Unity PlayMode results on the updated integration base: **85 passed out of 86**, including **all 17 wall visibility tests** and **15 timing/floor-text tests**. The visibility tests cover material restoration, possession target switching, multiple blockers, camera-inside-wall detection, pause behavior, scene installation, and preservation of physics and external material/property-block changes.

The remaining failure is `HeavyEnemyTests.PatrolMovesAtSixtyPercentOfGuardSpeed`. That test compares CharacterController displacement under headless frame timing; the spawned Guard retains a minimum movement threshold while the Heavy fixture clears its threshold. A second run with an attempted editor 60 Hz setting still failed. No Heavy behavior or shared test was changed for this feature. This is not a fully passing regression run; confirm the speed ratio in the normal editor and resolve the shared test separately.

Before/after images were rendered from the existing Level_01, Level_02, and Level_03 scenes. Each comparison found an obstructing wall; the fade shader compiled without errors. These renders verify visibility, not completion of the manual gameplay checklist above.

## P5 task status

- Wall visibility implemented and covered by automated tests.
- Floor-text generation removed; saved levels also hide legacy floor text at runtime.
- First-possession and objective-step timing implemented using existing shared events, with JSON/CSV exports and a results window.
- Manual playtesting with the results window and the team's camera-pan behavior remains.

The feature is developed on `Omkar/p5-visibility-data` for review against `integration`.
