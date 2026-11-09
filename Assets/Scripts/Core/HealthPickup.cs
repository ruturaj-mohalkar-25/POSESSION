using UnityEngine;

namespace Possession
{
    // Glowing green "+" that restores some of the Spirit's HP. Only the Spirit can take it,
    // only while hurt (so it is never wasted), and it re-forms after a cooldown.
    [RequireComponent(typeof(SphereCollider))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int healAmount = 2;
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 15f;
        [SerializeField, Min(0.1f)] private float size = 1.6f;   // large for now so it is easy to spot while testing
        [SerializeField, Min(0f)] private float height = 1.4f;

        private static readonly Color CrossColor = new Color(0.3f, 1f, 0.5f);
        private float readyAt;
        private bool consumed;
        private Transform cross;
        private Renderer[] parts;
        private Material glow;
        [SerializeField] private bool respawnInRoom = true; // re-form at a new random spot in the same room
        private Collider homeFloor;
        private Vector3 homePosition;

        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool IsConsumed => consumed;

        // Helper pickups spawned during play are taken once and then cleaned up
        public void MakeOneUse() => reforms = false;

        private void Awake()
        {
            var trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            trigger.center = Vector3.up * height;
        }

        private void Start()
        {
            homePosition = transform.position;
            homeFloor = PickupRoomRespawn.HomeFloor(transform);
            BuildCross();
        }

        private void OnTriggerEnter(Collider other) => Collect(other);
        private void OnTriggerStay(Collider other) => Collect(other);

        private void Collect(Collider other)
        {
            if (!IsReady) return;
            var spirit = other.GetComponentInParent<SpiritController>();
            if (spirit == null || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return;
            if (!spirit.Heal(healAmount)) return;
            if (reforms) readyAt = Time.time + cooldown; else consumed = true;
            // Hidden while re-forming, so move now: it comes back somewhere new in the same room
            if (reforms && respawnInRoom && PickupRoomRespawn.TryNewSpot(homeFloor, homePosition, transform, out Vector3 spot))
                transform.position = spot;
        }

        private void BuildCross()
        {
            glow = new Material(Shader.Find("Standard")) { color = CrossColor };
            glow.SetFloat("_Glossiness", 0.2f);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", CrossColor);
            var halo = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.45f, 1f, 0.6f, 0.25f) };

            cross = new GameObject("Health Cross").transform;
            cross.SetParent(transform, false);
            cross.localPosition = Vector3.up * height;
            cross.localScale = Vector3.one * size;

            Part(PrimitiveType.Cube, new Vector3(0.18f, 0.6f, 0.18f), glow);
            Part(PrimitiveType.Cube, new Vector3(0.6f, 0.18f, 0.18f), glow);
            Part(PrimitiveType.Sphere, Vector3.one * 0.95f, halo).shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            parts = cross.GetComponentsInChildren<Renderer>(true);
        }

        private Renderer Part(PrimitiveType type, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            var collider = go.GetComponent<Collider>();
            collider.enabled = false; // purely visual: never blocks the Spirit or aiming
            Destroy(collider);
            go.transform.SetParent(cross, false);
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return r;
        }

        private void Update()
        {
            if (cross == null) return;
            bool ready = IsReady;
            foreach (var part in parts) part.enabled = ready;
            if (!ready) return;

            // Hover, slow spin and a breathing glow
            cross.localPosition = Vector3.up * (height + Mathf.Sin(Time.time * 2f) * 0.15f);
            cross.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
            float pulse = 0.6f + 0.8f * (Mathf.Sin(Time.time * 2.6f) + 1f) * 0.5f;
            glow.SetColor("_EmissionColor", CrossColor * pulse);
        }

        private void OnDestroy()
        {
            if (parts == null) return;
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (var part in parts)
                if (part != null && part.sharedMaterial != null) materials.Add(part.sharedMaterial);
            foreach (var mat in materials) Destroy(mat);
        }
    }
}
