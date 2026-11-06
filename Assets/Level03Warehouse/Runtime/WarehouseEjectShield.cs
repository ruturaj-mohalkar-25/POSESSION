using UnityEngine;

namespace Possession.Warehouse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritController), typeof(PossessionController))]
    public sealed class WarehouseEjectShield : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float protectionSeconds = 2f;
        private SpiritController spirit;
        private PossessionController possession;
        private EnemyBase lastHost;
        private LineRenderer ring;
        private Material material;
        public float ProtectionSeconds => protectionSeconds;
        public bool IsVisible => ring != null && ring.enabled;

        private void Awake()
        {
            spirit = GetComponent<SpiritController>();
            possession = GetComponent<PossessionController>();
            var visual = new GameObject("Eject protection halo");
            visual.transform.SetParent(transform, false);
            ring = visual.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .075f;
            material = new Material(Shader.Find("Sprites/Default"));
            ring.sharedMaterial = material;
            ring.enabled = false;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .85f, -.9f, Mathf.Sin(angle) * .85f));
            }
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
            if (ring != null) ring.enabled = false;
        }
        private void OnPossessed(EnemyBase host, PlayerState state)
        {
            if (possession.PossessedEnemy == host) lastHost = host;
        }
        private void OnEjected(EnemyBase host, bool forced)
        {
            if (host != lastHost || possession.CurrentState != PlayerState.Spirit) return;
            lastHost = null;
            spirit.GrantDamageProtection(protectionSeconds);
            UpdateVisual();
        }
        private void Update() => UpdateVisual();
        private void UpdateVisual()
        {
            if (ring == null) return;
            ring.enabled = possession.CurrentState == PlayerState.Spirit && spirit.CurrentHP > 0 && spirit.IsDamageProtected;
            if (!ring.enabled) return;
            float remaining = Mathf.Clamp01(spirit.DamageProtectionRemaining / Mathf.Max(.01f, protectionSeconds));
            var color = new Color(.65f, .95f, 1f, .3f + remaining * .65f);
            ring.startColor = color;
            ring.endColor = color;
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
