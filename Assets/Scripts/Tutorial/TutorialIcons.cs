using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Small UI icons for the tutorial Field Guide, drawn from simple shapes in code so no art assets are needed.
    // Colours follow the in-game models: white Spirit, navy Guard, gold Heavy, green health, red vision cone.
    public static class TutorialIcons
    {
        public enum Kind { Spirit, Guard, Heavy, VisionCone, PossessionRing, SoulOrb, HealthOrb }

        private static readonly Color Navy = new(0.17f, 0.22f, 0.42f);
        private static readonly Color GoldBody = new(1f, 0.85f, 0.2f);
        private static readonly Color HeavyOrange = new(0.98f, 0.55f, 0.16f);
        private static readonly Color HealthGreen = new(0.36f, 1f, 0.56f);
        private static readonly Color SoulBlue = new(0.55f, 0.82f, 1f);
        private static readonly Color ConeRed = new(1f, 0.32f, 0.3f, 0.75f);
        private static readonly Color Ink = new(0.05f, 0.06f, 0.09f);

        private static Sprite circle, ring, wedge;

        // The cone shape doubles as a downward-pointing arrow (wide at the top, point at the bottom)
        public static Sprite ArrowDown => Wedge;

        public static void Build(RectTransform slot, Kind kind)
        {
            switch (kind)
            {
                case Kind.Spirit:
                    Shape(slot, Circle, new Vector2(0f, 3f), new Vector2(30f, 32f), Color.white);
                    Shape(slot, Circle, new Vector2(0f, -9f), new Vector2(22f, 18f), Color.white);
                    Shape(slot, Circle, new Vector2(-5f, 5f), new Vector2(5f, 8f), Ink);
                    Shape(slot, Circle, new Vector2(5f, 5f), new Vector2(5f, 8f), Ink);
                    break;
                case Kind.Guard:
                    Shape(slot, Circle, new Vector2(0f, -2f), new Vector2(24f, 34f), Navy);
                    Shape(slot, Circle, new Vector2(0f, 12f), new Vector2(18f, 14f), new Color(0.3f, 0.36f, 0.6f));
                    Shape(slot, null, new Vector2(9f, 0f), new Vector2(12f, 4f), Ink); // gun barrel
                    break;
                case Kind.Heavy:
                    // Like the model: orange body, yellow hard hat, hammer held at the side
                    Shape(slot, null, new Vector2(15f, -2f), new Vector2(3f, 22f), new Color(0.45f, 0.3f, 0.18f)); // handle
                    Shape(slot, null, new Vector2(15f, 10f), new Vector2(11f, 7f), new Color(0.55f, 0.58f, 0.62f)); // head
                    Shape(slot, Circle, new Vector2(-2f, -5f), new Vector2(26f, 32f), HeavyOrange);
                    Shape(slot, Circle, new Vector2(-2f, 9f), new Vector2(24f, 16f), GoldBody);               // hat dome
                    Shape(slot, null, new Vector2(-2f, 5f), new Vector2(30f, 4f), new Color(0.9f, 0.68f, 0.1f)); // brim
                    break;
                case Kind.VisionCone:
                    Shape(slot, Wedge, new Vector2(0f, 4f), new Vector2(40f, 34f), ConeRed);
                    Shape(slot, Circle, new Vector2(0f, -14f), new Vector2(12f, 12f), Navy);
                    break;
                case Kind.PossessionRing:
                    Shape(slot, Ring, Vector2.zero, new Vector2(38f, 38f), HealthGreen);
                    Shape(slot, Circle, Vector2.zero, new Vector2(16f, 20f), Navy);
                    break;
                case Kind.SoulOrb:
                    Shape(slot, Circle, Vector2.zero, new Vector2(40f, 40f), new Color(SoulBlue.r, SoulBlue.g, SoulBlue.b, 0.35f));
                    Shape(slot, Circle, Vector2.zero, new Vector2(22f, 24f), Color.white);
                    Shape(slot, Circle, new Vector2(-3f, 2f), new Vector2(3f, 5f), Ink);
                    Shape(slot, Circle, new Vector2(3f, 2f), new Vector2(3f, 5f), Ink);
                    break;
                case Kind.HealthOrb:
                    Shape(slot, Circle, Vector2.zero, new Vector2(40f, 40f), new Color(HealthGreen.r, HealthGreen.g, HealthGreen.b, 0.3f));
                    Shape(slot, null, Vector2.zero, new Vector2(8f, 26f), HealthGreen);
                    Shape(slot, null, Vector2.zero, new Vector2(26f, 8f), HealthGreen);
                    break;
            }
        }

        private static void Shape(RectTransform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject("Shape", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private static Sprite Circle => circle != null ? circle : circle = Make((x, y, r) => Edge(r - Dist(x, y)));
        private static Sprite Ring => ring != null ? ring : ring = Make((x, y, r) =>
            Mathf.Min(Edge(r - Dist(x, y)), Edge(Dist(x, y) - (r - 7f))));
        // A 75° cone opening upward from the bottom centre, like the enemies' vision on the floor
        private static Sprite Wedge => wedge != null ? wedge : wedge = Make((x, y, r) =>
        {
            float dx = x - r, dy = y; // apex at the bottom middle
            float angle = Mathf.Abs(Mathf.Atan2(dx, Mathf.Max(dy, 0.0001f))) * Mathf.Rad2Deg;
            return Mathf.Min(Edge(2f * r - Mathf.Sqrt(dx * dx + dy * dy)), Edge((37.5f - angle) * 0.6f));
        });

        private const int Size = 64;
        private static float Dist(float x, float y) => new Vector2(x - Size * 0.5f, y - Size * 0.5f).magnitude;
        private static float Edge(float d) => Mathf.Clamp01(d + 0.5f);

        private static Sprite Make(System.Func<float, float, float, float> alpha)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[Size * Size];
            float r = Size * 0.5f - 1f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha(x + 0.5f, y + 0.5f, r));
            tex.SetPixels32(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
