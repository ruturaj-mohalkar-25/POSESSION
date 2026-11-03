# Three-level WebGL preview

This preview starts from merged integration `3b0ffaa` and combines the Soul Coin collectible work (`90c36f3`, PR #57) with rear-first ejection and a two-second attack grace (`7364f5d`, PR #52). Those source branches remain unchanged. Unity version: **6000.3.22f1**.

## Player-facing level order

| Menu selection | Layout | Legacy scene asset |
| --- | --- | --- |
| Level 1 | Existing integration Level 1 | Level_01 |
| Level 2 | Old Level 3 | Level_03 |
| Level 3 | Our Hunted charge maze | Level_02 |

Scene filenames are retained so the existing puzzle scripts, references, configuration assets and regression tests stay aligned with their layouts. Menu selection and NEXT LEVEL follow the same explicit campaign order. The maze is the final level and shows FINISH. The tutorial is separate.

Integration's camera and tutorial are retained. Level 1 keeps its existing layout and unlimited possession. The maze's collectibles use the gold Soul Coin appearance, with its existing empty start, one-charge carry limit, pickup positions and reform rules. The reusable Soul Coin prefab and shared charge-count/analytics support are included. Enemy attacks and red vision cones are retained. Ejection tries behind the body first, then sides, then front. The body just left cannot attack for two seconds, including the maze's additional contact-pressure path. Other enemies remain dangerous.

Level 4 is outside this preview.

## Build package

The repository root contains `index.html`, `Build/`, `TemplateData/`, `.nojekyll` and `build-info.json`. The manifest records the exact source revision, included feature revisions, campaign order and enabled scenes.

In Unity, choose **Possession → Build WebGL for GitHub Pages**. Output defaults to the ignored `WebGLBuild/` staging folder. The package uses gzip with decompression fallback, allowing the loader to run on GitHub Pages without custom compression headers. Hashed filenames avoid stale loader/data combinations.

For batch builds, set `POSSESSION_WEBGL_OUTPUT` to a separate output directory and `POSSESSION_SOURCE_COMMIT` to the source revision, then run Unity with `-batchmode -nographics -quit -buildTarget WebGL -projectPath <project> -executeMethod Possession.Editor.WebGLBuild.Build`.

After verification, copy the generated build package to the repository root. **Possession → Prepare Combined Web Preview** synchronizes the menu and build list without rebuilding level geometry.

Serve the root over HTTP, for example `python3 -m http.server 8080`, and visit `http://localhost:8080/`. Opening index.html directly does not provide the HTTP environment required by Unity WebGL.

GitHub Pages currently publishes the root of **WebBuilds**. This branch is prepared and pushed for testing; it does not replace the public game.

## Verification

Unity PlayMode: **168/168 passed**, with zero failures and zero skipped. New checks cover actual level-select buttons, progression and final completion, integration camera/tutorial preservation, Soul Coin placement, and the maze's contact damage respecting eject grace.
