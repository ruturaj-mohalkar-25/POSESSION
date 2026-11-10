# C3 possession and beta HUD validation

Verified November 9, 2026, using Unity **6000.3.22f1**.

PR #65 is synced with integration `f56fb49`, including the expanded campaign, Warehouse, shared presentation and sliding-door updates from PRs #67, #69 and #71.

## Results

- Final full PlayMode suite: **251 passed, 0 failed, 0 skipped**.
- All countdown, HUD and hostile-projectile regressions pass alongside the latest campaign, tutorial, movement, combat, charge, shield, patrol and door checks.
- Cross-level countdown and typed-ammunition checks include the current Warehouse scene, all campaign stages, the legacy Level_01 scene and Tutorial.
- Current campaign scenes and both projectile implementations match integration exactly. Existing gameplay fixes and layouts are retained.

## Behavior covered

The text-free ring lies flat on the floor around the white possessed body's feet, retains equipment colours, and changes from green through amber to red without flashing. Its hint keeps a gap during tutorial zoom. Pause/re-enable, level-end events, offscreen hosts, material restoration and aim-line disposal have regression coverage.

The campaign keeps one shared screen HUD and its owned world-space countdown canvas. Checks distinguish the latest campaign camera from the legacy/tutorial camera and respect paused and level-end action blocking. Ammo stays inside screen edges on overlay and camera canvases. No tests are ignored or skipped.

## Reproduce

Open the project in Unity 6000.3.22f1, then use **Window → General → Test Runner → PlayMode → Run All**.

## Native previews from the earlier ring build

These staged captures demonstrate the ring's presentation before the latest campaign integration. They do not show the current expanded layouts or movement tuning. The current integration sync was checked with the full PlayMode suite above.

Guard with the ground-level circular timer, typed ammunition and bottom-centre status row:

![Guard possession](c3-guard.png)

Heavy with low remaining possession time:

![Heavy low-time state](c3-heavy-low.png)

Tutorial spotlight preserves the circle and hint spacing:

![Tutorial clock spotlight](c3-tutorial-clock.png)
