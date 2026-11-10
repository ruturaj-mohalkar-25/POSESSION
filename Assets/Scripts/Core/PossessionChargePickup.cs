using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PossessionChargePickup : MonoBehaviour
    {
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 23f;
        [SerializeField] private Renderer shard;
        [SerializeField] private bool soulWispLook = true; // shows a glowing ghost instead of the level's shard model
        [SerializeField, Min(0.1f)] private float wispSize = 1.6f; // same size as the health pickup cross
        [SerializeField, Min(0f)] private float wispHeight = 1.4f;
        [SerializeField] private bool respawnInRoom = true; // re-form at a new random spot in the same room
        private float readyAt;
        private bool consumed;
        private Transform wisp;
        private Renderer wispHead;
        private Renderer[] wispParts;
        private Material wispGlow;
        private Camera cam;
        private Collider homeFloor;
        private Vector3 homePosition;
        private static readonly Color WispGlow = new Color(0.55f, 0.78f, 1f);
        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool Reforms => reforms;
        public bool IsConsumed => consumed;
        public Renderer Visual => wispHead != null ? wispHead : shard;
        private void Awake() { GetComponent<SphereCollider>().isTrigger = true; ObjectiveVisibility.Ensure(this); }
        private void Start()
        {
            homePosition = transform.position;
            homeFloor = PickupRoomRespawn.HomeFloor(transform);
            if (soulWispLook) BuildSoulWisp();
        }
        // Helper pickups spawned during play are taken once and then cleaned up
        public void MakeOneUse() => reforms = false;
        private void Update()
        {
            if (wisp != null) { UpdateSoulWisp(); return; }
            if (shard == null) return;
            shard.enabled = IsReady;
            shard.transform.localPosition = new Vector3(0f, 1.05f + Mathf.Sin(Time.time * 2f) * 0.12f, 0f);
            shard.transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);
        }
        private void OnTriggerEnter(Collider other) => Collect(other);
        private void OnTriggerStay(Collider other) => Collect(other);
        private void Collect(Collider other)
        {
            var charge = other.GetComponentInParent<PossessionCharge>();
            if (charge != null) TryCollect(charge);
        }
        // The trigger collider decides the range; there is no second distance check here
        public bool TryCollect(PossessionCharge charge)
        {
            if (!IsReady || charge == null || !charge.TryCollect()) return false;
            if (reforms) readyAt = Time.time + cooldown; else consumed = true;
            // Hidden while re-forming, so move now: it comes back somewhere new in the same room
            if (reforms && respawnInRoom && PickupRoomRespawn.TryNewSpot(homeFloor, homePosition, transform, out Vector3 spot))
                transform.position = spot;
            return true;
        }

        // ---- Soul Wisp: a mini version of the Spirit, so the collectible reads as "a soul you can take" ----

        private void BuildSoulWisp()
        {
            if (shard != null)
                foreach (var part in shard.GetComponentsInChildren<Renderer>(true)) part.enabled = false;

            var body = RuntimeMaterials.Lit(Color.white, WispGlow);
            body.SetFloat("_Glossiness", 0.15f);
            wispGlow = body;
            var eyes = RuntimeMaterials.Lit(new Color(0.06f, 0.06f, 0.08f));
            var halo = RuntimeMaterials.Transparent(new Color(0.7f, 0.88f, 1f, 0.3f));

            wisp = new GameObject("Soul Wisp").transform;
            wisp.SetParent(transform, false);
            wisp.localPosition = Vector3.up * wispHeight;
            wisp.localScale = Vector3.one * wispSize;

            // Rounded head, droplet tail and a wavy hem, like the Spirit; eyes face the camera (-z)
            wispHead = Part(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.5f, 0.46f), body);
            Part(PrimitiveType.Sphere, new Vector3(0f, -0.2f, 0f), new Vector3(0.34f, 0.32f, 0.34f), body);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Part(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.12f, -0.33f, Mathf.Sin(a) * 0.12f), Vector3.one * 0.15f, body);
            }
            Part(PrimitiveType.Sphere, new Vector3(-0.08f, 0.1f, -0.21f), new Vector3(0.07f, 0.1f, 0.04f), eyes);
            Part(PrimitiveType.Sphere, new Vector3(0.08f, 0.1f, -0.21f), new Vector3(0.07f, 0.1f, 0.04f), eyes);
            var glow = Part(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.15f, halo);
            glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            wispParts = wisp.GetComponentsInChildren<Renderer>(true);
        }

        private Renderer Part(PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            var collider = go.GetComponent<Collider>();
            collider.enabled = false; // purely visual: never blocks the Spirit or aiming
            Destroy(collider);
            go.transform.SetParent(wisp, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return r;
        }

        private void UpdateSoulWisp()
        {
            bool ready = IsReady;
            foreach (var part in wispParts) part.enabled = ready;
            if (!ready) return;

            // Gentle hover and sway, a soft pulsing glow, and the face kept toward the camera
            wisp.localPosition = new Vector3(Mathf.Sin(Time.time * 1.3f) * 0.08f,
                wispHeight + Mathf.Sin(Time.time * 2f) * 0.18f, 0f);
            float pulse = 0.8f + 0.8f * (Mathf.Sin(Time.time * 2.6f) + 1f) * 0.5f; // bright, breathing glow
            RuntimeMaterials.SetEmission(wispGlow, WispGlow * pulse);
            if (cam == null) cam = Camera.main;
            if (cam != null) wisp.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
        }

        private void OnDestroy()
        {
            if (wispParts == null) return;
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (var part in wispParts)
                if (part != null && part.sharedMaterial != null) materials.Add(part.sharedMaterial);
            foreach (var mat in materials) Destroy(mat);
        }
    }
}
