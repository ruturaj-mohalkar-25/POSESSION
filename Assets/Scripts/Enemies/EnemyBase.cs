using UnityEngine;
using UnityEngine.Serialization;

namespace Possession
{
    [RequireComponent(typeof(CharacterController))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Base")]
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected int maxHP = 3;
        [SerializeField] protected Transform[] waypoints;

        [Header("Shared AI")]
        [SerializeField, Min(0f)] protected float detectionRange = 10f;
        [SerializeField] protected LayerMask spiritLayer = ~0;
        [FormerlySerializedAs("waypointIdleTime")]
        [SerializeField, Min(0f)] protected float waypointWaitTime = 0.75f;
        [SerializeField, Min(0.01f)] protected float waypointArrivalDistance = 0.3f;
        [SerializeField, Min(0f)] protected float chaseStopDistance = 1.5f;
        [SerializeField, Min(0f)] protected float patrolStartDelay;

        protected int currentHP;
        protected bool isPossessed;
        protected bool isHostile;
        protected float hostileTimer;

        private CharacterController controller;
        private int currentWaypointIndex;
        private float waypointWaitTimer;
        private float verticalSpeed;
        private SpiritController chaseTarget;

        public bool CanBePossessed => !isPossessed && !isHostile && currentHP > 0;
        public int CurrentHP => currentHP;
        public bool IsChasing => !isPossessed && chaseTarget != null
            && chaseTarget.isActiveAndEnabled;
        public bool IsIdle => !isPossessed && !IsChasing && waypointWaitTimer > 0f;
        public float DetectionRange => detectionRange;
        public float PatrolStartDelay => patrolStartDelay;

        protected virtual void Awake()
        {
            currentHP = maxHP;
            controller = GetComponent<CharacterController>();
            waypointWaitTimer = patrolStartDelay;

            // CharacterController handles collision for both AI and possessed movement.
            var legacyCollider = GetComponent<CapsuleCollider>();
            if (legacyCollider != null)
                legacyCollider.enabled = false;
        }

        protected virtual void Update()
        {
            if (Time.timeScale <= 0f || (GameManager.Instance != null
                && GameManager.Instance.CurrentState != LevelState.InProgress))
                return;

            if (isPossessed)
            {
                HandlePossessedMovement();
            }
            else if (isHostile)
            {
                hostileTimer -= Time.deltaTime;
                if (hostileTimer <= 0f)
                {
                    isHostile = false;
                    chaseTarget = null;
                    ResumePatrolAtNearestWaypoint();
                }
                else
                {
                    HandleHostileBehavior();
                }
            }
            else
            {
                HandlePatrol();
            }

            ApplyGravity();
        }

        protected virtual void HandlePatrol()
        {
            chaseTarget = DetectSpirit();
            if (IsChasing)
            {
                MoveTowards(chaseTarget.transform.position, chaseStopDistance);
                return;
            }

            if (waypointWaitTimer > 0f)
            {
                waypointWaitTimer -= Time.deltaTime;
                return;
            }

            if (!TryGetCurrentWaypoint(out Transform target)) return;

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= waypointArrivalDistance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                waypointWaitTimer = waypointWaitTime;
                return;
            }

            MoveTowards(target.position, 0f);
        }

        protected virtual void HandlePossessedMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(h, 0f, v).normalized;

            Move(direction * moveSpeed * Time.deltaTime);
            HandlePossessedAbility();
        }

        protected void HandlePossessedAbility()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ability = GetComponent<Ability>();
                if (ability != null)
                    ability.TryExecute();
            }
        }

        protected virtual void HandleHostileBehavior()
        {
            if (chaseTarget == null)
                chaseTarget = DetectSpirit();

            if (IsChasing)
                MoveTowards(chaseTarget.transform.position, chaseStopDistance);
        }

        public virtual void OnPossessed()
        {
            isPossessed = true;
            chaseTarget = null;
            waypointWaitTimer = 0f;
            var ability = GetComponent<Ability>();
            if (ability != null) ability.ResetUses();
        }

        public virtual void OnEjected()
        {
            isPossessed = false;
            chaseTarget = null;
        }

        public virtual void OnForcedEject(float hostileDuration)
        {
            isPossessed = false;
            isHostile = true;
            hostileTimer = hostileDuration;
            // PossessionController calls this before the spirit becomes visible again.
            chaseTarget = FindFirstObjectByType<SpiritController>(FindObjectsInactive.Include);
        }

        public virtual void TakeDamage(int amount)
        {
            currentHP -= amount;
            if (currentHP <= 0)
                Die();
        }

        protected virtual void Die()
        {
            gameObject.SetActive(false);
        }

        private SpiritController DetectSpirit()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange,
                spiritLayer, QueryTriggerInteraction.Ignore);
            SpiritController nearest = null;
            float nearestDistance = float.PositiveInfinity;

            foreach (Collider hit in hits)
            {
                var spirit = hit.GetComponentInParent<SpiritController>();
                if (spirit == null || !spirit.isActiveAndEnabled || spirit.CurrentHP <= 0)
                    continue;

                float distance = (spirit.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = spirit;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private bool TryGetCurrentWaypoint(out Transform target)
        {
            target = null;
            if (waypoints == null || waypoints.Length == 0) return false;
            currentWaypointIndex %= waypoints.Length;

            for (int i = 0; i < waypoints.Length; i++)
            {
                target = waypoints[currentWaypointIndex];
                if (target != null) return true;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }

            target = null;
            return false;
        }

        private void ResumePatrolAtNearestWaypoint()
        {
            waypointWaitTimer = 0f;
            if (waypoints == null || waypoints.Length == 0) return;

            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Vector3 delta = waypoints[i].position - transform.position;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    currentWaypointIndex = i;
                }
            }
        }

        private void MoveTowards(Vector3 position, float stopDistance)
        {
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= stopDistance) return;

            Move(delta.normalized * Mathf.Min(moveSpeed * Time.deltaTime,
                distance - stopDistance));
        }

        private void Move(Vector3 displacement)
        {
            if (displacement.sqrMagnitude <= 0f) return;
            transform.rotation = Quaternion.LookRotation(displacement);
            controller.Move(displacement);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            if (waypoints == null) return;

            Gizmos.color = Color.cyan;
            foreach (Transform point in waypoints)
            {
                if (point != null)
                    Gizmos.DrawLine(transform.position, point.position);
            }
        }
    }
}
