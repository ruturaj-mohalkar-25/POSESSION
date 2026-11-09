# Campaign objective visibility and HUD consistency

Targets and ready charge pickups receive an automatic depth-tested silhouette. It is visible immediately when an object is behind geometry in the camera view, with a bullseye on targets and the normal shard shape on pickups. Visible objects retain their original materials. Targets lose the silhouette when hit; collected shards lose it until they reform. There are no extra collision volumes, changes to shooting, or off-screen map markers. Walls and locked doors remain solid.

All three campaign stages use the Level 1 HUD style: bottom-centre HP and time, a shared objective bar, body-relative possession cues, cursor ammo and the same end-screen presentation. One top-centre charge strip is attached to the existing HUD canvas and survives scene transitions without duplication. It hides on completion or death. Hunted no longer creates a separate canvas/title/navigation overlay; its real route progress appears in the shared objective bar. R restarts it and C still retries an unlocked courtyard checkpoint.

Use **Possession → Level Design → Sync Campaign HUD with Level 1** to reapply authoring styles. The campaign builders also apply the style through their existing final combat/presentation pass. This operation preserves level-specific objectives, puzzle references, geometry, actor placement, current movement/combat rules and camera settings.

## Validation

Unity 6000.3.22f1: **232/232 full PlayMode checks pass**, no failures or skips. Checks cover immediate target/shard visibility, collected/reforming shards, real projectile obstruction, shared HUD layout across transitions, checkpoint retry, existing wall fading, both Warehouse wings, expanded campaign routes, combat ranges and safe ejection. Geometry/actor/camera signatures were unchanged by the HUD authoring pass in all three saved scenes.

Graphic previews checked all three HUDs, naturally obscured targets/shards and a controlled fully occluded target. With a solid wall in front, its visible red pixels changed from zero to 11,004 when the silhouette was enabled. The independent projectile check confirms the wall still blocks the shot. The current full gameplay validation is documented in [gameplay consistency](Campaign-Gameplay-Consistency.md).

Stop Play Mode and reopen MainMenu or the desired campaign scene before testing saved asset changes. Screenshots and logs are retained in local QA artifacts. No remote push is made for this local work.
