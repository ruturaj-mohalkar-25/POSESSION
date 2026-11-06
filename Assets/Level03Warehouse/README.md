# Warehouse — local Level 3 preview

Open this project with Unity **6000.3.22f1**. Open `Assets/Level03Warehouse/Level_03_Warehouse.unity` and press Play. The scene is intentionally absent from the campaign build list while shared gameplay changes are pending.

## Planned campaign order

| New slot | Source |
| --- | --- |
| Level 1 | Current displayed Level 2 (legacy `Assets/Scenes/Level_03.unity`) |
| Level 2 | Current displayed Level 3, Hunted maze (legacy `Assets/Scenes/Level_02.unity`) |
| Level 3 | This new Warehouse design |

Those are the current playable build's displayed numbers. Existing campaign files, main menu and build settings have not been renamed or edited in this preview branch. Apply the campaign order once the team's shared gameplay work is merged.

## Layout and routes

Single floor, enlarged warehouse rooms, lower-centre entry and far-centre extraction. Four Guards, two Wall Breakers, four sliding doors, twelve targets, three breakable barriers, nine reforming possession-charge sources and one reserved health placement.

- **Left route:** Entry → Sorting → Guard Room. Shoot its three cyan targets to open Inspection. Shoot Inspection's three violet targets to open Exit Hall → extraction. The right wing and middle walls can stay closed.
- **Right route:** Shoot Sorting's three pink targets to open Cargo. Shoot Cargo's three amber targets to open Loading. Use the right-junction Wall Breaker to smash Loading's cracked exit wall → Exit Hall → extraction. The left wing and middle walls can stay closed.
- **Optional crossing:** From the left Service Lobby, smash the left recovery barrier. From the right junction, smash the right recovery barrier. The health placement sits between them. Each broken entrance stays open, so the Spirit can retreat by the same path. Opening both allows a wing change but does not solve the target doors.
- Three distinct targets open each matching door, in any order. Hits remain latched across ejection and possession expiry. Lamps show the three hits; the door leaves visibly slide open.
- Exit as Spirit after completing either main route. The checkered extraction deck and illuminated EXIT arch identify the winning area.

## Space and movement tuning

The plan now uses 0.65 metres per coordinate instead of 0.5 (30% wider/deeper). Main wing rooms are 22.1 × 14.3 metres; Sorting is 16.9 × 13 metres. Main connecting passages and matching door/barrier openings are 6.5–7.8 metres across, with a 6.5-metre Service approach. The entry is also wider. Crates, characters, collision radii, attack ranges and camera settings keep their physical size, leaving more usable space around cover and enemies. This changes the local layout rather than shared combat rules. Door and recovery-wall openings were resized with their passages so locked routes remain sealed.

## Controls and recovery

Use the shared controls: WASD to move, Space to jump, E to possess/eject, LMB to shoot/smash. Pick up a green charge crystal before possession and approach a host outside its vision cone. Source crystals reform after eight seconds. **R** or the shared result-panel Retry button reloads the whole local preview, including defeated hosts and puzzle state. Results retain the shared HUD/analytics screen; its next button replays this preview.

Preview tuning is seven minutes on the level clock and twelve seconds per possession. These values live in `Data/warehouse-layout.json`. Enemy movement, attacks, vision, ability budgets, ejection and player HP follow the merged integration code and copied Level 1 actor settings. Current Heavy rules allow **one smash per possession**. Recollect a charge and possess again to open another barrier; this level does not grant extra smash uses or invulnerable hosts.

## Pending team dependency

The green cross is a **placement socket, not a functional heal yet**. Current integration provides no shared healing API. Bind the team's incoming health pickup here after that code is merged. The intended rule is once-per-run restoration of 30% maximum Spirit/player HP, capped at maximum, persistent after ejection, collectible in either form and not consumed at full health. Do not implement this with negative damage or a separate local HP pool.

The earlier paper proposal allowed both recovery walls to be smashed in one possession. That conflicts with current shared ability limits; the preview retains the shared limit. Revisit only if the team changes that rule globally. Final combat difficulty and timing still need human playtesting after the incoming shared features merge.

## Updating after integration merges

This folder is isolated from shared code. Bring the newly merged integration commits into this local branch, then use **Possession → Level Design → Level 3 Warehouse → Build and Open**. The builder copies the current Level 1 shell and actors so that rebuilt previews inherit shared components, current camera setup, HUD and gameplay changes. Resolve any changed shared APIs before the final campaign integration. No future conflict-free merge can be guaranteed, especially when the campaign scene order is registered.

The scene is generated from `Data/warehouse-layout.json`; edit that authoring data for durable layout changes. Rebuilding replaces manual edits made only to the generated scene. Units are 0.65 metres per plan coordinate; room connections and external boundaries come from the union of the authored floor areas. Doors and breakable walls occupy explicit openings. The first charge was moved from the rear corner into the visible entry approach for readability.

## Validation

Eleven Warehouse PlayMode checks pass in the team Unity version: shared component/count setup; three distinct hits and sliding presentation; both independent routes with Heavy-sized clearance; each sealed recovery entrance and retreat; actual shared projectile fire to all twelve targets; rejection of hostile shots; shared smash and damage threshold; real charge possession/forced ejection with persistent target state; and shared completion/replay reset. Camera screenshots were inspected at entry, both wings, recovery, open door and extraction. These are scene and integration checks, not a claim of completed human combat testing.

No existing scene, shared script, prefab, package or build setting is changed by this preview. No remote branch, PR or deployment is created for this work.
