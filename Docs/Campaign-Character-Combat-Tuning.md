# Campaign character and enemy-pressure tuning

Local branch: `Omkar/character-cone-tuning`. Uses merged integration `1ada259` (PR 68) while preserving the three-stage campaign from PR 67. At the start of this work GitHub still listed PR 67 as open, so this local branch keeps that work without modifying the open PR.

| Setting | Campaign tuning |
| --- | --- |
| Spirit / Guard root scale | 1.25 (25% larger) |
| Heavy root scale | 1.625 (25% larger than its former 1.3) |
| Full enemy vision-cone angle | 75° (previously 45°) |
| Guard detection distance | 9 m |
| Heavy detection distance | 7 m |
| Guard bullet attack/travel range | 8 m |
| Heavy attack range | 3.5 m |

Visual meshes and physical character capsules grow together. Characters are placed back above the floor, and waypoint heights are adjusted. The normal enemy vision-cone renderer reads the actual new angle/range and continues to stop at solid geometry. Rear approaches remain available for possession. Damage, attack intervals, windup/reaction delays, health, level timers and two-second eject protection are retained.

Guard projectiles now dispatch raycast hits on the Spirit through the same damage path as trigger hits, applying damage once per bullet. This repairs bullets being removed by their raycast before the Spirit receives damage. Eject protection absorbs the hit normally. Heavy attacks check line of sight both when beginning and resolving their windup, so the increased reach does not hit through solid walls.

The merged camera offset `(0, 15, -11)`, bottom-centre health/timer layout, bullet/hammer sprites and Hunted's one starting charge are synchronized in the saved campaign scenes. The entry Heavy in displayed Level 1 starts at the scaled version of the newly merged z=7 marker, facing its patrol away from the Spirit. Enlarged Level 1/2 rooms are retained. Warehouse keeps the original ten-room route at scale 0.65 with the wider rooms/passages, without the final extension; current character/combat settings remain applied.

## Reapply after authoring

Use **Possession → Level Design → Tune Campaign Characters and Enemy Cones**. It applies absolute sizes/ranges to each campaign scene and grounds the bodies, so rerunning does not multiply the scale again. The Hunted, P4 skeleton, Warehouse and full campaign builders include the authoring hook; the final campaign pass runs after NPC placement.

## Validation

Before the iteration 1 layout restoration, **52/52 focused checks passed**, including real Guard damage from six metres, Heavy damage at 3.2 metres, the wider visible/detection cone, grounded larger bodies without static overlap, wall blocking, duplicate raycast/trigger suppression, two-second eject protection and all existing campaign/Warehouse route checks.

The full suite from that tuning pass reported **189/214 passing, 25 failing**. Unmodified merged integration `1ada259` reports **136/162 passing, 26 failing**. No additional test cases fail; the wider level's former right-corridor check passes. Remaining integration failures cover old HUD/range/charge assumptions, legacy puzzle fixtures and tutorial behavior. Full results and screenshots are retained in local QA artifacts; tests were not hidden or skipped.

The original ten-room route retains the wider 0.65 layout and widened passages; only the extension and its objectives are removed. **45/45 targeted PlayMode checks pass** across Warehouse, campaign order, expanded Level 1/2 and combat tuning. Both original wings independently reach the original exit with larger bodies; extra rooms and puzzles are absent. The rebuilt entry and overhead layout were visually checked. Native final campaign flow remains covered by the automated checks. The full suite was not rerun for this Warehouse-only spacing correction.

## Test locally

Open the project in Unity **6000.3.22f1**, stop Play Mode and reopen `Assets/Scenes/MainMenu.unity`, then choose a campaign stage. Check the visible character size, wider red field, actual incoming Guard bullets, Heavy windup/reach, hiding behind cover and possessing from behind. Try manual and forced ejection near walls.

Automated checks cover actual detection on the wider flank, real Guard damage from six metres, Heavy damage at 3.2 metres, wall blocking, duplicate raycast/trigger hit suppression, shield protection, body grounding/clearance and existing puzzle routes. Human playtesting is needed to tune the difficulty and camera framing further.

Work is local only; no new PR or push is made for this tuning pass.
