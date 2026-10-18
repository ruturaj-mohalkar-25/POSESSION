# P3: Alpha Heavy setup

P4's `feature/level-puzzles` at `fa1717d` is merged locally with the existing
smash-indicator work. P2's alpha branch at `cd1685b` is also merged locally.
P5's alpha UI branches and the new Guard placements are not merged here.
This is a focused Heavy test version, not the final integrated Alpha build.

## Saved local levels

| Scene | Heavy count | Speed | Walls | Patrol routes |
| --- | --- | --- | --- | --- |
| Level_01 | 1 (existing) | 1.8 | Existing | Existing route retained |
| Level_02 | 1 | 2.0 | 1, at Z=27 | (-3,20) to (0,22) |
| Level_03 | 2 | 2.2 each | 2, at Z=24 and Z=36 | (-2,19) to (2,21); (-2,31) to (2,33) |

Each new Heavy has 5 HP, a CharacterController, the Puzzle-layer smash mask,
contact damage, and two stationary waypoints. The runtime smash indicator
attaches automatically. The Heavy prefab and P4's layout/Guard spawn markers
are preserved. New enemy instances and routes live under `P3_Heavies`.

Wall objects are supplied by P4's builders; the setup verifies they exist rather
than duplicating or moving them. Their current 2 HP matches the Heavy's 2 damage.
The Heavy spawns just above the floor so its scaled capsule does not start inside
it. The Level_01 scene, its speed and its patrol are unchanged.

## Test locally

1. Open `Assets/Scenes/Level_02.unity` or `Level_03.unity` in Unity 6000.3.22f1.
2. Press Play. P1's Guards are still spawn markers in this version.
3. For a focused Heavy test, choose **Possession > P3 > Testing > Unlock Guard
   Door (Play Mode)**. This editor-only helper hits the existing targets through
   their normal API. It does not exist in a player build or save puzzle progress.
4. Walk to a Heavy, press E, turn with WASD, and check the amber smash guide.
5. Approach its wall, face it and click LMB. Eject and inspect the Spirit's
   position. In Level_03 repeat with the second Heavy and exit wall.
6. Let possession expire near a wall; check grounded ejection, the guide hiding,
   temporary hostility, and contact damage. Restart and repeat.

These focused checks do not replace testing the Guard puzzles, HUD, ammo,
next-level flow, or the final WebGL build after team integration. In particular,
the Heavy now has one smash per possession from P2's ammo update. Eject and
repossess to replenish it; a depleted guide remains dim after cooldown expires.

## Rebuild and merge workflow

- **Possession > P3 > Configure Heavies in Current Level** applies only the P3
  setup to the open level and leaves it ready to save. Reapplying does not add
  duplicate bodies or routes. It resets P3 positions/routes to the listed preset.
- **Possession > P3 > Prepare Local Level 02 and 03** builds missing P4 scenes,
  configures Heavies and saves those levels. Existing scenes are configured, not
  rebuilt. Missing configs are created only when no existing level configs would
  be overwritten.
- P4's rebuild commands replace a whole scene, including enemy placements. Run
  the P3 setup afterward. Keep the setup script as the P3 source of truth so
  teammates can reapply these placements to their integrated scene versions.
- The generated scenes, config assets, metadata and build-scene list are local
  testing artifacts. Coordinate their final versions with the scene owner before
  merging shared scene changes.
- One imported P4 compile error was fixed: `FindObjectsByType` now supplies both
  `FindObjectsInactive.Include` and `FindObjectsSortMode.None` for Unity 6.3.

## Validation on October 18, 2026

Unity 6000.3.22f1 successfully built and saved both levels in an isolated copy.
The saved scenes/configs were then copied into the working repository.

- **6/6 new PlayMode tests passed**: counts, HP, speed, route references, patrol
  movement, wall smashing in sequence, manual/forced ejection in puzzle passages,
  against perimeter walls and in divider corners, and indicator visibility.
- **4/4 new EditMode tests passed**: idempotent setup for both levels, unchanged
  layout/Guard markers, missing-dependency protection, and preserving Level_01's
  existing patrol. Editor tests use isolated preview scenes and close them after
  each check ([Unity API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/scenemanagement/editorscenemanager/newpreviewscene)).
- Full PlayMode suite: **44/46 passed**. The same two failures previously
  reproduced on unchanged main remain: `PatrolMovesAtSixtyPercentOfGuardSpeed`
  and `HeavySmashBreaksWallAndOpensPath`. No new PlayMode failures were introduced.

Manual all-level QA and WebGL testing remain pending team integration.

## Material correction on October 18

The original saved test scenes inherited empty material slots from the puzzle
and Spirit prefabs, causing pink objects in the Editor. The P3 setup now fills
only missing scene-instance materials using the existing P5 assets. Already
assigned materials and the shared prefabs are preserved. Both saved test scenes
were repaired. This is basic test-scene readability, not P5's full visual pass.

The focused PlayMode checks passed 13/13, including checks for missing material
references. Editor setup checks also cover retaining assigned materials and
leaving the original prefab material unchanged.

The separate `FMOD failed to switch back to normal output` error was present in
the Editor log after reload. No project audio settings were changed to hide it;
save and restart the Unity Editor to retry audio initialization.

## Local P2 alpha integration

Merged `origin/dev/ruturaj-alpha` at `cd1685b` locally. The only merge conflict
was in SmashAbility: both P2's SphereCast/one-use setup and P3's range API were
retained. The range guide now includes the cast sphere's radius in its maximum
forward reach (2.8m with the current 2m cast range and 0.8m radius).

P2's camera now follows the possessed enemy and returns to Spirit on ejection.
Ammo, progression, rating and in-memory analytics code are also available. This
does not supply Guards or the new levels' HUD. The latest P4 branches contain
the same builder content already integrated; their new commit IDs reflect
rebasing rather than additional placements.

Extra regression coverage checks guide behavior after ammo depletion and refill,
and verifies camera targets during the real Level_02/03 possession transitions.

Validation after this merge: 14/14 indicator and Level_02/03 checks passed.
Three additional contact-damage checks passed, one per level: manual ejection
causes no damage, while forced ejection allows the nearby hostile Heavy to
damage the Spirit after its grace period. The full suite before these three
additional checks reported 45/47 passing, with only the two baseline failures
listed above. These automated checks do not replace keyboard/mouse or browser QA.
