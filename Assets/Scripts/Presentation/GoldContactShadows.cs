using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // A few shared, feathered floor stamps. No lights, physics queries or particle systems.
    [DefaultExecutionOrder(250)]
    public sealed class GoldContactShadows : MonoBehaviour
    {
        private sealed class Stamp
        {
            public Renderer Body, Shadow;
            public PlayerIdentityView Player;
        }
        private readonly List<Stamp> stamps = new();
        private MaterialPropertyBlock block;
        private MeshRenderer[] floors;
        private Mesh mesh;
        public Renderer PlayerShadow { get; private set; }

        public void Build(Scene scene)
        {
            var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
            floors = renderers.Where(GoldRoomPresentation.IsFloor).ToArray();
            var material = Resources.Load<Material>("Gold/ContactShadow");
            if (material == null) return;
            block = new MaterialPropertyBlock();
            mesh = FeatheredDisc();
            foreach (var player in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerIdentityView>(true)))
                PlayerShadow = Add("Gold_PlayerContactShadow", null, player, material);
            foreach (var cabinet in renderers.Where(r => r.name == "Gold_CornerCabinet"))
                Add("Gold_CabinetContactShadow", cabinet, null, material);
            Refresh();
        }

        private Renderer Add(string name, Renderer body, PlayerIdentityView player, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            stamps.Add(new Stamp { Body = body, Player = player, Shadow = renderer });
            return renderer;
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            foreach (var stamp in stamps)
            {
                var body = stamp.Player != null && stamp.Player.TrackedBody != null
                    ? stamp.Player.TrackedBody.GetComponent<Renderer>() : stamp.Body;
                bool visible = body != null && body.enabled && body.gameObject.activeInHierarchy
                    && (stamp.Player == null || stamp.Player.IsShowing);
                Bounds bounds = visible ? body.bounds : default;
                float surface = float.NegativeInfinity;
                if (visible) foreach (var floor in floors)
                {
                    var b = floor.bounds;
                    if (bounds.center.x >= b.min.x && bounds.center.x <= b.max.x && bounds.center.z >= b.min.z
                        && bounds.center.z <= b.max.z && b.max.y <= bounds.min.y + .6f)
                        surface = Mathf.Max(surface, b.max.y);
                }
                visible &= !float.IsNegativeInfinity(surface);
                stamp.Shadow.enabled = visible;
                if (!visible) continue;
                stamp.Shadow.transform.position = new Vector3(bounds.center.x, surface + .025f, bounds.center.z);
                stamp.Shadow.transform.localScale = new Vector3(Mathf.Max(.6f, bounds.size.x * 1.45f), 1,
                    Mathf.Max(.6f, bounds.size.z * 1.45f));
                float opacity = .24f * Mathf.Clamp01(1f - Mathf.Max(0, bounds.min.y - surface) / 4f);
                block.SetColor("_Color", new Color(.07f, .09f, .11f, opacity));
                stamp.Shadow.SetPropertyBlock(block);
            }
        }

        private static Mesh FeatheredDisc()
        {
            const int segments = 24;
            var vertices = new List<Vector3> { Vector3.zero };
            var colors = new List<Color> { Color.white };
            var triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++) for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments, radius = ring == 0 ? .22f : .5f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
                colors.Add(new Color(1, 1, 1, ring == 0 ? .48f : 0));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % segments, c = a + segments, d = b + segments;
                triangles.AddRange(new[] { 0, b, a, a, b, c, c, b, d });
            }
            var result = new Mesh { name = "Soft contact shadow" };
            result.SetVertices(vertices); result.SetColors(colors); result.SetTriangles(triangles, 0); result.RecalculateBounds();
            return result;
        }

        private void OnDestroy()
        {
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }
    }
}
