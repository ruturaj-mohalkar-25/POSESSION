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
        [SerializeField] private float ejectCooldown = 2f;
        [SerializeField] private float hostileTime = 5f;
        [SerializeField] private LayerMask enemyLayer;

        private PlayerState currentState = PlayerState.Spirit;
        private EnemyBase possessedEnemy;
        private float possessTimer;
        private float ejectCooldownTimer;
        private SpiritController spirit;
        private Vector3 lastSpiritPosition;
        private EjectShieldVisual shieldVisual;
        private Bounds lastHostBounds;
        private Vector3 lastHostForward = Vector3.forward;

        public PlayerState CurrentState => currentState;
        public float PossessTimer => possessTimer;
        public float PossessDuration => possessDuration;
        public float EjectProtectionSeconds => 2f;
        public EnemyBase PossessedEnemy => possessedEnemy;
        public float PossessRange => possessRange;
        public bool CanStartPossession => currentState == PlayerState.Spirit && ejectCooldownTimer <= 0f
            && spirit != null && spirit.CurrentHP > 0
            && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress)
            && (GetComponent<PossessionCharge>() == null || GetComponent<PossessionCharge>().HasCharge);

        public bool IsEjectShielded => currentState == PlayerState.Spirit && spirit != null && spirit.IsDamageProtected;

        private void Awake()
        {
            spirit = GetComponent<SpiritController>();
            shieldVisual = GetComponent<EjectShieldVisual>();
            if (shieldVisual == null) shieldVisual = gameObject.AddComponent<EjectShieldVisual>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress)
                return;

            ejectCooldownTimer -= Time.deltaTime;
            UpdateShield();

            if (currentState == PlayerState.Spirit)
            {
                HandlePossessInput();
            }
            else
            {
                if (possessedEnemy == null || possessedEnemy.CurrentHP <= 0 || !possessedEnemy.gameObject.activeInHierarchy)
                {
                    Eject(forced: false);
                    return;
                }
                CacheHostPose();
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
            bool canPossess = nearest != null && ejectCooldownTimer <= 0f && CanStartPossession;

            EventBus.PossessPromptUpdated?.Invoke(canPossess);

            if (!Input.GetKeyDown(KeyCode.E) || !canPossess) return;
            TryPossess(nearest);
        }

        public EnemyBase FindPossessableEnemy() => FindNearestEnemy();

        public bool TryPossess(EnemyBase enemy)
        {
            if (!CanStartPossession || Time.timeScale <= 0f || enemy == null
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress
                || !enemy.gameObject.activeInHierarchy || !enemy.CanBePossessedFrom(transform)
                || (enemyLayer.value & (1 << enemy.gameObject.layer)) == 0
                || Vector3.Distance(transform.position, enemy.transform.position) > possessRange
                || !HasLineOfSight(enemy)) return false;
            Possess(enemy);
            return possessedEnemy == enemy;
        }

        private EnemyBase FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, possessRange, enemyLayer);
            EnemyBase nearest = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.CanBePossessedFrom(transform)) continue;
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
            if (GetComponent<PossessionCharge>() != null && (!CanStartPossession
                || currentState != PlayerState.Spirit || enemy == null || !enemy.CanBePossessedFrom(transform)
                || !enemy.gameObject.activeInHierarchy || ejectCooldownTimer > 0f
                || (enemyLayer.value & (1 << enemy.gameObject.layer)) == 0
                || Vector3.Distance(transform.position, enemy.transform.position) > possessRange
                || !HasLineOfSight(enemy))) return;
            lastSpiritPosition = transform.position;
            possessedEnemy = enemy;
            CacheHostPose();
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
            if (currentState == PlayerState.Spirit) return;
            CacheHostPose();

            if (!TryFindEjectionPosition(out Vector3 ejectPos))
                return;

            spirit.GrantDamageProtection(EjectProtectionSeconds);

            if (possessedEnemy != null)
            {
                if (forced && possessedEnemy.CurrentHP > 0 && possessedEnemy.gameObject.activeInHierarchy)
                    possessedEnemy.OnForcedEject(hostileTime);
                else possessedEnemy.OnEjected();
            }
            spirit.RestoreAt(ejectPos);
            Vector3 awayFromSpirit = lastHostBounds.center - ejectPos;
            awayFromSpirit.y = 0f;
            if (possessedEnemy != null && awayFromSpirit.sqrMagnitude > .01f)
                possessedEnemy.transform.rotation = Quaternion.LookRotation(awayFromSpirit.normalized);
            ejectCooldownTimer = ejectCooldown;

            var ejectedEnemy = possessedEnemy;
            possessedEnemy = null;
            currentState = PlayerState.Spirit;

            UpdateShield();
            EventBus.Ejected?.Invoke(ejectedEnemy, forced);
        }

        public bool TryEject(bool forced)
        {
            if (Time.timeScale <= 0f || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            Eject(forced);
            return currentState == PlayerState.Spirit;
        }

        private bool TryFindEjectionPosition(out Vector3 position)
        {
            const float clearance = 0.1f;
            var capsule = GetComponent<CapsuleCollider>();
            position = lastSpiritPosition;
            if (capsule == null) return false;

            Physics.SyncTransforms();
            Bounds bounds = lastHostBounds;
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, capsule.height * Mathf.Abs(scale.y) * 0.5f);
            Vector3 centerOffset = transform.TransformVector(capsule.center);

            // Prefer behind, then rear diagonals, sides, and finally the front.
            // Try both clearances for each direction without crossing solid geometry.
            float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
            foreach (float angle in angles)
            {
                for (int ring = 0; ring < 2; ring++)
                {
                    Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up)
                        * -lastHostForward;
                    direction.y = 0f;
                    direction.Normalize();
                    float bodyExtent = Mathf.Abs(direction.x) * bounds.extents.x
                        + Mathf.Abs(direction.z) * bounds.extents.z;
                    Vector3 candidate = bounds.center + direction
                        * (bodyExtent + radius + clearance + ring * 0.5f);
                    candidate -= new Vector3(centerOffset.x, 0f, centerOffset.z);
                    if (!TryGroundedPosition(candidate, bounds, centerOffset, radius,
                        halfHeight, clearance, out position)) continue;
                    Vector3 path = position + centerOffset - bounds.center;
                    bool blocked = false;
                    foreach (var hit in Physics.SphereCastAll(bounds.center, radius + clearance * .5f,
                        path.normalized, path.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.transform.IsChildOf(transform)
                            || (possessedEnemy != null && hit.collider.transform.IsChildOf(possessedEnemy.transform))) continue;
                        blocked = true;
                        break;
                    }
                    if (!blocked) return true;
                }
            }

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
                    || (possessedEnemy != null && hit.collider.transform.IsChildOf(possessedEnemy.transform))
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

        private void CacheHostPose()
        {
            if (possessedEnemy == null) return;
            var collider = possessedEnemy.GetComponent<Collider>();
            if (collider != null && collider.bounds.size.sqrMagnitude > .01f) lastHostBounds = collider.bounds;
            else lastHostBounds.center = possessedEnemy.transform.position;
            lastHostForward = possessedEnemy.transform.forward;
        }

        private void UpdateShield()
        {
            if (shieldVisual != null) shieldVisual.Refresh();
        }
    }
}
