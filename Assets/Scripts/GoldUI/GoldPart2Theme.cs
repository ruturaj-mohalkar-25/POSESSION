using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Part 2 uses the exact palette approved for Gold Part 1 without depending on
    // Part 1's branch. This file can be replaced by GoldTheme after that PR merges.
    public static class GoldPart2Theme
    {
        public static readonly Color Background = new Color(.075f, .085f, .10f, .96f);
        public static readonly Color Panel = new Color(.105f, .12f, .14f, .97f);
        public static readonly Color Secondary = new Color(.22f, .24f, .27f);
        public static readonly Color Teal = new Color(.13f, .61f, .58f);
        public static readonly Color Text = new Color(.95f, .96f, .97f);

        public static void StyleText(Text text, int size, TextAnchor alignment)
        {
            if (text == null) return;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Text;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, size / 2);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static void StyleButton(Button button, bool primary = false)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null) button.targetGraphic = image;

            Color normal = primary ? Teal : Secondary;
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, .18f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(normal, Color.black, .25f);
            colors.disabledColor = new Color(.18f, .19f, .21f, .6f);
            colors.fadeDuration = .08f;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            StyleText(label, 30, TextAnchor.MiddleCenter);
        }
    }
}
