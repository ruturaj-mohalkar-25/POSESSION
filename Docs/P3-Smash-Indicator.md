# W0: Heavy smash direction indicator

While possessing a Heavy, an amber ground arc and arrow show its facing and
maximum forward smash reach. The guide follows movement and rotation, uses
world units regardless of model scale, and dims during cooldown or when the
single smash use has been spent. Repossessing refreshes the ability through
the existing possession system.

The guide hides on manual or forced ejection, death, level completion/failure,
and component disable. Runtime installation supports saved scenes, restarts
that clear EventBus, and dynamically spawned Heavies without prefab or scene
changes. Guards do not show the guide.

The read-only `SmashAbility.Range` property reports `smashRange + smashRadius`,
matching the existing sphere cast's maximum forward reach. Hit detection,
damage, enemy AI, and possession behavior are unchanged. The arc is an aiming
aid; it does not guarantee a hit through obstacles or represent a damage cone.
The visual adds no colliders. Its material and shader live in `Assets/Resources/P3`.

## Test

In Unity 6000.3.22f1, open Level_01 and press Play. Possess the Heavy, turn with
WASD, and check the ground guide. Smash with LMB: it should dim and stay dim
while ammo is empty. Eject, possess again, and check that it becomes ready.
Check manual and timed ejection, restart, and entering the level from MainMenu.

Run `Possession.Tests.HeavySmashIndicatorTests` in the PlayMode Test Runner.
Eight tests cover possession transitions, Guard exclusion, range/facing/scale,
cooldown, exhausted ammo, disable/re-enable, death/end events, scene installation,
EventBus resets, and spawned Heavies.
