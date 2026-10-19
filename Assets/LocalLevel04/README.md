# Level 04 prototype — The Relay Run

Experimental greybox based on Omkar's maze sketch, shared on a separate branch for team review. Everything unique to this prototype is under `Assets/LocalLevel04`. The scene is deliberately excluded from Build Settings and the shared main-menu progression.

The playable view uses the same 3D perspective camera as Levels 01–03: 40-degree field of view, 55-degree downward angle, and the existing CameraFollow with offset (0, 25, -13). It follows the Spirit or possessed host. Solid walls and characters use directional lighting and shadows; the small HUD minimap is an optional flat overview.

## Play

Stop Play mode, then choose **Possession → Local Experiments → Open Level 04** and press Play. You can also open `Level_04_Local.unity` directly. **Rebuild Level 04** regenerates this experiment, including its scene and materials; save hand edits elsewhere before rebuilding.

- WASD: move. Space: Spirit jump.
- E: possess a host within 5 metres and line of sight, or eject.
- LMB: aim and shoot as Guard; face a gate and smash as Heavy.
- M: toggle the live minimap. R: restart, including after victory or failure.
- Escape: pause/resume. The pause screen also has Resume and Retry buttons.

## Spectral HUD

The local HUD is built at runtime with Unity UI and scales with the Game view.
Spirit is cyan, Guard is blue, and Heavy is amber. It includes segmented health,
a circular countdown, a five-step mission tracker, ability uses and a possession
link bar. The signal map shows live host positions and pulses around the next
objective. Possession, ejection, damage and completed relays show short banners;
the screen border pulses red when time is low.

Pause freezes the clock and disables gameplay controls until Resume. Victory
shows the existing star rating, elapsed time, completed relays and host links;
failure explains whether time or Spirit integrity ran out. Both offer Retry.
This UI is confined to the Level 04 prototype. The shared HUD is unchanged.

Local Guard shots skip the shared muzzle-burst component, whose temporary
primitive collider could consume a projectile on its spawn frame. The shared
projectile prefab and effects code are unchanged.

## Route

1. Start in the south-west. Find the blue Guard beyond the cover block. Possess it and shoot the red north-west target to open gate 01.
2. Enter the central wing. Possess the orange Heavy and smash the east-facing cache gate (02).
3. Eject, find the central Guard, and enter the opened cache. Shoot the relay target inside to open gate 03 at the south-east of the central wing.
4. Go through gate 03 and take the long route around the right-hand end of the east baffle. Possess the second Heavy and smash gate 04, north of its patrol route.
5. Eject and enter the green finish zone as a living Spirit. All four gates must be open.

## Difficulty settings

- Footprint: 66 × 40 metres, with 6-metre walls to prevent jumping over the puzzle.
- Level clock: 120 seconds, editable on `Local04_GameManager`. Start with this budget and tune after human playtests.
- Possession: 10 seconds, 0.5-second re-entry cooldown, 5-second hostility after forced ejection. Existing Spirit/possession/ejection systems are reused.
- Three Guards (3.2 m/s) and two Heavies (2.2 m/s, 5 HP). Guards have three shots per possession; Heavies have one smash. Repossessing refreshes uses.
- Visible Spirit can be chased and take contact damage. Walls block detection; a short ejection grace period prevents immediate contact damage.
- Required hosts cannot be killed by stray Guard shots in this prototype, preventing a permanent puzzle softlock. Their AI uses local grid navigation around walls and closed gates. This is separate from the shared Guard/Heavy AI.
- Heavy gates reject ordinary projectiles and require a possessed Heavy. Gate prerequisites prevent opening the final route out of order.

## Verification

Eight native Unity 6.3 PlayMode tests cover scene isolation and roles, real projectile/smash gate progression, the long east route and sealed cache, Heavy approach times, grounded ejection beside walls, AI possession behavior, Spirit-only finish, restarting a scene outside Build Settings, HUD health/form updates, and pause/resume behavior. The tests use controlled placements to exercise interactions; they do not establish a human completion time or final difficulty balance.

The scene was also rendered with the native Unity camera for visual inspection. Shared scenes, prefabs, scripts, and Build Settings are unchanged. Final art, audio, wider screen-layout polish, difficulty tuning, and team integration are outside this local prototype.

## Previews

[3D layout overview](../../Docs/Level04/Overview.png) · [Spectral HUD preview](../../Docs/Level04/HUD.png)
