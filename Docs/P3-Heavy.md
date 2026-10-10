# P3: Heavy enemy

## Implemented scope

- `HeavyEnemy` inherits `EnemyBase` and keeps the existing possession callbacks:
  `OnPossessed()`, `OnEjected()`, and `OnForcedEject(float hostileDuration)`.
- Starts with 5 HP and deactivates on death through the inherited damage system.
- Moves at 1.8 units/second: 0.6 times the current Guard's 3 units/second, both
  during AI movement and possessed WASD movement. If P1 changes Guard speed,
  update Heavy speed in `Awake()` to maintain that ratio.
- Patrols assigned waypoints, pauses for 0.75 seconds at each, and loops.
  Empty routes and missing waypoint references are safe.
- Uses `Physics.OverlapSphere` to detect the nearest active, living spirit
  within 10 units, including colliders on children. `Spirit Layer` filters
  detection; by default it scans all layers and checks for `SpiritController`.
- Chases the spirit while detected, stops at 1.5 units, and resumes patrol when
  the spirit leaves detection range. Chase is direct movement, with no pathfinding.
- Possession stops AI and gives movement to WASD. Manual ejection restores AI.
  Forced ejection follows the duration supplied by P2 (normally 5 seconds),
  prevents possession during that interval, and then restores normal AI.
- `CharacterController` handles collision and gravity, so the Heavy cannot
  pass through solid walls. It does not attack or inflict contact damage.
- Movement stops when paused or when the level is complete/failed.

Smash ability and breakable-wall behavior are outside this P3 assignment. Their
existing components are retained.

## Unity setup

Open this project with **Unity 6000.3.22f1**. Open `Assets/Scenes/Level_01.unity`.
The existing Heavy in Room 3 keeps its two assigned scene waypoints. Its collider
and Inspector values have been updated; the room layout has not been redesigned.

For another level:

1. Drag `Assets/Prefabs/Enemies/Heavy.prefab` into the scene.
2. Keep it on the Enemy layer (6), matching P2's possession mask.
3. Create stationary, empty GameObjects for waypoints. Keep them outside the
   Heavy hierarchy so they do not move with it.
4. Assign their transforms to the Heavy's `Waypoints` array.
5. Tune detection range, wait time, arrival distance, and chase stop distance.
   Select the Heavy to see the detection sphere and waypoint links.

The saved prefab has an empty route because scene references cannot be stored in
the prefab. An unconfigured Heavy idles but still detects and chases the spirit.
`SceneBuilder.BuildHeavy()` also creates the correct controller for new scenes.
The menu `Possession > P3 > Update Heavy Prefab and Level Component` can repeat
the collider/HP migration without rebuilding the room layout.

## P2 integration blocker in the existing repository

`PossessionController` and `SpiritController` are on the same Spirit GameObject.
`PossessionController.Possess()` calls `spirit.SetVisible(false)`, which currently
calls `gameObject.SetActive(false)`. This disables the possession controller too,
so its `Update()` cannot count down the 15-second timer or handle E to eject.

P2 should keep the possession controller active while hiding the spirit renderer,
disabling spirit movement/collision, and freezing its Rigidbody. On ejection,
move the spirit's Rigidbody to a safe, non-overlapping position before restoring
collision and movement. Simply toggling the whole GameObject will not work.
Heavy's forced-eject callback already supports P2's current order: the spirit
can still be hidden when `OnForcedEject()` is called and be shown immediately after.

This issue is documented for P2; this branch does not change their controllers.
The tests cover the Heavy callbacks directly, rather than claiming the full
15-second possession loop works in the existing level.

## Verification

Unity's Test Runner contains `Possession.Tests.HeavyEnemyTests` under PlayMode.
The runtime assembly definition and Test Framework package allow these tests
to run without putting test code in player builds.

Validation on October 9, 2026: **12 PlayMode tests passed, 0 failed** in Unity
6000.3.22f1, including the saved prefab and the existing Level_01 scene.

Run from the project root on this Mac:

```sh
/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform PlayMode \
  -testFilter Possession.Tests.HeavyEnemyTests \
  -testResults "$PWD/Logs/p3-playmode-results.xml" \
  -logFile "$PWD/Logs/p3-playmode.log"
```

Close this project's Unity editor before running the batch command. Tests cover
HP/death, speed relative to Guard, waypoint arrival/wait/looping, missing routes,
child-collider detection, returning to patrol, possession/ejection callbacks,
hostility expiry, stopping distance, wall collision, layer filtering, pausing,
the saved prefab, and the Heavy in Level_01. Actual keyboard input and the shared
possession timer still need a manual playtest after P2 fixes the blocker above.

## External references for the GDD

- OpenAI Codex: AI assistance with P3 implementation, tests, and these notes.
  Review and cite this assistance in the GDD's External References section.
- Unity 6.3 API documentation:
  [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html)
  and [Physics.OverlapSphere](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphere.html).
- No external art, models, or audio were added for this work.
