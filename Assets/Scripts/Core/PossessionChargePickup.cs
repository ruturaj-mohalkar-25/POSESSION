using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Makes sure every level has collectibles the Spirit can reach from where it starts:
    // the possession pickup nearest the Spirit is moved a few metres away from it, inside the same room.
    // Fixed health pickups get their own spots (one in every second floor area), and Level 3's reserved
    // health spots ("Health_Pickup_Placement") get a real health pickup.
    public static class StartRoomPickupPlacer
    {
        const float Distance = 3.5f;
        const float Clearance = 0.6f;
        const float MinGapBetweenPickups = 2.5f;
        const float MinFloorSize = 4f;   // a collider at least this wide and deep (and thin) counts as a floor area
        const int MaxFixedHealth = 4;
        const float KeepAway = 3f;
        const string HealthSocketName = "Health_Pickup_Placement";
        const string FixedHealthName = "Fixed Health Pickup";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Place();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Place();

        private static void Place()
        {
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            if (spirit == null) return;
            Vector3 start = spirit.transform.position;

            Physics.SyncTransforms();
            Vector3 forward = spirit.transform.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            // Possession pickup: move the nearest one into the start room
            var pickups = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None);
            if (pickups.Length > 0)
            {
                PossessionChargePickup nearest = null;
                float best = float.MaxValue;
                foreach (var p in pickups)
                {
                    float d = (p.transform.position - start).sqrMagnitude;
                    if (d < best) { best = d; nearest = p; }
                }
                if (TryStartRoomSpot(spirit, start, forward, null, out Vector3 spot))
                    nearest.transform.position = spot;
            }

            // Fixed health pickups in their own spots: one in every second floor area (rooms and halls),
            // the same every time the level is played. More appear only when needed (PickupHelper).
            if (GameObject.Find(FixedHealthName) == null)
                PlaceFixedHealth(spirit, start, pickups);

            // Level 3 reserved health spots: swap the placeholder cross for a working pickup
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != HealthSocketName || t.GetComponent<HealthPickup>() != null) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                t.gameObject.AddComponent<HealthPickup>();
            }
        }

        private static void PlaceFixedHealth(SpiritController spirit, Vector3 start, PossessionChargePickup[] coins)
        {
            // Floor areas: wide, thin, flat colliders (rooms and halls), in a fixed order
            var floors = new System.Collections.Generic.List<Collider>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c.isTrigger || !c.enabled || c.transform.IsChildOf(spirit.transform)) continue;
                var b = c.bounds;
                if (b.size.x >= MinFloorSize && b.size.z >= MinFloorSize && b.size.y <= 1f) floors.Add(c);
            }
            floors.Sort((a, b) =>
            {
                int z = a.bounds.center.z.CompareTo(b.bounds.center.z);
                return z != 0 ? z : a.bounds.center.x.CompareTo(b.bounds.center.x);
            });

            int placed = 0;
            for (int i = 0; i < floors.Count && placed < MaxFixedHealth; i++)
            {
                if (floors.Count > 1 && i % 2 == 0) continue;
                if (TryFloorAreaSpot(floors[i], start, coins, out Vector3 spot))
                {
                    var health = new GameObject(FixedHealthName);
                    SceneManager.MoveGameObjectToScene(health, spirit.gameObject.scene);
                    health.transform.position = spot;
                    health.AddComponent<HealthPickup>();
                    placed++;
                }
            }
        }

        // The centre of the floor area first, then points around it; must be on this floor, clear of walls
        // and props, and keep away from possession pickups and the Spirit's start
        private static bool TryFloorAreaSpot(Collider floor, Vector3 start, PossessionChargePickup[] coins, out Vector3 spot)
        {
            var b = floor.bounds;
            Vector2[] offsets = { Vector2.zero, new Vector2(0.25f, 0.25f), new Vector2(-0.25f, -0.25f),
                new Vector2(0.25f, -0.25f), new Vector2(-0.25f, 0.25f), new Vector2(0.3f, 0f), new Vector2(-0.3f, 0f),
                new Vector2(0f, 0.3f), new Vector2(0f, -0.3f) };
            foreach (var o in offsets)
            {
                Vector3 probe = new Vector3(b.center.x + o.x * b.size.x, b.max.y + 3f, b.center.z + o.y * b.size.z);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider != floor) continue;
                Vector3 body = hit.point + Vector3.up * 1f;
                bool blocked = false;
                foreach (var c in Physics.OverlapSphere(body, Clearance, ~0, QueryTriggerInteraction.Ignore))
                    if (c != floor) { blocked = true; break; }
                if (blocked) continue;
                if (Flat(hit.point - start).magnitude < KeepAway) continue;
                bool nearCoin = false;
                foreach (var coin in coins)
                    if (coin != null && Flat(hit.point - coin.transform.position).magnitude < KeepAway) { nearCoin = true; break; }
                if (nearCoin) continue;
                spot = hit.point + Vector3.up * 0.1f;
                return true;
            }
            spot = Vector3.zero;
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // Ahead first, then the diagonals and sides, behind last; optionally keeping clear of another pickup
        private static bool TryStartRoomSpot(SpiritController spirit, Vector3 start, Vector3 forward, Vector3? avoid,
            out Vector3 spot)
        {
            float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
            foreach (float angle in angles)
            {
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                if (!TryFloorSpot(spirit, start, dir, Distance, out spot)) continue;
                if (avoid.HasValue && Vector3.Distance(spot, avoid.Value) < MinGapBetweenPickups) continue;
                return true;
            }
            spot = Vector3.zero;
            return false;
        }

        // A spot on the floor, in plain sight of the Spirit (no wall between) and clear of walls and props
        public static bool TryFloorSpot(SpiritController spirit, Vector3 start, Vector3 dir, float distance, out Vector3 spot)
            => TryFloorSpotFrom(start, dir, distance, spirit.transform, out spot);

        // Same check from any point at body height; colliders under `ignore` do not count as blocking
        private static bool TryFloorSpotFrom(Vector3 start, Vector3 dir, float distance, Transform ignore, out Vector3 spot)
        {
            spot = Vector3.zero;
            Vector3 eye = start + Vector3.up * 0.5f;
            if (Physics.Raycast(eye, dir, out RaycastHit block, distance + Clearance, ~0, QueryTriggerInteraction.Ignore)
                && (ignore == null || !block.collider.transform.IsChildOf(ignore)))
                return false;

            Vector3 above = start + dir * distance + Vector3.up * 2f;
            if (!Physics.Raycast(above, Vector3.down, out RaycastHit floor, 6f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (floor.normal.y < 0.7f || floor.point.y > start.y) return false; // not floor, or on top of a prop

            Vector3 body = floor.point + Vector3.up * 1f;
            foreach (var hit in Physics.OverlapSphere(body, Clearance, ~0, QueryTriggerInteraction.Ignore))
                if (hit != floor.collider && (ignore == null || !hit.transform.IsChildOf(ignore))) return false;

            spot = new Vector3(floor.point.x, floor.point.y + 0.1f, floor.point.z);
            return true;
        }
    }

    [RequireComponent(typeof(SphereCollider))]
    public sealed class PossessionChargePickup : MonoBehaviour
    {
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 23f;
        [SerializeField] private Renderer shard;
        [SerializeField] private bool soulWispLook = true; // shows a glowing ghost instead of the level's shard model
        [SerializeField, Min(0.1f)] private float wispSize = 2.2f;   // large for now so it is easy to spot while testing
        [SerializeField, Min(0f)] private float wispHeight = 1.6f;
        private float readyAt;
        private bool consumed;
        private Transform wisp;
        private Renderer[] wispParts;
        private Material wispGlow;
        private Camera cam;
        [SerializeField] private bool respawnInRoom = true; // re-form at a new random spot in the same room
        private Collider homeFloor;
        private Vector3 homePosition;
        private static readonly Color WispGlow = new Color(0.55f, 0.78f, 1f);
        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool Reforms => reforms;
        public bool IsConsumed => consumed;
        private void Awake() { GetComponent<SphereCollider>().isTrigger = true; }
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
        public bool TryCollect(PossessionCharge charge)
        {
            if (!IsReady || charge == null) return false;
            Vector3 offset = charge.transform.position - transform.position; offset.y = 0f;
            if (offset.sqrMagnitude > 2.56f || !charge.TryCollect()) return false;
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

            var body = new Material(Shader.Find("Standard")) { color = Color.white };
            body.SetFloat("_Glossiness", 0.15f);
            body.EnableKeyword("_EMISSION");
            body.SetColor("_EmissionColor", WispGlow);
            wispGlow = body;
            var eyes = new Material(Shader.Find("Standard")) { color = new Color(0.06f, 0.06f, 0.08f) };
            var halo = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.7f, 0.88f, 1f, 0.3f) };

            wisp = new GameObject("Soul Wisp").transform;
            wisp.SetParent(transform, false);
            wisp.localPosition = Vector3.up * wispHeight;
            wisp.localScale = Vector3.one * wispSize;

            // Rounded head, droplet tail and a wavy hem, like the Spirit; eyes face the camera (-z)
            Part(PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.5f, 0.46f), body);
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
            wispGlow.SetColor("_EmissionColor", WispGlow * pulse);
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
