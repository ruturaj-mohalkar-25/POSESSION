using UnityEngine;

namespace Possession
{
    public sealed class TutorialPopIn : MonoBehaviour
    {
        private float began;
        private Vector3 original;
        private bool initialized;
        private void Awake() { original = transform.localScale; initialized = true; }
        private void OnEnable() => Play();
        public void Play() => began = Time.unscaledTime;
        private void LateUpdate()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - began) / 0.38f);
            float s = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            transform.localScale = original * Mathf.LerpUnclamped(0.76f, 1f, s);
        }
        private void OnDisable() { if (initialized) transform.localScale = original; }
    }
}
