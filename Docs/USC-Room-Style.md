# USC room style stretch goal

This optional room dressing adds original cardinal-and-gold campus typography without importing or reproducing a university seal. Large wall-art headlines rotate through **USC**, **FIGHT ON**, **TROJANS**, **SC**, **LA**, and **BEAT UCLA**. Smaller captions provide **UNIVERSITY OF**, **SOUTHERN CALIFORNIA**, and **LOS ANGELES**. Wall TVs use the same palette and a simple USC / Trojans graphic.

Each eligible uninterrupted wall face can now carry artwork: north, south, east and west. Long spans receive multiple pieces, mixing TV screens with framed typography. Artwork reserves clearance around doors, puzzle objects and room nameplates; side-wall meshes and lettering rotate together and face into the room. Text fits using local mesh bounds, so side-wall rotation does not distort lettering. Paintings use a 3.9 × 2.7 m frame and large fitted lettering. Text uses a depth-tested font material and stays attached to its wall. Art hides while the wall fades for player visibility, then returns with the wall. Every art component owns and releases its font material and atlas subscription.

All four closed corners are considered on every floor large enough to hold a prop. There is no seven-room cap. Openings, floor seams, existing corner cabinets, doors, exits, targets, pickups, enemy positions and authored patrol segments retain clearance. Corner dressing alternates artificial plants in cardinal pots with leafy arrangements in cream-and-gold vases. Cardinal woven mats retain a cream border and gold stripes. No sofa is included.

All new parts use Ignore Raycast and have their primitive colliders disabled and removed. Gameplay geometry, enemy behavior, player controls and scene assets are unchanged. Reinstallation does not duplicate props.

## Verification

`Possession.Tests.UscRoomStyleTests` adds checks for visible headline dimensions and artificial vase foliage across the campaign, aggregate coverage of all six primary poster headlines in the actual three campaign layouts while retaining all five prop types per stage, wall-fade restoration, all 36 safe corners in a nine-room fixture, unchanged colliders, idempotence, and clearance from a door and a complete patrol segment. Poster variants advance only when a painting is successfully placed, with a two-poster campaign offset; skipped rooms and TV placement do not consume a poster design.

Run the new suite with the existing Gold presentation, occlusion and door tests. Review normal-camera and wall-art closeup captures in each campaign stage before publishing the stretch build. Integration verification passed 312/312 PlayMode tests. Release WebGL build succeeded with unchanged collider fingerprints in all three campaign scenes and Tutorial. Native captures are in `Docs/StretchGoal/`; these use an inspection camera, with intervening walls temporarily hidden for closeups.
