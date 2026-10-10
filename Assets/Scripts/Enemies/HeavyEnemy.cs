using UnityEngine;

namespace Possession
{
    public class HeavyEnemy : EnemyBase
    {
        private int currentWaypointIndex;

        protected override void Awake()
        {
            base.Awake();
            moveSpeed = 1.8f;
            maxHP = 5;
            currentHP = maxHP;
        }

        protected override void HandlePatrol()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Transform target = waypoints[currentWaypointIndex];
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;

            if (dir.magnitude < 0.3f)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }

        protected override void HandlePossessedMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;

            if (dir.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(dir);

            // Heavy is slower when possessed too
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }

        protected override void HandleHostileBehavior()
        {
            // TODO: charge toward spirit
        }
    }
}
