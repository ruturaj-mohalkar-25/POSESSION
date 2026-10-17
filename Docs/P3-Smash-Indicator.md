# P3: Heavy smash direction indicator

## Behavior and testing

Open Level_01 in Unity 6000.3.22f1 and press Play. Approach the Heavy in Room 3
and press E. An amber ground arc and arrow show the Heavy's current facing and
smash distance. Turn with WASD; the guide follows the body. Left-click to smash:
the guide dims during cooldown and returns to amber when the ability is ready.
Press E again or let the possession timer expire; the guide disappears.

Check the same behavior after Restart and after entering a level from MainMenu.
The guide also disappears on death or level completion/failure. It is a range
and direction aid; the arc does not describe a damage cone or guarantee a hit
through obstacles. Attack behavior is unchanged.

## Integration

- New `HeavySmashIndicator` and `HeavySmashIndicatorInstaller` scripts handle
  visuals and installation. Saved scenes, enemy AI, prefabs, EventBus, and level
  builders are unchanged. The installer handles scene loads, EventBus resets
  during restart, and Heavies spawned later and then possessed.
- The only existing runtime script edit is the read-only `SmashAbility.Range`
  property. Keep this property when changing smash hit detection; it exposes the
  existing serialized `smashRange`, so gameplay and the guide use the same value.
- Range is measured in world units and does not grow with Heavy's model scale.
  The arrow follows body facing, matching the current forward-facing smash.
- The guide uses `Ability.CanUse` for its ready/cooldown color. When P2 adds ammo,
  readiness will also reflect exhaustion if `CanUse` includes remaining uses.
- A small unlit shader and material in `Assets/Resources/P3` keep the guide's
  material available in builds. The guide has no colliders, damage logic, input
  handling, or per-frame scene searches.

## Validation on October 16, 2026

Seven new PlayMode tests passed in Unity 6000.3.22f1. Coverage includes actual
manual/forced possession transitions, Guard exclusion, world-space range after
movement/rotation/scaling, range changes, cooldown color, disable/re-enable,
death/end events, scene installation, EventBus reset, and spawned Heavies.

The full suite reported 38/40 passing. A separate run of unchanged main at
`7df1859` reproduced both failures (31/33 passing):

- `HeavyEnemyTests.PatrolMovesAtSixtyPercentOfGuardSpeed`: the spawned Guard
  barely moves in the batch test, failing the relative-speed assertion.
- `LevelPuzzleTests.HeavySmashBreaksWallAndOpensPath`: the wall is destroyed,
  but the subsequent clear-path raycast still hits something.

These existing failures need coordination with the enemy and puzzle owners.
The new tests are in `Possession.Tests.HeavySmashIndicatorTests`. Run that
fixture from Window > General > Test Runner > PlayMode. Keyboard/mouse and WebGL
acceptance checks remain for team testing.

A graphics-enabled run also passed all seven indicator tests plus a temporary
rendering check (8/8). The check rendered the actual possessed Level_01 Heavy
with the bundled shader; its image is saved locally in
`Logs/p3-smash-preview.png`. The temporary preview code stays outside the working
repository.
