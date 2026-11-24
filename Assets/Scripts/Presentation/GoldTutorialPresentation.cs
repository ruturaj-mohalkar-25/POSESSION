using UnityEngine;

namespace Possession
{
    // Tutorial-only borrowings from Level 1: its wooden house door and its plank floor.
    // Collision, targets and the teaching sequence are left exactly as they are.
    public static class GoldTutorialPresentation
    {
        public static void Apply(GameObject[] roots)
        {
            var planks = Resources.Load<Material>("Gold/FloorWood");
            var frame = Resources.Load<Material>("Gold/DoorFrame");
            foreach (var root in roots)
            {
                if (planks != null)
                    foreach (var floor in root.GetComponentsInChildren<MeshRenderer>(true))
                        if (GoldRoomPresentation.IsFloor(floor)) Plank(floor, planks);
                foreach (var door in root.GetComponentsInChildren<Door>(true))
                    if (door.HasTargets && door.GetComponent<SlidingDoorView>() == null) BuildDoor(door, frame);
            }
        }

        private static void Plank(MeshRenderer floor, Material planks)
        {
            floor.sharedMaterial = planks;
            var block = new MaterialPropertyBlock();
            floor.GetPropertyBlock(block);
            block.SetVector("_MainTex_ST", new Vector4(floor.bounds.size.x / 4f, floor.bounds.size.z / 4f, 0, 0));
            floor.SetPropertyBlock(block);
        }

        // Same leaves, jambs and header as Level 1's sliding door; GoldDoorPresentation then
        // dresses the leaves in wood panels and hides the old green blocker and padlock.
        private static void BuildDoor(Door door, Material frame)
        {
            var box = door.GetComponent<BoxCollider>();
            if (box == null) return;
            var root = new GameObject("Sliding door presentation").transform;
            root.SetParent(door.transform, false);
            root.localPosition = box.center;
            root.localScale = box.size;
            var left = Cube(root, "Left sliding leaf", new Vector3(-.25f, 0, 0), new Vector3(.5f, .92f, .6f), frame);
            var right = Cube(root, "Right sliding leaf", new Vector3(.25f, 0, 0), new Vector3(.5f, .92f, .6f), frame);
            Cube(root, "Door jamb left", new Vector3(-.52f, 0, 0), new Vector3(.04f, 1f, 1.3f), frame);
            Cube(root, "Door jamb right", new Vector3(.52f, 0, 0), new Vector3(.04f, 1f, 1.3f), frame);
            Cube(root, "Door header", new Vector3(0, .475f, 0), new Vector3(1.08f, .05f, 1.3f), frame);
            door.gameObject.AddComponent<SlidingDoorView>().Configure(left, right, 1f, true);
        }

        private static Transform Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.layer = parent.gameObject.layer;
            var collider = obj.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj.transform;
        }
    }
}
