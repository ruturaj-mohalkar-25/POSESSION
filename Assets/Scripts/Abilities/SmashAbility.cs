using UnityEngine;

namespace Possession
{
    public class SmashAbility : Ability
    {
        [SerializeField] private float smashRange = 2f;
        [SerializeField] private int smashDamage = 2;
        [SerializeField] private LayerMask smashableLayer;

        public float Range => smashRange;

        protected override void Execute()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 forward = transform.forward;

            if (Physics.Raycast(origin, forward, out RaycastHit hit, smashRange, smashableLayer))
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(smashDamage);
            }
        }
    }
}
