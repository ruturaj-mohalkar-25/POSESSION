# Three-level WebGL preview with the shared Soul Coin tutorial

Based on integration `3b0ffaa`, this preview includes Soul Coins (`90c36f3`, PR #57), rear ejection and attack grace (`7364f5d`, PR #52), and the simplified How to Play (`39c2481`, PR #60). The teammate source branches remain unchanged. Unity: **6000.3.22f1**.

## Level order

| Menu selection | Layout | Legacy scene asset | Soul Coin sources |
| --- | --- | --- | --- |
| How to Play | Updated practice lesson | Tutorial | 2 recovering sources |
| Level 1 | Existing Level 1 layout | Level_01 | 2 recovering sources |
| Level 2 | Old Level 3 layout | Level_03 | 4 recovering sources |
| Level 3 | Our Hunted maze | Level_02 | 3 recovering sources and 1 bonus coin |

Legacy scene filenames retain their puzzle/configuration references. Main Menu, NEXT LEVEL and FINISH all follow this player-facing order. Level 4 is outside this build. Existing campaign geometry and camera settings are retained.

## One set of possession rules

The tutorial and every campaign level start with **one charge**. Possessing costs one; collecting a Soul Coin as the free Spirit adds one. Charges stack and remain after ejection. Empty Spirits cannot possess. Front-facing or otherwise invalid attempts do not spend a charge. The gold coin model, charge HUD and analytics are shared.

The main-path sources return after eight seconds, allowing retries after spent charges or missed shots. Coins are placed before required doors or breakable walls. The maze's existing optional bonus remains one-use. Restart and retry restore the shared starting charge.

All levels retain shared Guard/Heavy abilities, enemy attacks, red vision cones and possession cues. Ejection tries behind the body first, then sides, then front. The body just left waits at least two seconds before attacking; the maze's additional contact-pressure path respects the same grace. Other enemies remain dangerous.

A shared projectile correction routes both swept movement and trigger callbacks through the same enemy-bullet hit method, preventing missed Spirit damage and duplicate hits.

## Updated tutorial

PR #60 keeps the welcome and result cards, while movement, coins, cones, possession, shooting, smashing and auto-ejection use the objective bar and short non-blocking cues. Beginning practice resumes gameplay immediately. Empty-charge hints direct the player to a coin. Recovering practice coins let the player retry abilities without exhausting the lesson.

## Build and publish

The root package consists of `index.html`, `Build/`, `TemplateData/`, `.nojekyll` and `build-info.json`. The manifest identifies the source revision, included tutorial/feature revisions, shared charge settings, campaign order and enabled scenes.

Use **Possession → Build WebGL for GitHub Pages**. The build synchronizes the authored charge settings, pickups, menu and build list. Local staging defaults to ignored `WebGLBuild/`. Gzip decompression fallback supports GitHub Pages without custom compression headers; hashed files prevent stale package combinations.

Batch builds accept `POSSESSION_WEBGL_OUTPUT` and `POSSESSION_SOURCE_COMMIT`, then run Unity with `-batchmode -nographics -quit -buildTarget WebGL -projectPath <project> -executeMethod Possession.Editor.WebGLBuild.Build`.

Serve local output over HTTP, for example `python3 -m http.server 8080`, rather than opening index.html directly.

The source/package branch is **Omkar/web-build-integration**. GitHub Pages publishes **WebBuilds**. Releases copy the verified package to the live root and an immutable `releases/tutorial-<source revision>/` directory, providing a fresh shareable URL alongside the usual playable link.

## Verification

Unity **6000.3.22f1**: **175/175 PlayMode tests passed**, zero failed and zero skipped. This includes real coin collection, shared player shots and Heavy smash through the tutorial's completion, forced ejection, coin recovery, consistent starting charges/HUD across all scenes, campaign order, and once-only enemy projectile damage.

Browser checks confirmed welcome-to-play with no extra modal, actual WASD/jump coaching, Soul Coin collection and stacking, possession ability prompts, and the shared coin count in all three campaign scenes. The browser check had no JavaScript errors. The root and versioned release packages use the same tested files. Human difficulty playtesting remains separate.

Versioned release URL: https://ruturaj-mohalkar-25.github.io/POSESSION/releases/tutorial-1183820/
