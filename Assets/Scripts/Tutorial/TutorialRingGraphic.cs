using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // A vector ring avoids texture assets and stays crisp as the focus pulses.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialRingGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2 outer = r.size * 0.5f;
            Vector2 inner = new Vector2(Mathf.Max(0f, outer.x - 5f), Mathf.Max(0f, outer.y - 5f));
            var vertices = new UIVertex[4];
            var points = new Vector2[4];
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64f, b = (i + 1) * Mathf.PI * 2f / 64f;
                points[0] = r.center + new Vector2(Mathf.Cos(a) * outer.x, Mathf.Sin(a) * outer.y);
                points[1] = r.center + new Vector2(Mathf.Cos(b) * outer.x, Mathf.Sin(b) * outer.y);
                points[2] = r.center + new Vector2(Mathf.Cos(b) * inner.x, Mathf.Sin(b) * inner.y);
                points[3] = r.center + new Vector2(Mathf.Cos(a) * inner.x, Mathf.Sin(a) * inner.y);
                for (int j = 0; j < 4; j++) { vertices[j] = UIVertex.simpleVert; vertices[j].position = points[j]; vertices[j].color = color; }
                vh.AddUIVertexQuad(vertices);
            }
        }
    }
}
