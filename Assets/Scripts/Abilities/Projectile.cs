using UnityEngine;

namespace Possession
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private int damage = 1;

        private float timer;

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            timer += Time.deltaTime;
            if (timer >= lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            var target = other.GetComponent<Target>();
            if (target != null)
            {
                target.OnHit();
                Destroy(gameObject);
                return;
            }

            // Hit a wall or floor
            if (!other.isTrigger)
                Destroy(gameObject);
        }
    }
}
