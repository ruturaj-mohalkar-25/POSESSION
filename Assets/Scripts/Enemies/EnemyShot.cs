using UnityEngine;

namespace Possession
{
    // Added to bullets fired by a hostile Guard so they damage the spirit on contact.
    public class EnemyShot : MonoBehaviour
    {
        private int damage = 1;
        private float maxDistance = 6f;
        private Vector3 start;
        private bool hitSpirit;

        public void Init(int damage, float maxDistance)
        {
            this.damage = damage;
            hitSpirit = false;
            this.maxDistance = maxDistance;
            start = transform.position;
        }

        // Enemy bullets only travel as far as the Guard's shooting range
        private void Update()
        {
            if ((transform.position - start).sqrMagnitude > maxDistance * maxDistance)
                Destroy(gameObject);
        }

        // Both the shared projectile raycast and the physics trigger use this path.
        // Guard against both arriving before Destroy is processed at the end of the frame.
        public bool TryHitSpirit(Collider other)
        {
            var spirit = other.GetComponentInParent<SpiritController>();
            if (spirit == null) return false;
            if (!hitSpirit)
            {
                hitSpirit = true;
                spirit.TakeDamage(damage);
            }
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryHitSpirit(other)) Destroy(gameObject);
        }
    }
}
