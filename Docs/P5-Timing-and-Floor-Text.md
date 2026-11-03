# P5: visibility and playtest timing

## Local project

- Unity **6000.3.22f1**.
- Project `/Users/omkarshinde/Documents/POSESSION-P5`.
- Branch `Omkar/p5-visibility-data`.
- Updated from merged integration commit `8bdc3dc` (PRs #36 and #37).

The Ghost/Flying prototype remains in the original POSESSION checkout. PR #35 is not merged into this branch.

## Floor-text cleanup

Removed the `StartInstructions` and `Sign_*` creation calls from the Level_01 designer and the Level_02/03 builders, and removed their unused FloorSign helpers. Rebuilding these levels will no longer generate floor instructions.

`LegacyFloorTextCleanup` hides the MeshRenderer of those legacy TextMesh objects when an existing saved scene loads. It preserves the GameObjects and any colliders; floating `WorldLabel` objects and HUD elements are excluded. This avoids rewriting the shared scene YAML just to hide old floor text.

## Timing metrics

`P5TimingInstaller` adds a tracker to the scene's GameManager at load time. It uses the existing EventBus without changing the shared HUD or AnalyticsTracker.

| Metric | Definition |
| --- | --- |
| Time to first possession | Gameplay seconds from level start to the first successful `Possessed` event, captured once per attempt. A value of `-1` means the player never possessed; zero is a valid immediate possession. |
| Time on each objective step | Seconds between level start and the first completed objective, then between subsequent objectives. `DoorOpened`, `WallBroken`, and successful exit completion close steps. |

The endpoints match how the current HUD advances objectives. In Level_02, shooting only one of two targets does not finish the door objective; unlocking the door does. A typical Level_03 report contains door unlock, first wall, second wall, and exit intervals. First-possession time is a separate metric within the opening interval; do not add it to objective durations.

The GameManager's elapsed gameplay clock excludes pauses. A scaled-time fallback supports standalone tracker use. The tracker deduplicates each door/wall object, ignores puzzle objects in other scenes, and finalizes an attempt only once. It observes GameManager's failure state as well as events because the current countdown does not broadcast `LevelFailed`.

Finishing produces `Completed`; death produces `Died`; timeout/failure produces `Failed`; stopping/restarting before an ending produces `Abandoned`. Unsuccessful attempts retain their final unfinished interval with `completed=false`. Compare unfinished intervals separately from completed objectives so early failures do not look like fast completions.

## View results in Unity

1. Open the **POSESSION-P5** project and play Level_01, Level_02, or Level_03.
2. Possess a body and unlock a door or smash a wall. Finish the level, fail, or stop Play Mode.
3. Select **Possession → P5 → Analytics → Open Timing Results**.
4. Click **Refresh / current attempt**. The window shows first-possession time and objective-duration bars. During Play Mode this is a live snapshot; an unfinished step is stored when the attempt ends.
5. Use **Saved attempt** to inspect another attempt, **Open data folder** for files, or **Export displayed report as CSV**.

No demonstration values are inserted. Empty data shows a “No timing report yet” message.

## Collection and export

The pipeline is **shared gameplay event → timing tracker → immutable report snapshot → JSON/CSV → editor results window**. The report stores an anonymous attempt ID, scene name, UTC start timestamp, capture context, outcome, first-possession value, and objective intervals. It contains no account name or player identifier.

On desktop/editor, each finalized attempt creates matching `<attemptId>.json` and `<attemptId>.csv` files in `Application.persistentDataPath/P5Timing`. The CSV uses quoted text fields and invariant decimal separators for spreadsheet import. PlayerPrefs also stores the latest report under `Possession.P5.LatestTiming.v1`.

In WebGL, only the latest report is stored in local browser PlayerPrefs. The results window is an **editor tool**, and this change adds no browser download UI, remote server, or team dashboard. A hosted playtest export/collection route can be added separately if the team needs browser reports from other players. Automated batch tests do not write playtest history or PlayerPrefs.

Keep before/after feedback builds and level names consistent when comparing attempts. Useful checks include a reduction in median first-possession time and fewer unusually long objective intervals after clearer cues are introduced. These are analysis suggestions, not claims of measured improvement.

## Verification

- All **32 P5 tests passed**: 17 wall-visibility tests, 13 timing tests, and 2 floor-text tests.
- Full PlayMode suite: **85/86 passed**. The existing `HeavyEnemyTests.PatrolMovesAtSixtyPercentOfGuardSpeed` still fails under very small headless frame times, as it did before these changes. Shared enemy code was not altered for P5.
- The timing tests cover first possession at time zero, first-only capture, interval arithmetic including exit, incomplete attempts, immutable snapshots, duplicate and cross-scene events, paused time, countdown failure, and JSON/CSV round trips.
- The cleanup tests check all three saved levels and preserve object labels and colliders.
- Manual final playtesting of the editor results window and teammates' camera-pan changes is still required.

The only modified shared files are four level-builder files containing floor-text removal. CameraFollow, HUDManager, HUDSetup, VisualSetup, EventBus, AnalyticsTracker, scenes, prefabs, and project settings are unchanged by P5. The remaining code, shader, tools, tests, and documentation are new files.

The feature is developed on `Omkar/p5-visibility-data` for review against `integration`.
