using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession.Level04Gold
{
    // Level 4 adapts the campaign's shared presentation to its two independent storeys.
    // Original collision, floor heights, routes, puzzle state and character materials stay owned by gameplay.
    [DisallowMultipleComponent, DefaultExecutionOrder(1100)]
    public sealed class Level4WorldPresentation : MonoBehaviour
    {
        private readonly List<Renderer> gatePanels = new List<Renderer>();
        private readonly List<(Renderer wall, GameObject board)> boards = new List<(Renderer, GameObject)>();
        private Mesh cube;
        private Font signFont;
        private Material signText;
        private Material wood, tile, stone, wall, frame, face, brass, teal;

        public bool IsApplied { get; private set; }
        public IReadOnlyList<Renderer> GatePanels => gatePanels;

        private void Start() => Apply(gameObject.scene);

        public void Apply(Scene scene)
        {
            if (IsApplied || !scene.IsValid() || !scene.isLoaded || scene != gameObject.scene) return;
            var roots = scene.GetRootGameObjects();
            var geometry = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Level04_Geometry");
            if (geometry == null) return;
            wood = Resources.Load<Material>("Gold/Wood");
            tile = Resources.Load<Material>("Gold/FloorTile");
            stone = Resources.Load<Material>("Gold/FloorStone");
            wall = Resources.Load<Material>("Gold/Wall");
            frame = Resources.Load<Material>("Gold/DoorFrame");
            face = Resources.Load<Material>("Gold/DoorFace");
            brass = Resources.Load<Material>("Gold/DoorBrass");
            teal = Resources.Load<Material>("Gold/Teal");
            var floorWood = Resources.Load<Material>("Gold/FloorWood");
            if (wood == null || floorWood == null || tile == null || stone == null || wall == null
                || frame == null || face == null || brass == null || teal == null) return;

            var renderers = geometry.GetComponentsInChildren<MeshRenderer>(true);
            var floors = renderers.Where(r => r.name.StartsWith("Floor_", StringComparison.Ordinal)
                && r.GetComponent<Collider>() != null && r.bounds.size.y < .6f).ToArray();
            if (floors.Length == 0) return;
            // A static floor's sharedMesh may be Unity's combined world-space batch.
            // Presentation pieces require their own unit geometry, never a scene renderer's mesh.
            if (cube == null) cube = CreateUnitCube();

            foreach (var floor in floors)
            {
                string name = floor.name.Substring("Floor_".Length);
                floor.sharedMaterial = name == "Entry" || name == "EngineerBay" || name == "Records"
                    ? floorWood : name == "Workshop" || name == "RearStair" || name == "Maintenance"
                        || name == "LongGallery" ? stone : tile;
            }
            foreach (var renderer in renderers)
            {
                if (renderer.name.StartsWith("Wall_", StringComparison.Ordinal))
                    renderer.sharedMaterial = renderer.name.StartsWith("Wall_Cap_", StringComparison.Ordinal) ? frame : wall;
                else if (renderer.name.StartsWith("Cover_", StringComparison.Ordinal))
                    renderer.sharedMaterial = renderer.name.StartsWith("Cover_Rim_", StringComparison.Ordinal) ? frame : wood;
                else if (renderer.name.StartsWith("Stair_Tread_", StringComparison.Ordinal)
                    || renderer.name == "Stair_Ramp_WalkSurface") renderer.sharedMaterial = stone;
                else if (renderer.name == "Stair_Rail_Post" || renderer.name.StartsWith("Maintenance_Arch_Post", StringComparison.Ordinal))
                    renderer.sharedMaterial = frame;
                else if (renderer.name == "Stair_Handrail") renderer.sharedMaterial = brass;
            }

            // Separate hosts keep generated upper-floor patches in the visibility component's subtree.
            foreach (string storey in new[] { "Lower floor", "Upper floor" })
            {
                var parent = geometry.Find(storey);
                if (parent == null) continue;
                var group = floors.Where(f => f.transform.IsChildOf(parent)).ToArray();
                if (group.Length == 0) continue;
                var surface = new GameObject("Level4_GoldFloorSurface");
                surface.transform.SetParent(parent, false);
                surface.AddComponent<GoldFloorSurface>().Build(group);
            }

            foreach (var player in roots.SelectMany(r => r.GetComponentsInChildren<PossessionController>(true)))
            {
                var identity = player.GetComponent<PlayerIdentityView>();
                if (identity == null) identity = player.gameObject.AddComponent<PlayerIdentityView>();
                identity.Refresh();
            }
            foreach (var door in roots.SelectMany(r => r.GetComponentsInChildren<SlidingDoorView>(true)))
            {
                var skin = door.GetComponent<GoldDoorPresentation>();
                if (skin == null) skin = door.gameObject.AddComponent<GoldDoorPresentation>();
                if (skin.Panels.Count == 0) skin.Build();
                foreach (var renderer in door.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.name.StartsWith("Frame ", StringComparison.Ordinal)) renderer.sharedMaterial = frame;
            }
            foreach (var gate in roots.SelectMany(r => r.GetComponentsInChildren<Level4AccessGate>(true)))
                StyleGate(gate);
            foreach (var renderer in roots.SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)))
                if (renderer.name.StartsWith("Vault frame ", StringComparison.Ordinal)) renderer.sharedMaterial = frame;

            StyleSigns(roots, renderers);
            var shadows = GetComponent<GoldContactShadows>();
            if (shadows == null)
            {
                shadows = gameObject.AddComponent<GoldContactShadows>();
                shadows.Build(scene);
            }
            shadows.Refresh();
            IsApplied = true;
        }

        private void StyleGate(Level4AccessGate gate)
        {
            Transform panel = gate.MovingPanel;
            if (panel == null || panel.Find("Level4_GoldGatePanels") != null) return;
            var renderer = panel.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = wood;
            foreach (var existing in panel.GetComponentsInChildren<MeshRenderer>(true))
                if (existing.transform != panel) existing.enabled = false;
            foreach (var existing in gate.GetComponentsInChildren<MeshRenderer>(true))
                if (existing.name == "Left jamb" || existing.name == "Right jamb") existing.sharedMaterial = frame;

            var details = new GameObject("Level4_GoldGatePanels").transform;
            details.SetParent(panel, false);
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float x in new[] { -.25f, .25f })
                {
                    foreach (float y in new[] { -.25f, .19f })
                        GatePart(details, "Raised wood panel", new Vector3(x, y, side * .56f),
                            new Vector3(.36f, .31f, .10f), face);
                    float handleX = x - Mathf.Sign(x) * .195f;
                    GatePart(details, "Handle backplate", new Vector3(handleX, -.04f, side * .57f),
                        new Vector3(.04f, .17f, .10f), frame);
                    GatePart(details, "Door handle", new Vector3(handleX, -.04f, side * .67f),
                        new Vector3(.05f, .045f, .10f), brass);
                }
                GatePart(details, "Meeting stile", new Vector3(0, 0, side * .56f),
                    new Vector3(.012f, .94f, .10f), wood);
            }
        }

        private void GatePart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var renderer = Part(parent, name, position, scale, material);
            gatePanels.Add(renderer);
        }

        private MeshRenderer Part(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            // Reuse our owned unit cube: decorative parts never create even a temporary Collider.
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<MeshFilter>().sharedMesh = cube;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Mesh CreateUnitCube()
        {
            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var uv = new Vector2[24];
            var triangles = new int[36];
            Vector3[] normal = { Vector3.forward, Vector3.back, Vector3.right,
                Vector3.left, Vector3.up, Vector3.down };
            Vector3[] across = { Vector3.right, Vector3.left, Vector3.back,
                Vector3.forward, Vector3.right, Vector3.right };
            Vector3[] upwards = { Vector3.up, Vector3.up, Vector3.up,
                Vector3.up, Vector3.back, Vector3.forward };
            Vector2[] corners = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            for (int face = 0; face < 6; face++)
            {
                Vector3 centre = normal[face] * .5f;
                Vector3 u = across[face] * .5f, v = upwards[face] * .5f;
                int first = face * 4, triangle = face * 6;
                vertices[first] = centre - u - v;
                vertices[first + 1] = centre + u - v;
                vertices[first + 2] = centre + u + v;
                vertices[first + 3] = centre - u + v;
                for (int corner = 0; corner < 4; corner++)
                {
                    normals[first + corner] = normal[face];
                    uv[first + corner] = corners[corner];
                }
                triangles[triangle] = first;
                triangles[triangle + 1] = first + 1;
                triangles[triangle + 2] = first + 2;
                triangles[triangle + 3] = first;
                triangles[triangle + 4] = first + 2;
                triangles[triangle + 5] = first + 3;
            }
            return new Mesh
            {
                name = "Level4 Presentation Unit Cube",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                normals = normals,
                uv = uv,
                triangles = triangles,
                bounds = new Bounds(Vector3.zero, Vector3.one)
            };
        }

        private void StyleSigns(GameObject[] roots, MeshRenderer[] geometryRenderers)
        {
            var signs = roots.SelectMany(r => r.GetComponentsInChildren<TextMesh>(true))
                .Where(t => t.GetComponentInParent<Level4Cipher>() == null
                    && (t.text == "ENGINEER" || t.text == "MAINTENANCE" || t.text == "NORTH GALLERY"
                        || t.text == "ARCHIVE / ENGINEER" || t.text == "VAULT")).ToArray();
            if (signs.Length == 0) return;
            signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var sourceMaterial = Resources.Load<Material>("Gold/RoomSignText");
            if (sourceMaterial == null || signFont == null) return;
            signText = new Material(sourceMaterial);
            signFont.RequestCharactersInTexture(string.Join(" ", signs.Select(s => s.text)), 64);
            RefreshFontAtlas(signFont);
            Font.textureRebuilt += RefreshFontAtlas;
            var walls = geometryRenderers.Where(r => r.name.StartsWith("Wall_", StringComparison.Ordinal)
                && !r.name.StartsWith("Wall_Cap_", StringComparison.Ordinal) && r.GetComponent<Collider>() != null).ToArray();
            foreach (var sign in signs)
            {
                Vector3 position = sign.transform.position;
                var backingWall = walls.Where(w => position.y >= w.bounds.min.y && position.y <= w.bounds.max.y
                    && position.x >= w.bounds.min.x && position.x <= w.bounds.max.x)
                    .OrderBy(w => Mathf.Abs(position.z - w.bounds.min.z)).FirstOrDefault();
                if (backingWall != null && Mathf.Abs(position.z - backingWall.bounds.min.z) > .8f) backingWall = null;
                var board = new GameObject("Gold_RoomSign") { layer = 2 };
                board.transform.SetPositionAndRotation(position, sign.transform.rotation);
                board.transform.SetParent(backingWall != null ? backingWall.transform : sign.transform.parent, true);
                float width = sign.text.Length > 15 ? 3.8f : Mathf.Clamp(sign.text.Length * .23f + .7f, 2.6f, 3.8f);
                Part(board.transform, "Nameplate", Vector3.zero, new Vector3(width, .9f, .1f), frame);
                Part(board.transform, "Teal edge", new Vector3(-width * .5f + .11f, 0, -.06f),
                    new Vector3(.08f, .65f, .025f), sign.text.Contains("ENGINEER") ? brass : teal);
                sign.transform.SetParent(board.transform, false);
                sign.transform.localPosition = new Vector3(.07f, 0, -.065f);
                sign.transform.localRotation = Quaternion.identity;
                sign.transform.localScale = Vector3.one;
                sign.font = signFont; sign.fontSize = 64; sign.characterSize = .1f;
                sign.anchor = TextAnchor.MiddleCenter; sign.alignment = TextAlignment.Center;
                sign.color = sign.text.Contains("ENGINEER") ? new Color(1f, .78f, .32f) : new Color(.94f, .93f, .87f);
                var textRenderer = sign.GetComponent<MeshRenderer>();
                textRenderer.sharedMaterial = signText; textRenderer.shadowCastingMode = ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
                var size = textRenderer.bounds.size;
                float fit = Mathf.Min((width - .55f) / Mathf.Max(.01f, size.x), .48f / Mathf.Max(.01f, size.y));
                sign.transform.localScale = Vector3.one * fit;
                boards.Add((backingWall, board));
            }
        }

        private void LateUpdate()
        {
            foreach (var entry in boards)
            {
                if (entry.board == null || entry.wall == null) continue;
                var material = entry.wall.sharedMaterial;
                bool faded = material != null && material.HasProperty("_P5OcclusionAlpha")
                    && material.GetFloat("_P5OcclusionAlpha") < .98f;
                entry.board.SetActive(entry.wall.enabled && !faded);
            }
        }

        private void RefreshFontAtlas(Font font)
        {
            if (font == signFont && signText != null) signText.mainTexture = font.material.mainTexture;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= RefreshFontAtlas;
            if (signText != null)
            {
                if (Application.isPlaying) Destroy(signText); else DestroyImmediate(signText);
            }
            if (cube != null)
            {
                if (Application.isPlaying) Destroy(cube); else DestroyImmediate(cube);
            }
        }
    }
}
