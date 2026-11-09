using UnityEngine;

namespace Possession
{
    public class Target : MonoBehaviour
    {
        [SerializeField] private bool isHit;

        public bool IsHit => isHit;

        private void Awake() => ObjectiveVisibility.Ensure(this);

        public void OnHit()
        {
            if (isHit) return;
            isHit = true;

            // Visual feedback
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.green;

            EventBus.TargetHit?.Invoke(this);
        }
    }
}
