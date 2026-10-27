using UnityEngine;

namespace Possession
{
    // Soft, slow glow on an interactive object (target, breakable wall) so it draws the eye
    // without a text label. A target stops glowing once it has been hit.
    public class HintGlow : MonoBehaviour
    {
        [SerializeField] private Color glow = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private float speed = 2.2f;
        [SerializeField] private float min = 0.15f;
        [SerializeField] private float max = 0.85f;

        private Material[] mats;
        private Target target;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            target = GetComponent<Target>();
            var renderers = GetComponentsInChildren<Renderer>();
            mats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                mats[i] = renderers[i].material; // per-object copy so shared material assets stay unchanged
                mats[i].EnableKeyword("_EMISSION");
            }
        }

        private void Update()
        {
            if (mats == null) return;

            if (target != null && target.IsHit)
            {
                SetEmission(Color.black);
                enabled = false;
                return;
            }

            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            SetEmission(glow * Mathf.Lerp(min, max, t));
        }

        private void SetEmission(Color color)
        {
            foreach (var mat in mats)
                if (mat != null) mat.SetColor(EmissionColor, color);
        }
    }
}
