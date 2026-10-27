# How to Play practice level

## Open locally

- Project: `/Users/omkarshinde/Documents/POSESSION-P5`
- Branch: `Omkar/how-to-play`
- Unity: **6000.3.22f1**
- Integration base: `8575bab`

Open `Assets/Scenes/MainMenu.unity`, press **Play**, then click **HOW TO PLAY**. The button is installed when the menu runs, so it appears in the Game view during Play Mode. The existing PLAY/level-select flow still works.

You can also open `Assets/Scenes/Tutorial.unity` directly, or select **Possession → Tutorial → Open Practice Level**.

## Lessons

The practice level is a separate copy of Level_01 with the shared Spirit, Guard, Heavy, abilities, damage, possession, puzzle, camera, visibility, and HUD systems. It uses short lesson cards and nearby-enemy explanations for onboarding.

1. Select **START PRACTICE**. Short spotlights say **You have 5 minutes to reach the exit** and **You have 3 HP. Avoid hostile enemies** over the actual HUD widgets. Press **GOT IT** after each. Gameplay and input pause while an explanation is open.
2. Follow the bouncing key prompts in order: **W**, **A**, **S**, **D**, then **SPACE**. Each movement prompt pops in, pulses, and points in the requested direction. Demonstrate the current action to reveal the next key.
3. Approach the Guard. A pulsing halo and large **E** key appear when close enough to possess. After pressing E, a paused spotlight explains the Guard's ability. Press **LET'S SHOOT** to resume.
4. A large **LMB** prompt and pulsing target halo identify what to shoot. Aim with the mouse and **left-click**. The shared target/door logic unlocks the door and a success popup points out the result.
5. After shooting, a short possession-bar spotlight explains **AUTO-EJECT AT ZERO**. Press **WATCH IT RUN OUT** to resume the **12-second practice possession timer**. You can also press **E** to eject safely and continue directly to the Wall Breaker.
6. The final **3–2–1** seconds show a large red countdown and a flashing ring around the possession clock. This works in both the Guard and Wall Breaker.
7. Auto-ejection makes that enemy hostile for **5 seconds**. **AUTO-EJECTED — MOVE AWAY** appears with a red **WASD** prompt. The lesson follows the enemy you actually left, and returns to the next objective after its hostility ends.
8. Go through the unlocked door. The **Wall Breaker** (the shared Heavy enemy) gets its own nearby **E** prompt, possession spotlight, and wall **LMB** highlight. Press **LET'S SMASH**, move close, face the brick wall with WASD, and left-click. Its prompts take priority when you control it, even if optional Guard or movement lessons remain incomplete.
9. Eject back into Spirit form and enter the green exit before the level clock expires. Once the door is open and the wall is broken, entering as the Spirit finishes practice. Optional movement/ejection lesson flags never block an otherwise valid exit.

The **HOW TO PLAY** badge shows the current controlled form and completed-skill count. The regular HUD continues to display health, level time, possession time, and ammo. Spotlight zooms restore each widget's exact original scale when dismissed. Pop effects use unscaled time so they continue animating while gameplay is paused. Damage briefly flashes the actual HP area red.

Redundant old controls/objective text and the regular end panels are hidden in this tutorial scene only; the tutorial provides its own lesson and completion cards. The effects use UI graphics and leave enemy colliders, movement, abilities, and shared HUD scripts unchanged.

## Recovery and replay

- **RESTART** and **MAIN MENU** are available throughout practice.
- If the Spirit dies or time runs out, the retry card restarts the whole practice attempt.
- Guard ejection practice is optional: leaving early lets the player proceed to the Wall Breaker immediately. Returning to the Guard does not lose puzzle progress.
- Empty Guard ammo or a missed Heavy smash changes the large key prompt to **E**, with an eject/repossess hint using the shared refill behavior.
- Completing practice offers **PLAY LEVEL 01**, **REPLAY PRACTICE**, and **MAIN MENU**.

## Build and maintenance

`TutorialBuildRegistration` places Tutorial immediately after MainMenu, before Level_01. This preserves the existing campaign sequence and keeps Level_03 as the last authored campaign level. The registration also repairs this ordering if a shared level/HUD build tool rewrites Build Settings.

Select **Possession → Tutorial → Build or Rebuild Practice Level** after adopting a changed Level_01 layout. Rebuilding replaces the tutorial scene from the current Level_01 and reapplies tutorial settings. It does not save changes to the source level or shared prefabs. Stop Play Mode before rebuilding.

The practice configuration is `Assets/Config/LevelConfig_Tutorial.asset`. Its current values are 300 seconds for the level, 12 seconds per possession, a slower practice Guard, and a short detection range so players can approach before being chased. Enemy health, damage, and hostile behavior remain real shared gameplay behavior.

The existing MainMenu scene and Levels_01–03 are unchanged. The only modified shared asset is Build Settings, adding the Tutorial scene; the implementation, configuration, practice scene, and tests are new files. Nothing is pushed automatically.

## Verification

Automated tests cover the lesson order, movement requirements, early actions, menu entry and duplicate prevention, paused welcome clock, genuine forced ejection and hostility expiry, real Guard damage, shared target/wall/exit events, completion, retry, return to menu, and campaign order.

All **21 tutorial tests pass**. Checks cover sequential key prompts, paused spotlight clocks, HUD zoom and restoration, input restoration, nearby E prompts, ability explanations, cards avoiding the highlighted body, possession-bar focus, empty-ammo recovery prompts, renderable halos, finishing without optional lesson flags, the Wall Breaker route without the Guard ejection checklist, the real shared smash breaking the scene wall, and Wall Breaker countdown/hostility recovery.

The untouched integration baseline has two existing failures: `BarChartTests.CallingShowChartTwiceClearsOldBars` and `HeavyEnemyTests.PatrolMovesAtSixtyPercentOfGuardSpeed`. These shared tests and their implementations are outside the tutorial change.

Scripted screen renders were checked for the menu, welcome, short clock and HP spotlights, movement keys, Guard E and LMB prompts, auto-ejection spotlight/countdown/escape warning, Wall Breaker E prompt and ability spotlight, wall LMB prompt, and completion. A manual WASD/mouse playthrough on the target Game view or WebGL build is still recommended before submitting a PR.
