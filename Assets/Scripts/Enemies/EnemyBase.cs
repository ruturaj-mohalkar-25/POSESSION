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
        [SerializeField, Min(0f)] protected float detectionRange = 5f;
        [SerializeField] protected LayerMask spiritLayer = ~0;
        [FormerlySerializedAs("waypointIdleTime")]
        [SerializeField, Min(0f)] protected float waypointWaitTime = 0.75f;
        [SerializeField, Min(0.01f)] protected float waypointArrivalDistance = 0.3f;
        [SerializeField, Min(0f)] protected float chaseStopDistance = 1.5f;
        [SerializeField, Min(0f)] protected float patrolStartDelay;
        // After losing sight of the Spirit, walk to where it was last seen and look
        // around for this long before going back to the nearest patrol waypoint.
        [SerializeField, Min(0f)] protected float loseSightSearchTime = 1.5f;

        [Header("Vision Cone")]
        // Full angle of the cone in front of the enemy. detectionRange is its length.
        // The enemy only spots the Spirit inside the cone with a clear line of sight,
        // and can only be possessed when it cannot see the Spirit (e.g. from behind).
        [SerializeField, Range(1f, 360f)] protected float visionAngle = 45f;

        [Header("Room containment (optional)")]
        [SerializeField] private bool useHomeBounds;
        [SerializeField] private Bounds homeBounds;

        public bool HasHomeBounds => useHomeBounds;
        public Bounds HomeBounds => homeBounds;
        public void ConfigureHomeBounds(Bounds bounds)
        {
            homeBounds = bounds;
            useHomeBounds = true;
        }
        public bool IsInsideHome(Vector3 position) => !useHomeBounds
            || position.x >= homeBounds.min.x && position.x <= homeBounds.max.x
            && position.z >= homeBounds.min.z && position.z <= homeBounds.max.z;
        public bool CanPursue(Transform target) => target != null
            && (!useHomeBounds || IsInsideHome(transform.position) && IsInsideHome(target.position));

        protected int currentHP;
        protected bool isPossessed;
        protected bool isHostile;
        protected float hostileTimer;

        private CharacterController controller;
        private int currentWaypointIndex;
        private float waypointWaitTimer;
        private float verticalSpeed;
        private SpiritController chaseTarget;
        private Vector3 lastSeenPosition;
        private float searchTimer;

        public bool CanBePossessed => !isPossessed && !isHostile && currentHP > 0;
        public int CurrentHP => currentHP;
        public bool IsChasing => !isPossessed && chaseTarget != null
            && chaseTarget.isActiveAndEnabled && CanPursue(chaseTarget.transform);
        public bool IsIdle => !isPossessed && !IsChasing && !IsSearching && waypointWaitTimer > 0f;
        // True while the enemy checks the Spirit's last seen position after losing sight of it.
        public bool IsSearching => !isPossessed && !isHostile && searchTimer > 0f;
        // True while the enemy is in its post-eject hostile state (chasing or not).
        public bool IsHostile => isHostile;
        public float DetectionRange => detectionRange;
        public float VisionAngle => visionAngle;
        public bool IsPossessed => isPossessed;
        public float PatrolStartDelay => patrolStartDelay;

        // Lets a readability/feel cue retune this enemy's speed at runtime without
        // touching the shared prefab (e.g. widening the Heavy's speed gap).
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

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

            if (!isPossessed && chaseTarget != null && !CanPursue(chaseTarget.transform))
            {
                chaseTarget = null;
                searchTimer = 0f;
                ResumePatrolAtNearestWaypoint();
            }

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
            SpiritController seen = DetectSpirit();
            if (seen != null)
            {
                chaseTarget = seen;
                searchTimer = 0f;
                lastSeenPosition = seen.transform.position;
                MoveTowards(lastSeenPosition, chaseStopDistance);
                return;
            }

            if (chaseTarget != null)
            {
                // Lost sight this frame: search the last seen position first.
                chaseTarget = null;
                searchTimer = loseSightSearchTime;
                if (searchTimer <= 0f)
                    ResumePatrolAtNearestWaypoint();
            }

            if (searchTimer > 0f)
            {
                searchTimer -= Time.deltaTime;
                MoveTowards(lastSeenPosition, waypointArrivalDistance);
                if (searchTimer <= 0f)
                    ResumePatrolAtNearestWaypoint();
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
            searchTimer = 0f;
            waypointWaitTimer = 0f;
            var ability = GetComponent<Ability>();
            if (ability != null) ability.ResetUses();
        }

        public virtual void OnEjected()
        {
            isPossessed = false;
            chaseTarget = null;
            searchTimer = 0f;
        }

        public virtual void OnForcedEject(float hostileDuration)
        {
            isPossessed = false;
            isHostile = true;
            hostileTimer = hostileDuration;
            searchTimer = 0f;
            // PossessionController calls this before the spirit becomes visible again.
            chaseTarget = FindFirstObjectByType<SpiritController>(FindObjectsInactive.Include);
        }

        // True when the target is inside the vision cone and nothing solid blocks the view.
        public bool CanSee(Transform target)
        {
            if (target == null || !CanPursue(target)) return false;
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > detectionRange) return false;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (distance > 0.01f && forward.sqrMagnitude > 0.0001f
                && Vector3.Angle(forward, toTarget) > visionAngle * 0.5f)
                return false;

            if (!Physics.Linecast(transform.position, target.position, out RaycastHit hit,
                    ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.transform.IsChildOf(target)
                || hit.collider.transform.IsChildOf(transform);
        }

        // Possession only works from outside the vision cone, for example from behind.
        public bool CanBePossessedFrom(Transform spirit)
        {
            return CanBePossessed && !CanSee(spirit);
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
                if (!CanSee(spirit.transform))
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
            // Only autonomous movement is contained. A possessed body may cross a doorway.
            if (!isPossessed && useHomeBounds && IsInsideHome(transform.position))
            {
                float inset = controller.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) + .05f;
                Vector3 next = transform.position + displacement;
                next.x = Mathf.Clamp(next.x, homeBounds.min.x + inset, homeBounds.max.x - inset);
                next.z = Mathf.Clamp(next.z, homeBounds.min.z + inset, homeBounds.max.z - inset);
                displacement = next - transform.position;
            }
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
            Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + left * detectionRange);
            Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);
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
