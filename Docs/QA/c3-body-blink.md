# C3 earlier, progressive possession blink

This follow-up restores the warning requested in closed PRs #63 and #64 on top of the ground countdown merged in #65. It starts from integration `6b87fb0`, including the tutorial changes from #76.

## Behavior

- The warning starts at **65% remaining**, giving the player time to react before the final seconds. For a 10-second possession the warning window is now about **6.5 seconds**, compared with 3.5 seconds before; for 12 seconds it is 7.8 seconds instead of 4.2 seconds.
- Brief **1 Hz** pulses grow smoothly toward **6 Hz** as time runs out. The first pulse arrives when the warning band is entered, without waiting through a full slow cycle. Cadence remains continuous as urgency changes.
- The body stays visible for about **88% of each early cycle**, gradually reducing to **60% near expiry**. This keeps the early warning gentle and the white body easy to track.
- The body keeps its white possession material. Hats, tools, ammunition, the aim line and the ground countdown keep their existing presentation.
- The warning changes only the body renderer's visibility flag. It does not replace materials, write material property blocks, disable the renderer component or touch colliders.
- Pause freezes the blink. Ejection, expiry, death, a disabled host, a disabled warning and level-end events restore the original visibility state.
- One warning is installed on each possession controller when a scene loads. It also handles enemies spawned later, with either a root body renderer or a child mesh. No scene, prefab or enemy AI changes are needed.

The old blink selected only child meshes, skipping the root body used by the saved levels. It also wrote the enemy's original colour over the newer white possession material. Both issues are avoided here.

## Validation

Verified November 12, 2026, with Unity **6000.3.22f1**:

- Focused warning and countdown checks: **30/30 passed** (15 blink checks and 15 existing countdown checks).
- PlayMode regression suite, excluding the two obsolete tutorial fixtures described below: **248/248 passed, 0 failed, 0 skipped**.
- Native Guard and Heavy capture check: **1/1 passed**, recorded alongside that suite.

Both new reaction-time and progression checks fail against the previous runtime and pass after this update. In the native 10-second recordings, the first visible blink moved from **3.27 seconds remaining to 6.47 seconds remaining**, giving about **3.2 seconds of additional notice**. The clips use the same timer and frame rate in both builds.

The warning checks cover visible notice before half the timer remains for both Guard and Heavy, progressively faster early/middle/urgent bands, body visibility between pulses, material and property-block preservation, equipment visibility, both ejection types, real expiry, invalid and replenished timers, pause, disable/re-enable, death, level end, unrelated enemy events, runtime-spawned hosts and installation across every saved level and Tutorial.

To reproduce, open **Window → General → Test Runner → PlayMode** and run `PossessedBodyBlinkTests` together with `PossessionTimerTests`.

The broader run selected all tests matching this filter, with `TutorialTests` and `TutorialEffectsTests` explicitly excluded:

```text
^Possession[.](Tests[.](?!Tutorial(Effects)?Tests)|Warehouse[.]Tests[.])
```

The unfiltered suite currently stalls in `TutorialEffectsTests.CompactTutorialNeverPausesForAHudSpotlight`: it waits on scaled time while the tutorial's intro popup has paused the game. A separate checkout of unchanged integration `6b87fb0`, without the blink code, also fails `TutorialTests.TutorialAutoStartsWithoutAModalOrPausedCoaching` because the lesson no longer starts before the intro is dismissed. These fixtures still expect the earlier automatic, unpaused tutorial. The full suite is not reported as passing.

## Native previews

Captured from Unity's gameplay camera while the real 10-second possession timer counts down and forces ejection. The clips show normal possession, earlier brief pulses, increasing urgency and restored visibility after eject. Enemy movement and attacks are held still to make the warning readable. The ground countdown keeps its existing colours and only the body blinks. GIF previews are sampled at 15 fps from the native 30 fps capture.

Guard:

![Guard body blink with the ground countdown](c3-blink-guard.gif)

Heavy:

![Heavy body blink with the ground countdown](c3-blink-heavy.gif)
