# Main goal: shadows, readable characters and solid cover

Approved F shadows combine directional silhouettes and soft floor contact for Spirit and all enemies. Police have a navy uniform, cap, badge and visible sidearm. Spirit is white/teal. Heavy has an orange work uniform, hardhat and hammer.

Spirit, police and Heavy have two visible legs and boots. Feet alternate with actual distance travelled, settle when movement stops and freeze during pause. Animation moves only visual pieces. Possession colours, visible-body countdown blink, combat warning tint and Heavy smash guide stay connected to gameplay.

Flat mat-like props are replaced by **solid wooden hiding crates**, approximately 3 × 2.3 × 2.1 m including trim. Each intentionally adds one enabled blocking BoxCollider. Cover stops movement, actual enemy sight and the rendered red vision cone, and avoids full patrol segments and objectives. Crates remain opaque during camera occlusion so their overlapping box/trim surfaces do not pulse between solid and transparent. Original authored colliders are unchanged. Vision cones use 96 ray samples and an expanding hit buffer.

## Validation

The corrected main build passed **310/310 PlayMode tests**, zero failures/skips. New tests exercise actual sight and cone obstruction, distance-driven stride/idle/pause and retained Heavy guide visibility. Existing full campaign solution, patrol, possession/ejection, combat, jumping and occlusion tests also passed.

Scene captures use an inspection camera with HUD hidden. Closeups temporarily hide intervening walls/world labels. Stride images advance the same distance-driven pose by small staged root movements; they are inspection poses rather than recordings of human play. The runtime build uses normal cameras and visibility.

![Solid cover](Level_03-hiding-crate.png)
![Police stride](Level_03-police-stride2.png)
![Heavy](Level_03-heavy.png)

Review order: existing #84 → main #85 → USC stretch #86. Main playable build: https://utkarsh-landge.github.io/posession-gold-playtest-2026/playtest-feedback/main-goal/

Crate trim protrudes beyond the wooden panels, with separate top/side/front planes to prevent z-fighting. The collider encloses the full trim. A contact/camera-edge regression checks unchanged crate transforms and materials across 50 physics steps while the player pushes against it.

After the crate stability correction, all **46/46 affected character, wall-visibility, vision-cone and presentation tests passed**. The earlier full main run passed 310/310; the combined full correction run passed 317/317 before the narrow stability followup. Final combined verification is recorded separately.
