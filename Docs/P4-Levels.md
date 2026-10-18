# P4 — Level_02 and Level_03 (Alpha)

Both levels are built from Editor scripts, the same way Level_01 is. Nothing here hand-edits a
`.unity` file. Re-running a builder rebuilds that scene from scratch, so treat the builder as the
source of truth and put layout changes in the script rather than in the saved scene.

## Menu items to run (in order)

The menu root is `Possession/`, matching the existing P1/P2/P3/P5 tools.

1. `Possession > P4 > Create or Update Level Configs` — writes the three `LevelConfig` assets into
   `Assets/Config/`. Run this first; the builders read the matching asset.
2. `Possession > P4 > Build Level_02` — builds and saves `Assets/Scenes/Level_02.unity`, then adds it
   to Build Settings after Level_01.
3. `Possession > P4 > Build Level_03` — same for `Assets/Scenes/Level_03.unity`.

After each build, commit the generated `.unity` scene, its `.meta`, and
`ProjectSettings/EditorBuildSettings.asset`.

## Layouts

### Level_02 — two rooms plus an exit corridor

```
z=0 ──────── Room 1 ──────── z=12 ──────── Room 2 ──────── z=24 ─ corridor ─ z=30
     Spirit start                  Target_02 (x=+4)            BreakableWall (z=27)
     Target_01 (x=-4)              open passage at z=12        ExitZone (z=29)
                                   Door at z=24
```

Intended flow: possess a Guard, shoot `Target_01` from Room 1 and `Target_02` from Room 2 → `Door`
opens → possess the Heavy and smash `BreakableWall` → eject and reach `ExitZone` as the Spirit.

### Level_03 — three rooms plus an exit alcove, gated as a sequence

```
z=0 ── Room 1 ── z=12 ── Room 2 ── z=24 ── Room 3 ── z=36 ── alcove ── z=42
   Spirit start       Heavy_01        Heavy_02           ExitZone (z=41)
   Target_01          BreakableWall_01 BreakableWall_02
   Door (z=12)        (z=24)           (z=36, exit wall)
```

Intended flow: Guard shoots `Target_01` → `Door` opens into Room 2 → Heavy_01 smashes
`BreakableWall_01` into Room 3 → Heavy_02 smashes `BreakableWall_02` → finish as the Spirit.

## Conventions carried over from Level_01

- 12 x 12 rooms running along +Z, floors are cubes scaled `12 x 0.2 x 12`.
- Walls are 6 m tall (`y = 3`, `scale.y = 6`) so the Spirit cannot jump a puzzle or leave the level.
- Every divider leaves a 3.2 m centred opening — wide enough for the Heavy, and wide enough for the
  Spirit to eject without clipping into geometry.
- Targets sit at `y = 1.6`, the Guard's FirePoint height, so a shot can actually land on them.
- Layer 6 = Enemy, layer 7 = Puzzle. Puzzle prefabs are instantiated from `Assets/Prefabs/Puzzle`.
- `LevelExit` is built as an empty GameObject with a trigger BoxCollider (there is no LevelExit
  prefab), wired to one Door and one BreakableWall, exactly as Level_01 ends up after the P4 refine.

## Difficulty config

`Assets/Scripts/Config/LevelConfig.cs` is a ScriptableObject; `Assets/Config/LevelConfig_Level_0X.asset`
holds the values. The builders write `levelTime` onto `GameManager` and `possessionTimer` onto the
Spirit's `PossessionController` while building, and will push `guardDetectionRange` onto any
`GuardEnemy` already in the scene.

| Level | Possession timer | Level time | Guard detection | Gold / Silver / Bronze |
|-------|------------------|------------|-----------------|------------------------|
| 01    | 12 s             | 300 s      | 10 m            | 100 / 150 / 225 s      |
| 02    | 10 s             | 240 s      | 12 m            | 80 / 120 / 180 s       |
| 03    | 8 s              | 180 s      | 14 m            | 60 / 90 / 135 s        |

Par times are starting points, picked as roughly a third, a half and three quarters of the level
clock. Retune them once the levels have actually been played.

The Level_01 asset is written for reference only — the builders never touch Level_01, and that scene
currently runs a 10 s possession timer. Changing it is P2's call.

## Playtest checklist (manual, in the Editor)

Run the three menu items above first, then open each scene and press Play.

Level_02:
- [ ] Spirit spawns in Room 1 and can walk into Room 2 through the z=12 opening.
- [ ] Both targets are visible and sit at shot height; the Door stays solid until both are hit.
- [ ] Door opens once `Target_01` and `Target_02` are both hit, and the Heavy can walk through it.
- [ ] `BreakableWall` breaks after the configured hits and the corridor opens.
- [ ] `ExitZone` only completes the level for the Spirit, after the Door is open and the wall is gone.
- [ ] Floor signs are readable from the default camera and do not bleed through walls badly.

Level_03:
- [ ] Guard can hit `Target_01` from Room 1 and the Door at z=12 opens.
- [ ] Heavy_01 reaches and breaks `BreakableWall_01`; the gap is wide enough to pass.
- [ ] Heavy_02 reaches and breaks `BreakableWall_02`.
- [ ] `ExitZone` completes the level only after the Door is open and the exit wall is gone.
- [ ] No spot lets the Spirit jump a wall or fall out of the level.

Both:
- [ ] Build Settings lists MainMenu, Level_01, Level_02, Level_03 in that order.
- [ ] Level clock and possession timer match the table above.

## Left for teammates

- P1: drop Guards onto `SpawnPoint_Guard_0X` and give them waypoints.
- P3: drop Heavies onto `SpawnPoint_Heavy_0X` and give them waypoints.
- P5: the HUD canvas is not built in these scenes — `Possession > P5 > Setup HUD Bars and Buttons`
  works on the open scene, but it needs an existing `HUDManager` to find.
