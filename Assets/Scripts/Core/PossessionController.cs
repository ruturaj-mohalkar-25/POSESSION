using UnityEngine;

namespace Possession
{
    public enum PlayerState
    {
        Spirit,
        Possessed_Guard,
        Possessed_Heavy,
        Possessed_Drone
    }

    public class PossessionController : MonoBehaviour
    {
        [Header("Possession Settings")]
        [SerializeField] private float possessRange = 5f;
        [SerializeField] private float possessDuration = 15f;
        [SerializeField] private float ejectCooldown = 0.5f;
        [SerializeField] private float hostileTime = 5f;
        [SerializeField] private LayerMask enemyLayer;

        private PlayerState currentState = PlayerState.Spirit;
        private EnemyBase possessedEnemy;
        private float possessTimer;
        private float ejectCooldownTimer;
        private SpiritController spirit;

        public PlayerState CurrentState => currentState;
        public float PossessTimer => possessTimer;
        public float PossessDuration => possessDuration;
        public EnemyBase PossessedEnemy => possessedEnemy;

        private void Awake()
        {
            spirit = GetComponent<SpiritController>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress)
                return;

            ejectCooldownTimer -= Time.deltaTime;

            if (currentState == PlayerState.Spirit)
            {
                HandlePossessInput();
            }
            else
            {
                possessTimer -= Time.deltaTime;
                EventBus.PossessTimerUpdated?.Invoke(possessTimer, possessDuration);

                if (possessTimer <= 0f)
                    Eject(forced: true);
                else if (Input.GetKeyDown(KeyCode.E))
                    Eject(forced: false);
            }
        }

        private void HandlePossessInput()
        {
            EnemyBase nearest = FindNearestEnemy();
            bool canPossess = nearest != null && ejectCooldownTimer <= 0f;

            EventBus.PossessPromptUpdated?.Invoke(canPossess);

            if (!Input.GetKeyDown(KeyCode.E) || !canPossess) return;
            Possess(nearest);
        }

        private EnemyBase FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, possessRange, enemyLayer);
            EnemyBase nearest = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.CanBePossessed) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void Possess(EnemyBase enemy)
        {
            possessedEnemy = enemy;
            possessTimer = possessDuration;

            currentState = enemy switch
            {
                GuardEnemy => PlayerState.Possessed_Guard,
                HeavyEnemy => PlayerState.Possessed_Heavy,
                _ => PlayerState.Possessed_Guard
            };

            enemy.OnPossessed();
            spirit.SetVisible(false);
            EventBus.Possessed?.Invoke(enemy, currentState);
        }

        private void Eject(bool forced)
        {
            if (possessedEnemy == null) return;

            Vector3 ejectPos = possessedEnemy.transform.position;

            if (forced)
                possessedEnemy.OnForcedEject(hostileTime);
            else
                possessedEnemy.OnEjected();

            transform.position = ejectPos;
            spirit.SetVisible(true);
            ejectCooldownTimer = ejectCooldown;

            var ejectedEnemy = possessedEnemy;
            possessedEnemy = null;
            currentState = PlayerState.Spirit;

            EventBus.Ejected?.Invoke(ejectedEnemy, forced);
        }
    }
}
