using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Shared, deliberately simple Canvas styling. No generated artwork is used in the game.
    public static class GoldTheme
    {
        public static readonly Color Background = new Color(.075f, .085f, .10f);
        public static readonly Color Panel = new Color(.105f, .12f, .14f, .94f);
        public static readonly Color Secondary = new Color(.22f, .24f, .27f);
        public static readonly Color Teal = new Color(.13f, .61f, .58f);
        public static readonly Color TextColor = new Color(.95f, .96f, .97f);

        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var image = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string name, string value, int size,
            Vector2 min, Vector2 max, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = Rect(parent, name, min, max).gameObject.AddComponent<Text>();
            text.text = value;
            Style(text, size, alignment);
            return text;
        }

        public static void Style(Text text, int size, TextAnchor alignment)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = TextColor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            foreach (var shadow in text.GetComponents<Shadow>()) shadow.enabled = false;
        }

        public static Button Button(Transform parent, string name, string label,
            Vector2 min, Vector2 max, bool primary = false)
        {
            var image = Box(parent, name, min, max, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? Teal : Secondary;
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, .18f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, .25f);
            colors.disabledColor = new Color(.18f, .19f, .21f, .6f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            image.raycastTarget = true;
            Label(image.transform, "Label", label, 30, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            return button;
        }

        public static void Fill(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
