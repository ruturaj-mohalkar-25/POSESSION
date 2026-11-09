# Campaign character and combat settings

The saved campaign uses merged integration f5ee9d3 (including PRs 67 and 69) plus the local presentation and gameplay-consistency changes.

| Setting | Shared campaign value |
| --- | --- |
| Spirit / Guard root scale | 1.25 |
| Heavy root scale | 1.625 |
| Spirit speed / jump launch | 8 m/s / 8 m/s |
| Guard / Heavy base speed | 5 / 4 m/s |
| Full enemy vision cone | 75 degrees |
| Guard / Heavy detection | 9 / 7 m |
| Guard shot / Heavy attack range | 8 / 3.5 m |
| Camera offset | (0, 25, -18) |
| Eject shield / possession cooldown | 2 s / 2 s, with separate clocks |

Meshes and physical capsules grow together. The authoring pass grounds bodies, adjusts waypoint heights, configures room/patrol boundaries and applies the shared HUD. The existing Heavy weight cue slows only its possessed movement by the same factor in every stage. Health, attack damage/rates and level-specific timers are retained.

The wider vision mesh follows the real detection angle and range and stops at solid geometry. Shared hostile projectiles apply damage once across raycast/trigger hits. Heavy attacks require line of sight at windup and impact. Rear approaches remain valid for possession; ejection prefers a clear grounded rear or side position and grants the shared cyan two-second shield.

Use **Possession → Level Design → Tune Campaign Characters and Enemy Cones** to reapply. Campaign builders use this same final authoring pass. The wider first two stages and original ten-room Warehouse route at 0.65 scale are retained without the removed extension.

**222/222 full PlayMode tests pass**, with no failures or skips. All three stages were also visually checked for jumping, shield/eject behavior and native completion/replay. See [gameplay consistency](Campaign-Gameplay-Consistency.md) for the full rules, fixes, test scope and local playtest steps. Work remains local; no push was made.
