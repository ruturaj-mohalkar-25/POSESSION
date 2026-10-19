using System.Collections.Generic;
using UnityEngine;

namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04Brain : MonoBehaviour
    {
        public Rect territory;
        public LocalLevel04Navigation navigation;
        private CharacterController body;
        private SpiritController spirit;
        private PossessionController player;
        private List<Vector3> path = new List<Vector3>();
        private int waypoint;
        private float repath, waitUntil, damageAfter = 2f;
        private bool wasPossessed;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            spirit = FindFirstObjectByType<SpiritController>();
            player = FindFirstObjectByType<PossessionController>();
            damageAfter = Time.time + 2f;
        }

        public void Tick(Transform[] route, float speed, bool hostile)
        {
            if (wasPossessed)
            {
                wasPossessed = false;
                damageAfter = Time.time + 1.5f;
                repath = 0; path.Clear();
            }
            bool chase = spirit != null && spirit.CurrentHP > 0 && player.CurrentState == PlayerState.Spirit
                && territory.Contains(new Vector2(spirit.transform.position.x, spirit.transform.position.z))
                && Vector3.Distance(transform.position, spirit.transform.position) < (hostile ? 14f : 8f)
                && VisibleSpirit();
            Vector3 destination;
            if (chase)
            {
                destination = spirit.transform.position;
                Vector3 gap = destination - transform.position; gap.y = 0;
                if (gap.magnitude < 1.85f)
                {
                    if (Time.time >= damageAfter) { spirit.TakeDamage(1); damageAfter = Time.time + 1.3f; }
                    return;
                }
            }
            else
            {
                if (route == null || route.Length == 0 || Time.time < waitUntil) return;
                waypoint %= route.Length;
                destination = route[waypoint].position;
                Vector3 gap = destination - transform.position; gap.y = 0;
                if (gap.magnitude < .4f)
                {
                    waypoint = (waypoint + 1) % route.Length;
                    waitUntil = Time.time + .65f; repath = 0; return;
                }
            }
            repath -= Time.deltaTime;
            if (repath <= 0f)
            {
                path = navigation.FindPath(transform.position, destination);
                repath = .65f;
            }
            while (path.Count > 0)
            {
                Vector3 delta = path[0] - transform.position; delta.y = 0;
                if (delta.magnitude < .22f) { path.RemoveAt(0); continue; }
                transform.rotation = Quaternion.LookRotation(delta);
                body.Move(delta.normalized * Mathf.Min(speed * Time.deltaTime, delta.magnitude));
                break;
            }
        }

        public void Drive(float speed)
        {
            wasPossessed = true;
            Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
            body.Move(direction * speed * Time.deltaTime);
        }

        private bool VisibleSpirit()
        {
            return !Physics.Linecast(transform.position, spirit.transform.position, out var hit,
                ~0, QueryTriggerInteraction.Ignore) || hit.collider.GetComponent<SpiritController>() == spirit;
        }
    }
}
