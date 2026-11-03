using UnityEngine;

namespace Possession
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private int damage = 1;

        private float timer;
        private Collider ownerCollider;

        private void Update()
        {
            float step = speed * Time.deltaTime;
            Vector3 forward = transform.forward;

            if (Physics.Raycast(transform.position, forward, out RaycastHit hit, step,
                    ~0, QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;
                HandleHit(hit.collider);
                return;
            }

            transform.position += forward * step;

            timer += Time.deltaTime;
            if (timer >= lifetime)
                Destroy(gameObject);
        }

        public void SetOwner(Collider col)
        {
            ownerCollider = col;
        }

        private void HandleHit(Collider other)
        {
            if (other == ownerCollider) return;
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

            if (!other.isTrigger)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other) => HandleHit(other);
    }
}
