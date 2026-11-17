using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Small physical nameplates on camera-facing solid walls, never on puzzle objects.
    [DefaultExecutionOrder(1100)]
    public sealed class GoldRoomSigns : MonoBehaviour
    {
        private readonly List<(Renderer wall, GameObject board)> signs = new();
        private Font signFont;
        private Material signText;

        public void Build(Scene scene, MeshRenderer[] floors, MeshRenderer[] walls)
        {
            signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signText = new Material(Resources.Load<Material>("Gold/RoomSignText"));
            signFont.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZ ", 64);
            RefreshFontAtlas(signFont);
            Font.textureRebuilt += RefreshFontAtlas;
            var obstacles = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.GetComponent<Target>() != null || t.GetComponent<Door>() != null
                    || t.GetComponent<BreakableWall>() != null || t.GetComponent<LevelExit>() != null
                    || t.GetComponent<PossessionChargePickup>() != null).Select(t => t.position).ToArray();
            foreach (var floor in floors.OrderBy(f => f.name))
            {
                string label = RoomName(scene.name, floor.name);
                if (label == null || signs.Count >= 8) continue;
                var room = floor.bounds;
                foreach (var wall in walls.OrderBy(w => Mathf.Abs(w.bounds.center.x - room.center.x)))
                {
                    var b = wall.bounds;
                    if (b.size.z > 1f || b.center.z < room.center.z || Mathf.Abs(b.min.z - room.max.z) > .8f) continue;
                    float left = Mathf.Max(b.min.x, room.min.x) + .3f, right = Mathf.Min(b.max.x, room.max.x) - .3f;
                    float width = Mathf.Min(3.8f, right - left);
                    if (width < 2.6f) continue;
                    float x = Mathf.Clamp(room.center.x, left + width * .5f, right - width * .5f);
                    var point = new Vector3(x, Mathf.Min(room.max.y + 3.5f, b.max.y - .8f), b.min.z - .08f);
                    if (obstacles.Any(p => FlatDistance(p, point) < width * .5f + 1.5f)
                        || signs.Any(s => Vector3.Distance(s.board.transform.position, point) < 5f)) continue;
                    Create(wall, point, width, label);
                    break;
                }
            }
        }

        private void Create(Renderer wall, Vector3 point, float width, string label)
        {
            var root = new GameObject("Gold_RoomSign");
            root.layer = 2;
            root.transform.position = point;
            root.transform.SetParent(wall.transform, true);
            GoldRoomPresentation.Part(root.transform, "Nameplate", PrimitiveType.Cube, point,
                new Vector3(width, .9f, .1f), Resources.Load<Material>("Gold/DoorFrame"));
            GoldRoomPresentation.Part(root.transform, "Teal edge", PrimitiveType.Cube,
                point + new Vector3(-width * .5f + .11f, 0, -.06f), new Vector3(.08f, .65f, .025f),
                Resources.Load<Material>("Gold/Teal"));
            var textObject = new GameObject("Room name", typeof(TextMesh));
            textObject.layer = 2;
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = new Vector3(.07f, 0, -.065f);
            var text = textObject.GetComponent<TextMesh>();
            text.text = label; text.font = signFont;
            text.fontSize = 64; text.characterSize = .1f; text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center; text.color = new Color(.94f, .93f, .87f);
            var renderer = text.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = signText;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var size = renderer.bounds.size;
            float fit = Mathf.Min((width - .55f) / Mathf.Max(.01f, size.x), .48f / Mathf.Max(.01f, size.y));
            textObject.transform.localScale = Vector3.one * fit;
            signs.Add((wall, root));
        }

        private void LateUpdate()
        {
            foreach (var sign in signs)
            {
                if (sign.board == null) continue;
                var material = sign.wall != null ? sign.wall.sharedMaterial : null;
                bool faded = material != null && material.HasProperty("_P5OcclusionAlpha")
                    && material.GetFloat("_P5OcclusionAlpha") < .98f;
                sign.board.SetActive(sign.wall != null && sign.wall.enabled && !faded);
            }
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= RefreshFontAtlas;
            foreach (var sign in signs) if (sign.board != null)
                if (Application.isPlaying) Destroy(sign.board); else DestroyImmediate(sign.board);
            if (signText != null) { if (Application.isPlaying) Destroy(signText); else DestroyImmediate(signText); }
        }

        private void RefreshFontAtlas(Font font)
        {
            if (font == signFont && signText != null) signText.mainTexture = font.material.mainTexture;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static string RoomName(string scene, string floor)
        {
            if (scene == CampaignLevels.FirstScene) return floor switch
            {
                "Entry_Floor" => "ENTRY", "Fork_Floor" => "JUNCTION", "Left_Floor" => "WEST WING",
                "Top_Floor" => "NORTH WING", "Right_Floor" => "EAST WING", "Exit_Floor" => "EXIT", _ => null
            };
            if (scene == CampaignLevels.SecondScene) return floor switch
            {
                "R1_Floor" => "ENTRY", "R2_Floor" => "COURTYARD", "R3_Floor" => "NORTH PASSAGE",
                "R4_Floor" => "NORTH STORAGE", "R5_Floor" => "SOUTH PASSAGE", "R6_Floor" => "GATE CHAMBER",
                "R7_Floor" => "EXIT", _ => null
            };
            if (scene == CampaignLevels.ThirdScene && floor.EndsWith("_Floor") && floor != "Hall_Floor"
                && floor != "Recovery_passage_Floor" && floor != "Service_lobby_Floor")
                return floor.Replace("_Floor", "").Replace('_', ' ').ToUpperInvariant();
            return null;
        }
    }
}
