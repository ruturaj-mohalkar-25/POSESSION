using UnityEngine;

namespace Possession
{
    // Slowly pulses a renderer's emission so goal objects (the exit) draw the eye.
    public class PulseGlow : MonoBehaviour
    {
        [SerializeField] private Color glow = new Color(0.2f, 0.9f, 0.35f);
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float min = 0.2f;
        [SerializeField] private float max = 1f;

        private Material mat;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            var r = GetComponent<Renderer>();
            if (r == null) return;
            mat = r.material; // per-object copy so the shared material asset isn't changed
            mat.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            if (mat == null) return;
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            mat.SetColor(EmissionColor, glow * Mathf.Lerp(min, max, t));
        }
    }
}
