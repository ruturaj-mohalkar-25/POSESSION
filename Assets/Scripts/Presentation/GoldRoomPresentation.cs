using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Changes renderer materials only. Existing collision, gates, routes and actor poses are retained.
    public sealed class GoldRoomPresentation : MonoBehaviour
    {
        public void Apply(Scene scene)
        {
            var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
            var wood = Resources.Load<Material>("Gold/FloorWood");
            var tile = Resources.Load<Material>("Gold/FloorTile");
            var stone = Resources.Load<Material>("Gold/FloorStone");
            var wall = Resources.Load<Material>("Gold/Wall");
            if (wood == null || tile == null || stone == null || wall == null) return;
            var floors = renderers.Where(IsFloor).OrderBy(r => r.name, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < floors.Length; i++)
            {
                var floor = floors[i];
                bool warehouse = scene.name == CampaignLevels.ThirdScene;
                bool outside = floor.name.IndexOf("courtyard", StringComparison.OrdinalIgnoreCase) >= 0;
                bool passage = floor.name.IndexOf("hall", StringComparison.OrdinalIgnoreCase) >= 0
                    || floor.name.StartsWith("Fork") || floor.name.StartsWith("Exit");
                floor.sharedMaterial = warehouse || outside ? stone : passage || i % 3 == 1 ? tile : wood;
                var block = new MaterialPropertyBlock();
                floor.GetPropertyBlock(block);
                block.SetVector("_MainTex_ST", new Vector4(floor.bounds.size.x / 4f, floor.bounds.size.z / 4f, 0, 0));
                floor.SetPropertyBlock(block);
            }
            foreach (var renderer in renderers.Where(IsWall)) renderer.sharedMaterial = wall;
            gameObject.AddComponent<GoldFloorSurface>().Build(floors);

            // A few low corner landmarks; none have colliders or occupy a doorway/objective.
            int placed = 0;
            foreach (var floor in floors.Where(f => f.bounds.size.x >= 12f && f.bounds.size.z >= 12f))
            {
                if (placed >= 3) break;
                var b = floor.bounds;
                Vector3 point = new Vector3(b.min.x + 1.35f, b.max.y, b.max.z - 1.35f);
                if (!ClearCorner(scene, point)) continue;
                var cabinet = Part(transform, "Gold_CornerCabinet", PrimitiveType.Cube,
                    point + Vector3.up * .55f, new Vector3(1.1f, 1.1f, .7f), Resources.Load<Material>("Gold/Wood"));
                Part(cabinet.transform, "Top", PrimitiveType.Cube, point + Vector3.up * 1.13f,
                    new Vector3(1.2f, .08f, .8f), Resources.Load<Material>("Gold/Wood"));
                placed++;
            }
        }

        public static bool IsFloor(MeshRenderer r) => r.GetComponent<Collider>() != null
            && r.bounds.size.y < .6f && (r.name.EndsWith("_Floor", StringComparison.Ordinal)
                || r.name.StartsWith("Hall_", StringComparison.Ordinal));

        private static bool IsWall(MeshRenderer r)
        {
            if (r.GetComponent<Collider>() == null || r.bounds.size.y < 3f
                || r.GetComponentInParent<EnemyBase>() != null || r.GetComponentInParent<Door>() != null
                || r.GetComponentInParent<BreakableWall>() != null || r.GetComponentInParent<Target>() != null) return false;
            return r.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0
                || r.name.StartsWith("Divider", StringComparison.Ordinal)
                || r.transform.parent != null && r.transform.parent.name == "Walls";
        }

        private static bool ClearCorner(Scene scene, Vector3 point)
        {
            Physics.SyncTransforms();
            // A landmark only goes in a real, closed corner, not at a floor seam or a route junction.
            if (!Physics.Raycast(point + Vector3.up * .8f, Vector3.left, 1.5f, 1, QueryTriggerInteraction.Ignore)
                || !Physics.Raycast(point + Vector3.up * .8f, Vector3.forward, 1.5f, 1, QueryTriggerInteraction.Ignore)) return false;
            if (Physics.CheckBox(point + Vector3.up * .7f, new Vector3(.7f, .6f, .5f),
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if ((t.GetComponent<EnemyBase>() != null || t.GetComponent<SpiritController>() != null
                        || t.GetComponent<Target>() != null || t.GetComponent<Door>() != null
                        || t.GetComponent<BreakableWall>() != null
                        || t.GetComponent<PossessionChargePickup>() != null || t.GetComponent<LevelExit>() != null
                        || t.name.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0)
                        && Vector2.Distance(new Vector2(point.x, point.z), new Vector2(t.position.x, t.position.z)) < 3.5f)
                        return false;
            return true;
        }

        internal static GameObject Part(Transform parent, string name, PrimitiveType type,
            Vector3 worldPosition, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = 2; // Ignore Raycast. Decorative objects never alter aim or movement.
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
    }
}
