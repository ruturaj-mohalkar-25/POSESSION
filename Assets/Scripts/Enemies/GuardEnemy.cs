using UnityEngine;

namespace Possession
{
    public class GuardEnemy : EnemyBase
    {
        private int currentWaypointIndex;

        protected override void Awake()
        {
            base.Awake();
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

        protected override void HandleHostileBehavior()
        {
            var target = GetSpiritTransform();
            if (target == null) return;

            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.magnitude < 1.5f) return;

            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
