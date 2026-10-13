using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(CharacterController))]
    public class HeavyEnemy : EnemyBase
    {
        [Header("Heavy AI")]
        [SerializeField, Min(0f)] private float detectionRange = 10f;
        [SerializeField] private LayerMask spiritLayer = ~0;
        [SerializeField, Min(0f)] private float waypointWaitTime = 0.75f;
        [SerializeField, Min(0.01f)] private float waypointArrivalDistance = 0.3f;
        [SerializeField, Min(0f)] private float chaseStopDistance = 1.5f;

        private CharacterController controller;
        private int currentWaypointIndex;
        private float waypointWaitTimer;
        private float verticalSpeed;
        private SpiritController chaseTarget;

        public bool IsChasing => !isPossessed && chaseTarget != null
            && chaseTarget.isActiveAndEnabled;

        private void Reset()
        {
            moveSpeed = 1.8f;
            maxHP = 5;
        }

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();

            // Older scenes used a primitive capsule. The controller replaces that collider.
            var legacyCollider = GetComponent<CapsuleCollider>();
            if (legacyCollider != null)
                legacyCollider.enabled = false;
        }

        protected override void Update()
        {
            if (Time.timeScale <= 0f || (GameManager.Instance != null
                && GameManager.Instance.CurrentState != LevelState.InProgress))
                return;

            base.Update();

            if (controller.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
        }

        protected override void HandlePatrol()
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

            if (waypoints == null || waypoints.Length == 0) return;
            currentWaypointIndex %= waypoints.Length;

            // A prefab has no scene references until the level designer assigns a route.
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[currentWaypointIndex] != null) break;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }

            Transform target = waypoints[currentWaypointIndex];
            if (target == null) return;

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

        protected override void HandlePossessedMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;

            Move(dir * moveSpeed * Time.deltaTime);
            HandlePossessedAbility();
        }

        protected override void HandleHostileBehavior()
        {
            // EnemyBase ends hostility before calling this hook on the final frame.
            if (!isHostile)
            {
                HandlePatrol();
                return;
            }

            if (chaseTarget == null)
                chaseTarget = DetectSpirit();

            if (IsChasing)
                MoveTowards(chaseTarget.transform.position, chaseStopDistance);
        }

        public override void OnPossessed()
        {
            base.OnPossessed();
            chaseTarget = null;
            waypointWaitTimer = 0f;
        }

        public override void OnEjected()
        {
            base.OnEjected();
            chaseTarget = null;
        }

        public override void OnForcedEject(float hostileDuration)
        {
            base.OnForcedEject(hostileDuration);
            // P2 calls this before showing the spirit again, so include inactive objects.
            chaseTarget = FindFirstObjectByType<SpiritController>(FindObjectsInactive.Include);
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
