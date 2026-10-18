using UnityEngine;

namespace Possession
{
    // Added to bullets fired by a hostile Guard so they damage the spirit on contact.
    public class EnemyShot : MonoBehaviour
    {
        private int damage = 1;
        private float maxDistance = 6f;
        private Vector3 start;

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

        private void OnTriggerEnter(Collider other)
        {
            var spirit = other.GetComponentInParent<SpiritController>();
            if (spirit == null) return;
            spirit.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
