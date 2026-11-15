# Level 4 — The Split Vault

Level 4 review prototype: two floors, guarded archive access, a recoverable vault cipher, and an Engineer vault finale. Open the scene directly to playtest the layout and mechanics before campaign integration.

## Open and play

Use **Unity 6000.3.22f1** in this checkout.

- Open `Assets/Level04Gold/Scenes/Level_04_Gold.unity`, then press **Play**.
- Editor shortcut: **Possession → Level 4 Gold → Open Local Level**.
- To regenerate the blockout, stop Play Mode and choose **Possession → Level 4 Gold → Build and Open**. This replaces the local Level 4 scene; preserve manual edits separately.

Registered in local Build Settings for Retry. The existing campaign menu, campaign order and tutorial are unchanged; test Level 4 directly.

## Layout

Rooms retain the planned connections with 1.25× larger X/Z footprints and 6 m ordinary passages. Floors remain at Y = 0 and 5 m. Walls are 4.5 m high and 0.40 m thick, with 1.7 m cover crates. Physical stairs connect the floors; the broken overlook reveals the lower service connection.

From the gallery, walk east into the flat stair foyer and follow the north-pointing floor arrow up. The foot clears the full gallery opening; the side glass starts beside the flight. The 7.5 m-wide flight rises 5 m over an 8.5 m run. Both Spirit and possessed bodies can walk up and down without jumping.

## Controls

| Input | Action |
| --- | --- |
| WASD | Move Spirit or the possessed body. |
| Space | Jump as Spirit. |
| E | Possess an eligible host, or eject. Requires a charge, clear sight and an approach outside its vision cone. |
| Mouse + LMB | Guard: aim and shoot. Heavy: face the weak wall and smash. |
| F | Engineer: unlock a marked security door, copy the archive cipher, or operate the vault. Any controlled form can use an inside emergency release. Hold F at the broken overlook to survey below. |
| A / D | Rotate the vault dial counterclockwise / clockwise. On-screen buttons also work. |
| Enter | Confirm a stopped alignment; after both bolts release, turn the handle. |
| F / Escape | Leave the vault controls. E still ejects. |
| R | Retry the whole level. |

Engineer has no shooting or smashing ability. Its tools unlock **Maintenance Access**, **Archive Entry** and **Archive Clue** doors, copy the cipher and operate the vault. Unlocked doors stay open for other forms.

## Routes

- **Reach the upper floor:** Junction Guard hits three targets → gallery door → long gallery → rear stair. Refill at the upper landing and possess the local Engineer.
- **Recover the cipher:** Press F at the Engineer panel to open Archive Entry. Cross the Watch gallery using cover and patrol gaps; two Guards and a Heavy remain active. Unlock Archive Clue with F, then press F at the archive scanner to copy the physical cipher. Recovery saves the clue and releases the return doors. The vault requires this recovered cipher even if you already know the combination.
- **Open service access:** The lower Engineer can unlock Maintenance Access. The alternate combat route remains Service Guard's three targets → Workshop door → Workshop Heavy's wall smash → landing release.
- **Finale:** Bring a surviving Engineer through opened service access to the vault. The lower Engineer can operate it after the upper Engineer recovers the cipher. Opening the vault completes the level; no further exit task.

Selected archive Guards warn with a red lamp for **0.9 seconds** before shooting at a possessed body. Cover or leaving their cone cancels the warning. Shots deal one damage with a **2.4-second cooldown** before the next warning and an **8 m range**. Spirit remains vulnerable to shared enemy attacks after ejection.

## Vault and recovery

Rotate **clockwise to TRIANGLE (60°)**, release the direction key and confirm. Then rotate **counterclockwise to SQUARE (300°)**, release and confirm. Turn the handle to finish. The provisional tolerance is **±12°**, with near-stop snapping.

The **12-second possession timer** continues during travel and vault controls. Stage the body behind cover, eject and refill between legs. Inside emergency releases let Spirit retreat even before copying the cipher. Open doors, a recovered cipher and released vault bolts persist after manual ejection or expiry. Ejection retains the shared two-second shield; forced expiry temporarily prevents repossession of that host.

Both Engineers are ordinary damageable hosts with 3 HP. If all Engineers are lost, **NO ENGINEER LEFT** requires Retry. Charges do not restore defeated hosts. Retry resets hosts, puzzles, pickups, timers and dial progress.

Initial settings: **600-second level timer**, **5 Spirit HP**, **one starting charge / three maximum**, **eight hosts** (four Guards, two Heavies, two Engineers), **six targets**, **two target doors** and **one breakable wall**, plus the three Engineer-operated doors. Charge sources reform after eight seconds; health pickups restore Spirit HP. Difficulty and completion time still need playtesting.
