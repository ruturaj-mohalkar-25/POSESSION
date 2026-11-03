using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PossessionChargePickup : MonoBehaviour
    {
        private static readonly Color ShardColor = new Color(0.35f, 1f, 0.72f);
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 23f;
        [SerializeField] private Renderer shard;
        private float readyAt;
        private bool consumed;
        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool Reforms => reforms;

        public static PossessionChargePickup Create(Transform parent, string pickupName, Vector3 position)
        {
            var root = new GameObject(pickupName);
            root.transform.SetParent(parent);
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Energy shard";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
            visual.transform.localRotation = Quaternion.Euler(35f, 45f, 35f);
            var visualCollider = visual.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(visualCollider); else DestroyImmediate(visualCollider);
            var renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial.color = ShardColor;

            var pickup = root.AddComponent<PossessionChargePickup>();
            pickup.shard = renderer;
            return pickup;
        }
        private void Awake() { GetComponent<SphereCollider>().isTrigger = true; }
        private void Update()
        {
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
            return true;
        }
    }
}
