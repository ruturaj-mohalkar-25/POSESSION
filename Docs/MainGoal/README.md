# Main goal: character shadows and avatars

Approved style F: soft directional silhouette plus stronger floor contact. Covers Spirit, autonomous Guards and Heavies, controlled hosts, runtime-spawned enemies and Tutorial. The NPC stamp is hidden while its host is possessed so it does not double the player shadow.

The Spirit has a white/teal humanoid avatar. Guards have navy clothing, a peaked cap, gold badges, duty belt/radio and a visible sidearm. Only clothing turns white in possession. Existing root renderers remain as logical visibility/bounds proxies; gameplay transforms, colliders and abilities are retained.

Countdown blink targets visible body pieces; equipment retains visibility. Attack warning property blocks bind to the new model. Hide/show follows possession, death, component enable/disable and scene transitions. Global lighting settings restore after the final owner leaves, including additive scenes.

## Review order

This is a focused followup to existing PR84 (base branch codex/playtest-feedback). The USC stretch PR is stacked above this main PR and contains its own room-art/corner changes. Neither PR is merged automatically.

## Verification

Full gameplay suite: **304 passed, 0 failed**. After the additional fallback lifecycle edge fix, **24/24 avatar and visible-body blink checks passed**. The final combined stretch suite will include these same runtime changes. Build and browser evidence follow. New regression coverage exercises real possession/ejection and shield retention, visible uniform versus cap colours, every stage/Tutorial avatar/shadow installation, no new colliders, component lifecycle, cloned actors, death, attack warning tint and additive lighting restoration. Existing countdown-blink timing/pause/restoration tests now observe the visible torso instead of the hidden legacy capsule.

Native closeups are inspection camera renders; any wall between the camera and character is temporarily hidden for the capture only. Gameplay overview and browser evidence are recorded separately.
