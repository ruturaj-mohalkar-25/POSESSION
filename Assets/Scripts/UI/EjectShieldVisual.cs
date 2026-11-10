using UnityEngine;

namespace Possession
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritController), typeof(PossessionController))]
    public sealed class EjectShieldVisual : MonoBehaviour
    {
        private SpiritController spirit;
        private PossessionController possession;
        private Renderer bubble;
        private Material bubbleMaterial;
        public float ProtectionSeconds => possession != null ? possession.EjectProtectionSeconds : 2f;
        public bool IsVisible => bubble != null && bubble.enabled;

        private void Awake()
        {
            spirit = GetComponent<SpiritController>();
            possession = GetComponent<PossessionController>();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "EjectShield";
            sphere.layer = 2;
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 1.8f;
            var collider = sphere.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            bubble = sphere.GetComponent<Renderer>();
            bubbleMaterial = new Material(Shader.Find("Sprites/Default"));
            bubbleMaterial.color = new Color(.3f, .85f, 1f, .25f);
            bubble.sharedMaterial = bubbleMaterial;
            bubble.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bubble.receiveShadows = false;
            bubble.enabled = false;
        }
        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
        }
        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            if (bubble != null) bubble.enabled = false;
        }
        // Protection is granted atomically by PossessionController, before any eject callbacks.
        private void OnPossessed(EnemyBase host, PlayerState state) => Refresh();
        private void OnEjected(EnemyBase host, bool forced) => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (bubble == null) return;
            bool visible = possession.CurrentState == PlayerState.Spirit && spirit.CurrentHP > 0 && spirit.IsDamageProtected
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress);
            bubble.enabled = visible;
            if (!visible) return;
            // Original ejection bubble: cyan sphere with a smooth pulse, while the real shield is active.
            var color = bubbleMaterial.color;
            color.a = .15f + .15f * Mathf.Sin(Time.time * 6f);
            bubbleMaterial.color = color;
        }
        private void OnDestroy()
        {
            if (bubbleMaterial != null) Destroy(bubbleMaterial);
        }
    }
}
