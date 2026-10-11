using UnityEngine;

namespace Possession
{
    // Lets a hostile enemy (forced eject) hit the spirit when it catches up.
    // Kept separate from EnemyBase so the Guard/Heavy scripts stay untouched.
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float hitRange = 2f;     // Guard/Heavy stop chasing at 1.5m
        [SerializeField] private float hitCooldown = 1f;
        [SerializeField] private float firstHitDelay = 0.75f; // grace period to run after ejecting

        private EnemyBase enemy;
        private SpiritController spirit;
        private Collider spiritCollider;
        private bool hostile;
        private float nextHitTime;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            EventBus.Ejected += OnEjected;
            EventBus.Possessed += OnPossessed;
        }

        private void OnDisable()
        {
            EventBus.Ejected -= OnEjected;
            EventBus.Possessed -= OnPossessed;
        }

        private void OnEjected(EnemyBase ejected, bool forced)
        {
            if (ejected != enemy || !forced) return;
            hostile = true;
            nextHitTime = Time.time + firstHitDelay;
        }

        private void OnPossessed(EnemyBase possessed, PlayerState state)
        {
            if (possessed == enemy) hostile = false;
        }

        private void Update()
        {
            if (!hostile) return;

            // EnemyBase makes the enemy possessable again once its hostile timer ends
            if (enemy.CanBePossessed || enemy.CurrentHP <= 0)
            {
                hostile = false;
                return;
            }

            if (spirit == null)
            {
                spirit = FindFirstObjectByType<SpiritController>();
                if (spirit == null) return;
                spiritCollider = spirit.GetComponent<Collider>();
            }

            // Spirit is hidden while possessing another enemy
            if (spiritCollider != null && !spiritCollider.enabled) return;
            if (Time.time < nextHitTime) return;

            Vector3 offset = spirit.transform.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude > hitRange) return;

            spirit.TakeDamage(damage);
            nextHitTime = Time.time + hitCooldown;
        }
    }
}
