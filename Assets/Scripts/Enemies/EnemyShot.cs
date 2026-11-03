using UnityEngine;

namespace Possession
{
    // Added to bullets fired by a hostile Guard so they damage the spirit on contact.
    public class EnemyShot : MonoBehaviour
    {
        private int damage = 1;
        private float maxDistance = 6f;
        private Vector3 start;
        private bool consumed;

        public void Init(int damage, float maxDistance)
        {
            this.damage = damage;
            this.maxDistance = maxDistance;
            start = transform.position;
        }

        // Enemy bullets only travel as far as the Guard's shooting range
        private void Update()
        {
            if ((transform.position - start).sqrMagnitude > maxDistance * maxDistance)
                Destroy(gameObject);
        }

        // Both swept movement and physics triggers must use the same hit path.
        // Unity can deliver both callbacks before deferred destruction completes.
        public bool TryHit(Collider other)
        {
            if (consumed) return true;
            var spirit = other.GetComponentInParent<SpiritController>();
            if (spirit == null) return false;
            consumed = true;
            spirit.TakeDamage(damage);
            Destroy(gameObject);
            return true;
        }

        private void OnTriggerEnter(Collider other) => TryHit(other);
    }
}
