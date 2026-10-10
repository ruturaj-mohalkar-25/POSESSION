# P3: Heavy enemy

## Implemented scope

- `HeavyEnemy` inherits `EnemyBase` and keeps the existing possession callbacks:
  `OnPossessed()`, `OnEjected()`, and `OnForcedEject(float hostileDuration)`.
- Starts with 5 HP and deactivates on death through the inherited damage system.
- Moves at 1.8 units/second: 0.6 times the current Guard's 3 units/second, both
  during AI movement and possessed WASD movement. If P1 changes Guard speed,
  update the Heavy prefab/scene Move Speed in the Inspector to maintain that ratio.
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

## P2 integration

Merged `origin/integration` at `015753c` into the local P3 branch. This brings in
P2's spirit visibility fix, possession prompt events, Guard ability input,
projectile prefab/wiring, hostile chase, Inspector-configurable stats, and
camera-follow component.

The previous possession blocker is fixed: hiding the spirit now disables its
renderer/collider and makes its Rigidbody kinematic while keeping the GameObject
and possession controller active. The timer and ejection can continue running.
The Heavy retains its collision-aware movement and uses the shared LMB ability
handler, preserving P2's ability activation without restoring transform-based
movement through walls.

Open the merged Level_01 scene to test the complete sequence: possess Guard,
shoot the target, eject with E, walk through the unlocked door as the visible
Spirit, approach Heavy, possess it, move with WASD, and eject again. For a focused
P3 test, move the Spirit directly to `(0, 1.1, 27)` during Play mode.

The imported camera-follow script targets the Spirit. Tracking the possessed
enemy instead of the hidden Spirit is not implemented by that script.

## Ejection physics fix

Ejection previously placed the Spirit at the enemy's exact center and enabled
its collider there. The overlapping Heavy and Spirit could push the Spirit
upward during physics resolution. The earlier integration tests used a frozen
Spirit Rigidbody and did not catch that interaction.

Ejection now searches for a grounded, unoccupied position beside the enemy,
including alternate sides when a wall blocks the first choice. It can return to
the pre-possession position if that is clear. If there is no safe position, it
keeps the body possessed until space becomes available. The Spirit's Rigidbody
is teleported while hidden, then restored with zero linear/angular velocity and
reset grounded state. This applies to manual and forced ejection.

A regression test failed on the previous code because Spirit/enemy horizontal
separation was zero. After the fix, **18 PlayMode tests passed, 0 failed**, including
a gravity-enabled Spirit, a scaled Heavy, blocked ejection positions, and stale
jump velocity. The forced-ejection test uses a short timer but checks real physics
for the same ejection path used at 15 seconds.

## Verification

Unity's Test Runner contains `Possession.Tests.HeavyEnemyTests` under PlayMode.
The runtime assembly definition and Test Framework package allow these tests
to run without putting test code in player builds.

Initial P3 validation on October 9, 2026: **12 PlayMode tests passed, 0 failed**
in Unity 6000.3.22f1, including the saved prefab and the existing Level_01 scene.

After merging P2: **15 PlayMode tests passed, 0 failed**, including three new
checks for configurable stats and the shared possession/ejection loop. Validation
ran in a separate copy while the working project stayed open in Unity.

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
the saved prefab, and the Heavy in Level_01. Additional integration tests cover the shared possession controller staying
active, its timer counting down, manual and forced ejection, and Heavy hostility
expiring. They use shortened timer values to keep the run quick. Actual keyboard
and mouse input still need a manual playtest in the merged scene.

## External references for the GDD

- OpenAI Codex: AI assistance with P3 implementation, tests, and these notes.
  Review and cite this assistance in the GDD's External References section.
- Unity 6.3 API documentation:
  [CharacterController.Move](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html)
  and [Physics.OverlapSphere](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapSphere.html).
- No external art, models, or audio were added for this work.
