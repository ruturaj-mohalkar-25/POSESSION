using UnityEngine;

namespace Possession
{
    public class SmashAbility : Ability
    {
        [SerializeField] private float smashRange = 2f;
        [SerializeField] private float smashRadius = 0.8f;
        [SerializeField] private int smashDamage = 2;
        [SerializeField] private LayerMask smashableLayer;

        // The cast ends smashRange ahead of the body; its sphere extends farther.
        public float Range => smashRange + smashRadius;

        private void Reset()
        {
            maxUses = 1;
        }

        protected override void Execute()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f - forward * smashRadius;

            if (Physics.SphereCast(origin, smashRadius, forward, out RaycastHit hit, smashRange + smashRadius, smashableLayer))
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(smashDamage);
            }
        }
    }
}
