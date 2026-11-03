using UnityEngine;
namespace Possession
{
    [RequireComponent(typeof(EnemyBase))]
    public sealed class Level03EnemyPressure : MonoBehaviour
    {
        private EnemyBase enemy;
        private Level03Director level;
        private float windup = -1f, nextHit;
        private void Awake() => enemy = GetComponent<EnemyBase>();
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (level == null) level = FindFirstObjectByType<Level03Director>();
            if (level == null) return;
            var spirit = level.Spirit;
            bool reach = enemy.CanBePossessed && enemy.IsChasing && level.Possession.CurrentState == PlayerState.Spirit
                && spirit.CurrentHP > 0 && Vector3.Distance(transform.position,spirit.transform.position)<1.9f
                && GameManager.Instance.CurrentState == LevelState.InProgress;
            if (reach && Physics.Linecast(transform.position,spirit.transform.position,out var hit,~0,QueryTriggerInteraction.Ignore))
                reach = hit.collider.GetComponentInParent<SpiritController>() == spirit || hit.collider.GetComponentInParent<EnemyBase>() == enemy;
            if (!reach || Time.time < nextHit) { windup = -1f; return; }
            if (windup < 0f) windup = Time.time + 2.5f;
            if (Time.time < windup) return;
            if (level.TryContactDamage()) nextHit = Time.time + 5f;
            windup = -1f;
        }
    }
}
