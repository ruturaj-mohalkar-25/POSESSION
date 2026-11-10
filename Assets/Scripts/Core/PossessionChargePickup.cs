using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PossessionChargePickup : MonoBehaviour
    {
        [SerializeField] private bool reforms = true;
        [SerializeField, Min(0f)] private float cooldown = 23f;
        [SerializeField] private Renderer shard;
        private float readyAt;
        private bool consumed;
        public bool IsReady => !consumed && Time.time >= readyAt;
        public bool Reforms => reforms;
        public Renderer Visual => shard;
        private void Awake() { GetComponent<SphereCollider>().isTrigger = true; ObjectiveVisibility.Ensure(this); }
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
