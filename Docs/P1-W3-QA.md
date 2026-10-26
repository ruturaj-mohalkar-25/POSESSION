# P1 W3 — Enemy Route QA

Date: October 19, 2026

## Scope

The W3 checks exercise every Guard currently present in the integrated levels. They verify:

- two valid, independent waypoints per Guard;
- a clear capsule-width patrol segment between waypoints;
- visible movement along the saved patrol route;
- `OverlapSphere` detection and chase behavior;
- AI suspension while possessed;
- hostility immediately after a forced eject;
- possession becoming available after the hostile timer expires; and
- patrol resuming toward the nearest waypoint.

## Integrated level results

| Level | Guards | Patrol | Detection/chase | Possession/eject | Hostility/resume | Result |
| --- | ---: | --- | --- | --- | --- | --- |
| Level 01 | 1 | Pass | Pass | Pass | Pass | Pass |
| Level 02 | 2 | Pass | Pass | Pass | Pass | Pass |
| Level 03 | 2 | Pass | Pass | Pass | Pass | Pass |
| Level 04 | — | Pending upstream scene | — | — | — | Blocked |

Level 04 is not present in the `integration` branch as of this report. Its final route walk and behavior verification must run after the Level 04 scene is merged.

## Automated verification

`EnemyRouteW3Tests` loads each integrated level and performs the complete behavior sequence on every saved Guard. The full Unity PlayMode suite completed with **54 passed, 0 failed and 0 skipped**.

Related coverage also verifies Guard prefab configuration, per-level tuning, staggered Level 02 patrol timing, tighter Level 03 routes, wall collision, puzzle progression, Heavy patrols, HUD behavior and level completion.
