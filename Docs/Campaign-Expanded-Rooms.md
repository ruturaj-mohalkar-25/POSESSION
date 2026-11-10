# Expanded campaign Levels 1 and 2

Use Unity **6000.3.22f1** and open `Assets/Scenes/MainMenu.unity`. Player-facing Level 1 remains `Assets/Scenes/Level_03.unity`; Level 2 remains the Hunted maze at `Assets/Scenes/Level_02.unity`. Warehouse remains Level 3.

Both existing layouts have twice their original horizontal dimensions, giving approximately four times the floor area. This expands their existing rooms and connecting passages; it does not add new puzzle objectives or enemies.

| Area | Before | Expanded |
| --- | --- | --- |
| Level 1 entry | 6 × 14 m | 12 × 28 m |
| Level 1 fork | 24 × 8 m | 48 × 16 m |
| Level 1 left wing | 8 × 18 m | 16 × 36 m |
| Level 1 right wing | 8 × 34 m | 16 × 68 m |
| Level 1 puzzle openings | 3.2 m | 6.4 m |
| Hunted starting room | 8 × 11.2 m | 16 × 22.4 m |
| Hunted courtyard | 9.6 × 11.2 m | 19.2 × 22.4 m |
| Hunted north rooms | 11.2 × 8 m | 22.4 × 16 m |
| Hunted main passages | 3.2 m | 6.4 m |

Targets, crates and charge crystals retain their original physical size. The subsequent [character/combat tuning](Campaign-Character-Combat-Tuning.md) enlarges campaign characters by 25% and increases enemy vision/attack reach. Health, possession duration, damage, attack delays/cooldowns and shared ejection protection are retained. Camera and HUD presentation follow merged integration PR 68. Existing level clocks remain three minutes for Level 1 and four minutes for Hunted. Human playtesting should judge whether these clocks need further tuning for the larger spaces.

Floors, boundary walls, puzzle gates, breakable walls, exit areas and checkpoint volumes expand together. Actor positions, external waypoints, spawn markers, pickups and the Hunted checkpoint destination follow the new spacing. Both Hunted puzzle branches remain independent.

## Rebuild

Use **Possession → Level Design → Expand Campaign Levels 1 and 2** to apply the spacing to existing saved stages. The operation preserves the scene's existing gameplay components and has a serialized `CampaignLayoutScale` marker to prevent repeated expansion. The original P4 level builder, Hunted maze builder and three-level campaign build also apply this spacing. Guard/Heavy placement tools map their patrol coordinates to the expanded layout.

Do not scale a whole level root manually: that would also resize characters and gameplay colliders. Durable layout changes belong in the original builders and the spacing setup tool.

Stop Play Mode and reopen MainMenu after syncing saved scene changes. An older open scene in Unity may still hold the previous spacing; avoid saving it over the updated asset.
