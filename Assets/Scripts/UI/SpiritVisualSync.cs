using UnityEngine;

namespace Possession
{
    // Only the authored ghost model follows the body; shields and carried charges own their visibility.
    public class SpiritVisualSync : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        private Renderer body;
        private Renderer[] parts;

        private void Awake()
        {
            body = GetComponent<Renderer>();
            if (modelRoot == null) modelRoot = transform.Find("P5_Model");
            parts = modelRoot != null ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();
        }

        private void LateUpdate()
        {
            if (body == null) return;
            bool visible = body.enabled;
            foreach (var part in parts)
                if (part != null && part.enabled != visible) part.enabled = visible;
        }
    }
}
