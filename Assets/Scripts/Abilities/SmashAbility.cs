using UnityEngine;

namespace Possession
{
    public class SmashAbility : Ability
    {
        [SerializeField] private float smashRange = 2f;
        [SerializeField] private float smashRadius = 0.8f;
        [SerializeField] private int smashDamage = 2;
        [SerializeField] private LayerMask smashableLayer;

        public float Range => smashRange;

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
