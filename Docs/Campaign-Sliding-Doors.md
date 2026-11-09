# Campaign sliding doors

Levels 1 and 2 now present target-locked Guard puzzle doors using the same two-panel opening animation as Warehouse (Level 3). The old solid-colour door slab and padlock model are hidden. Each doorway has a steel frame, two sliding leaves and one signal lamp per required target. Lamps turn green as their own targets are hit; both leaves slide into the sides over 0.4 seconds when the last required target is hit. Thin-wall doors clip the visible panels to the frame while retracting, so leaves do not stick into empty space or neighbouring rooms.

The shared `SlidingDoorView` handles all three stages. Warehouse's existing component and saved references remain compatible through a thin subclass, retaining its authored geometry and group colours. The original shared `Door` controls unlocking, collision and the single `DoorOpened` event. Target requirements, exit conditions, breakable barriers, room sizes and actor settings are retained. Visual parts add no colliders. Animation follows game time and stops when paused. Retry restores the closed door and unhit targets.

Stop Play Mode, reopen MainMenu, then play Levels 1 and 2. Shoot the door's required target(s) using a possessed Guard. Check that its lamps turn green, both panels visibly retract and the passage is clear. Retry to reset the puzzle. Level 3 uses the same animation as before.

The campaign builders apply this presentation through their existing final combat/profile pass. To update saved scenes without rebuilding rooms, use **Possession → Level Design → Sync Campaign Sliding Doors**. This pass rebinds each door to its own targets, including doors cloned by the Hunted builder, and replaces previous presentation parts rather than duplicating them.

## Validation

Unity **6000.3.22f1**: **232/232 full PlayMode tests passed**, no failures or skips. The three new checks cover saved door/target bindings and physical blockers in every campaign stage, target-specific unlocking and lamp feedback, animated full-width clearance, single unlock events, pause and retry reset. Graphic previews capture closed and open doors in all three stages.

Changes remain local; no push or PR was made.
