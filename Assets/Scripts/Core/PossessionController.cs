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
        [SerializeField] private float possessDuration = 10f;
        [SerializeField] private float ejectCooldown = 0.5f;
        [SerializeField] private float hostileTime = 5f;
        [SerializeField] private LayerMask enemyLayer;

        private PlayerState currentState = PlayerState.Spirit;
        private EnemyBase possessedEnemy;
        private float possessTimer;
        private float ejectCooldownTimer;
        private SpiritController spirit;
        private Vector3 lastSpiritPosition;

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
                if (!HasLineOfSight(enemy)) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        // Stops possessing an enemy through walls or a locked door
        private bool HasLineOfSight(EnemyBase enemy)
        {
            if (!Physics.Linecast(transform.position, enemy.transform.position, out RaycastHit hit,
                    ~0, QueryTriggerInteraction.Ignore))
                return true;

            return hit.collider.GetComponentInParent<EnemyBase>() == enemy
                || hit.collider.transform.IsChildOf(transform);
        }

        private void Possess(EnemyBase enemy)
        {
            lastSpiritPosition = transform.position;
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

            if (!TryFindEjectionPosition(out Vector3 ejectPos))
                return; // Stay possessed until there is space, rather than spawn in a wall.

            if (forced)
                possessedEnemy.OnForcedEject(hostileTime);
            else
                possessedEnemy.OnEjected();

            spirit.RestoreAt(ejectPos);
            ejectCooldownTimer = ejectCooldown;

            var ejectedEnemy = possessedEnemy;
            possessedEnemy = null;
            currentState = PlayerState.Spirit;

            EventBus.Ejected?.Invoke(ejectedEnemy, forced);
        }

        private bool TryFindEjectionPosition(out Vector3 position)
        {
            const float clearance = 0.1f;
            var capsule = GetComponent<CapsuleCollider>();
            position = lastSpiritPosition;
            if (capsule == null) return false;

            Physics.SyncTransforms();
            var enemyCollider = possessedEnemy.GetComponent<Collider>();
            Bounds bounds = enemyCollider != null ? enemyCollider.bounds
                : new Bounds(possessedEnemy.transform.position, Vector3.one * 2f);
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, capsule.height * Mathf.Abs(scale.y) * 0.5f);
            Vector3 centerOffset = transform.TransformVector(capsule.center);

            // Try all sides of the enemy, then a slightly wider ring for corners.
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 direction = Quaternion.AngleAxis(i * 45f, Vector3.up)
                        * possessedEnemy.transform.forward;
                    direction.y = 0f;
                    direction.Normalize();
                    float bodyExtent = Mathf.Abs(direction.x) * bounds.extents.x
                        + Mathf.Abs(direction.z) * bounds.extents.z;
                    Vector3 candidate = bounds.center + direction
                        * (bodyExtent + radius + clearance + ring * 0.5f);
                    candidate -= new Vector3(centerOffset.x, 0f, centerOffset.z);
                    if (TryGroundedPosition(candidate, bounds, centerOffset, radius,
                        halfHeight, clearance, out position))
                        return true;
                }
            }

            // If every side is blocked, return to the pre-possession spot if still clear.
            return TryGroundedPosition(lastSpiritPosition, bounds, centerOffset, radius,
                halfHeight, clearance, out position);
        }

        private bool TryGroundedPosition(Vector3 candidate, Bounds enemyBounds,
            Vector3 centerOffset, float radius, float halfHeight, float clearance,
            out Vector3 position)
        {
            position = candidate;
            float groundY = float.NegativeInfinity;
            Vector3 origin = new Vector3(candidate.x + centerOffset.x,
                enemyBounds.max.y + 0.5f, candidate.z + centerOffset.z);
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down,
                enemyBounds.size.y + 2f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)
                    || hit.collider.transform.IsChildOf(possessedEnemy.transform)
                    || hit.normal.y < 0.5f || hit.point.y > enemyBounds.min.y + 0.3f)
                    continue;
                groundY = Mathf.Max(groundY, hit.point.y);
            }
            if (float.IsNegativeInfinity(groundY)) return false;

            position.y = groundY + halfHeight - centerOffset.y + clearance;
            Vector3 center = position + centerOffset;
            Vector3 segment = Vector3.up * (halfHeight - radius);
            foreach (Collider hit in Physics.OverlapCapsule(center + segment, center - segment,
                radius + clearance * 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.transform.IsChildOf(transform)) return false;
            }
            return true;
        }
    }
}
