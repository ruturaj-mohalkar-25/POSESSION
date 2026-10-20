using UnityEngine;

namespace Possession
{
    // Tiny primitive-based effects: a sphere that grows/shrinks then disappears,
    // and chunks that fly out and fade. No particle systems or extra assets needed.
    public static class SimpleFX
    {
        public static void Burst(Vector3 position, Material material, float startSize, float endSize, float duration)
        {
            if (material == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "FX_Burst";
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            go.transform.position = position;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<FadeAndDie>().Init(Vector3.one * startSize, Vector3.one * endSize, duration);
        }

        public static void Chunks(Vector3 center, Vector3 spread, Material material, int count, Vector3 push)
        {
            if (material == null) return;
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "FX_Chunk";
                go.layer = 2; // Ignore Raycast: never blocks aiming or possession
                var collider = go.GetComponent<Collider>();
                collider.enabled = false;
                Object.Destroy(collider);
                go.transform.position = center + Vector3.Scale(Random.insideUnitSphere, spread);
                go.transform.rotation = Random.rotation;
                float size = Random.Range(0.18f, 0.35f);
                go.transform.localScale = new Vector3(size * 1.6f, size, size);
                go.GetComponent<Renderer>().sharedMaterial = material;

                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.linearVelocity = push + Random.insideUnitSphere * 2.5f + Vector3.up * Random.Range(1f, 3f);
                rb.angularVelocity = Random.insideUnitSphere * 8f;

                go.AddComponent<FadeAndDie>().Init(go.transform.localScale, Vector3.zero, Random.Range(1.2f, 1.8f), 0.6f);
            }
        }
    }

    public class FadeAndDie : MonoBehaviour
    {
        private Vector3 startScale, endScale;
        private float duration, holdFraction, age;

        // holdFraction: share of the lifetime spent at full size before shrinking
        public void Init(Vector3 startScale, Vector3 endScale, float duration, float holdFraction = 0f)
        {
            this.startScale = startScale;
            this.endScale = endScale;
            this.duration = Mathf.Max(0.01f, duration);
            this.holdFraction = holdFraction;
            transform.localScale = startScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            float shrink = Mathf.InverseLerp(holdFraction, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, shrink);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
