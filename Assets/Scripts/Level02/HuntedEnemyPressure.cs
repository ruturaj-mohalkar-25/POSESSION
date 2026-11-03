using UnityEngine;

namespace Possession
{
    // Level 2 contact pressure has a windup and shares a cooldown across ordinary enemies.
    [RequireComponent(typeof(EnemyBase))]
    public sealed class HuntedEnemyPressure : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float windup = 1.2f;
        [SerializeField, Min(0.1f)] private float hitInterval = 3.2f;
        private EnemyBase enemy;
        private EnemyContactDamage sharedAttack;
        private Level02Director level;
        private float readyAt = -1f;
        private float nextHit;
        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            sharedAttack = GetComponent<EnemyContactDamage>();
        }
        private void OnDisable() => readyAt = -1f;
        private void Update()
        {
            if (level == null) level = FindFirstObjectByType<Level02Director>();
            if (level == null) return;
            var spirit = level.Spirit;
            bool canReach = enemy.CanBePossessed && !enemy.IsHostile && enemy.IsChasing
                && (sharedAttack == null || !sharedAttack.IsInEjectGrace)
                && spirit.isActiveAndEnabled && spirit.CurrentHP > 0
                && level.Possession.CurrentState == PlayerState.Spirit
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress)
                && Vector3.Distance(transform.position, spirit.transform.position) <= 1.9f;
            if (canReach && Physics.Linecast(transform.position, spirit.transform.position, out var hit, ~0, QueryTriggerInteraction.Ignore))
                canReach = hit.collider.GetComponentInParent<SpiritController>() == spirit || hit.collider.GetComponentInParent<EnemyBase>() == enemy;
            if (!canReach || Time.time < nextHit) { readyAt = -1f; return; }
            if (readyAt < 0f) readyAt = Time.time + windup;
            if (Time.time < readyAt) return;
            if (level.TryContactDamage()) nextHit = Time.time + hitInterval;
            // A blocked hit must wind up again, rather than landing immediately when grace expires.
            readyAt = -1f;
        }
    }
}
