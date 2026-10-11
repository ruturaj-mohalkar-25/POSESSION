using UnityEngine;

namespace Possession
{
    // SpiritController only hides its own renderer while possessing.
    // This keeps the ghost's extra parts (eyes, hem) in sync with it.
    public class SpiritVisualSync : MonoBehaviour
    {
        private Renderer body;
        private Renderer[] parts;

        private void Awake()
        {
            body = GetComponent<Renderer>();
            var all = GetComponentsInChildren<Renderer>(true);
            parts = System.Array.FindAll(all, r => r != body);
        }

        private void LateUpdate()
        {
            if (body == null) return;
            bool visible = body.enabled;
            foreach (var part in parts)
                if (part.enabled != visible) part.enabled = visible;
        }
    }
}
