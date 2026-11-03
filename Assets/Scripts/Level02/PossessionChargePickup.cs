using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PossessionChargePickup : MonoBehaviour
    {
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 8f;
        [SerializeField] private Renderer shard;
        [SerializeField] private Material collectBurst; // optional flash when the Spirit picks it up
        private Renderer[] shardParts; // the shard and anything modelled under it (coin, core, halo)
        private float readyAt;
        private bool consumed;
        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool Reforms => reforms;
        private void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }
        private void Update()
        {
            if (shard == null) return;
            if (shardParts == null) shardParts = shard.GetComponentsInChildren<Renderer>(true);
            bool ready = IsReady;
            foreach (var part in shardParts) part.enabled = ready;
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
            if (collectBurst != null)
                SimpleFX.Burst(shard != null ? shard.transform.position : transform.position + Vector3.up, collectBurst, 0.4f, 1.6f, 0.35f);
            return true;
        }
    }
}
