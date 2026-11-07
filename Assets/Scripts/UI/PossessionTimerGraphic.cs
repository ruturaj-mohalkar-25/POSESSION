using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // A clockwise countdown with a fixed outline, so time is readable without text.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PossessionTimerGraphic : MaskableGraphic
    {
        private const int Segments = 64;
        private const float Thickness = 5f;
        private static readonly Color TrackColor = new Color(0.06f, 0.08f, 0.1f, 0.85f);
        public float Fraction { get; private set; } = 1f;

        public void SetProgress(float fraction, Color tint)
        {
            fraction = Mathf.Clamp01(fraction);
            if (Fraction != fraction)
            {
                Fraction = fraction;
                SetVerticesDirty();
            }
            color = tint;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            float inner = Mathf.Max(0f, outer - Thickness);
            AddArc(vh, rect.center, outer, inner, 1f, TrackColor);
            AddArc(vh, rect.center, outer, inner, Fraction, color);
        }

        private static void AddArc(VertexHelper vh, Vector2 center, float outer,
            float inner, float fraction, Color tint)
        {
            int count = Mathf.CeilToInt(Segments * fraction);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * 0.5f - Mathf.PI * 2f * i / Segments;
                float b = Mathf.PI * 0.5f - Mathf.PI * 2f * Mathf.Min((i + 1f) / Segments, fraction);
                Vector2 start = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 end = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int index = vh.currentVertCount;
                vh.AddVert(center + start * outer, tint, Vector2.zero);
                vh.AddVert(center + end * outer, tint, Vector2.zero);
                vh.AddVert(center + end * inner, tint, Vector2.zero);
                vh.AddVert(center + start * inner, tint, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
