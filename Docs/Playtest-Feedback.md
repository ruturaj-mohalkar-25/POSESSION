# Cross-team playtest feedback

Based on the two-player recording supplied on November 20, 2026. The recording is ambiguous about getting stuck and enemy reaction time; it also includes positive feedback on the circle indicator and overall look.

## Changes

- Practice starts with an actionable approach instruction and a nearby WASD/Space reminder. It only says **Press E** when the intended body is the actual selectable target.
- Practice explains blocked walls, red vision cones, missing charges and the post-ejection recovery period using the same validation as possession itself. Possessing the wrong body offers an explicit way back to Spirit form.
- The shared E marker follows the actual possession selector, including vision-cone restrictions, rather than choosing a separate nearest body.
- The campaign charge banner distinguishes a ready possession from recovery and a safe approach.
- The countdown keeps a bright stationary outer rim while its coloured arc drains. Practice explains early ejection and forced ejection at zero.
- Danger guidance says to move out of the red zone without claiming health is falling during the existing protection period.

Enemy speeds, reaction delays, attack windups, the two-second eject shield, colliders and ejection placement are unchanged. The recording did not establish a specific collision defect or agree on a longer escape window. Existing real ejection, jumping, shield and attack tests remain part of validation.

## Verification

| Gate | Check |
| --- | --- |
| First action is clear | Real Tutorial spawn shows approach instruction and movement/jump keys |
| E hints are honest | Real possession, wall, cone, no-charge and recovery scenarios |
| Marker targets correct body | A closer visible but invalid body cannot steal the marker from the valid target |
| Timer remains readable | Fixed rim mesh persists while the coloured arc shrinks; browser capture |
| Escape behavior retained | Existing cross-level ejection, jumping, shield and enemy-attack regressions |
| Shareable result | Release WebGL build, browser playthrough and screenshots |

The untouched integration baseline (`e701a02`) ran **287 PlayMode tests: 280 passed, 7 failed**. All seven failures were stale Tutorial expectations: earlier wording, movement-checklist behavior, duplicate objective-bar visibility and an ellipsis character. Related expectations were updated for the current non-pausing, nearby-instruction design while retaining real possession, shooting/smashing, completion and clock checks. The existing pickup cooldown/reuse fixture also disables random relocation during its short wait, preventing automatic recollection from starting a second cooldown before its assertion. Pickup behavior in the game is unchanged.

Final full PlayMode run: **296 passed, 0 failed, 0 skipped**, including all nine new feedback regressions. Browser evidence is recorded below after the release build is checked.

## Reproduce

Use Unity **6000.3.22f1**. Run PlayMode tests from the Test Runner. New regression class: `Possession.Tests.PlaytestFeedbackTests`.

Build with **Possession → Build Playtest Feedback WebGL**, or batch entry `Possession.Editor.PlaytestFeedbackBuild.WebGL`. Output: `Builds/PlaytestFeedback`. The build uses gzip with browser fallback and hashed asset filenames, then restores its temporary Player Settings.
