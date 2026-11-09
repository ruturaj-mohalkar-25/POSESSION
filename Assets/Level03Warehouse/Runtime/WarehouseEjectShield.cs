using UnityEngine;

namespace Possession.Warehouse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpiritController), typeof(PossessionController))]
    public sealed class WarehouseEjectShield : MonoBehaviour
    {
        private SpiritController spirit;
        private PossessionController possession;
        private LineRenderer ring, arc;
        private Renderer bubble;
        private Material lineMaterial, bubbleMaterial;
        public float ProtectionSeconds => possession != null ? possession.EjectProtectionSeconds : 2f;
        public bool IsVisible => ring != null && ring.enabled;

        private void Awake()
        {
            spirit = GetComponent<SpiritController>();
            possession = GetComponent<PossessionController>();
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            ring = MakeLine("Eject shield countdown", .12f);
            arc = MakeLine("Eject shield arch", .085f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Eject shield bubble";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = Vector3.up * .1f;
            sphere.transform.localScale = new Vector3(1.75f, 2.35f, 1.75f);
            var collider = sphere.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            bubble = sphere.GetComponent<Renderer>();
            bubbleMaterial = new Material(Shader.Find("Sprites/Default"));
            bubbleMaterial.color = new Color(.25f, .8f, 1f, .13f);
            bubble.sharedMaterial = bubbleMaterial;
            bubble.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bubble.receiveShadows = false;
            bubble.enabled = false;
        }
        private LineRenderer MakeLine(string name, float width)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(transform, false);
            var line = visual.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 49;
            line.widthMultiplier = width;
            line.sharedMaterial = lineMaterial;
            line.enabled = false;
            return line;
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
            if (arc != null) arc.enabled = false;
            if (bubble != null) bubble.enabled = false;
        }
        // Protection is granted atomically by PossessionController, before any eject callbacks.
        private void OnPossessed(EnemyBase host, PlayerState state) => UpdateVisual();
        private void OnEjected(EnemyBase host, bool forced) => UpdateVisual();
        private void Update() => UpdateVisual();
        private void UpdateVisual()
        {
            if (ring == null) return;
            bool visible = possession.CurrentState == PlayerState.Spirit && spirit.CurrentHP > 0 && spirit.IsDamageProtected;
            ring.enabled = arc.enabled = visible;
            // Use integration's shared bubble while its cooldown shield is active.
            bubble.enabled = visible && !possession.IsEjectShielded;
            if (!visible) return;
            float remaining = Mathf.Clamp01(spirit.DamageProtectionRemaining / ProtectionSeconds);
            var color = new Color(.35f, .9f, 1f, .65f + remaining * .35f);
            ring.startColor = ring.endColor = arc.startColor = arc.endColor = color;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f * remaining / (ring.positionCount - 1);
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .95f, -.9f, Mathf.Sin(angle) * .95f));
                float arch = i * Mathf.PI / (ring.positionCount - 1);
                arc.SetPosition(i, new Vector3(Mathf.Cos(arch) * .9f, -.85f + Mathf.Sin(arch) * 2.15f, 0f));
            }
        }
        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
            if (bubbleMaterial != null) Destroy(bubbleMaterial);
        }
    }
}
