using UnityEngine;

namespace Possession
{
    // Lets a re-forming pickup come back at a new random spot in the same room (its home floor),
    // clear of walls, props and enemies and away from other pickups.
    public static class PickupRoomRespawn
    {
        const float Clearance = 0.6f;
        const float EdgeMargin = 0.15f;     // keep away from the floor's edges (walls stand there)
        const float MaxRoam = 6f;           // on one big shared floor, stay this close to the original spot
        const float GapToOtherPickups = 2f;
        const int Attempts = 12;

        // The floor collider directly under a pickup, used as its "room"
        public static Collider HomeFloor(Transform pickup)
        {
            Vector3 from = pickup.position + Vector3.up * 2f;
            foreach (var hit in Physics.RaycastAll(from, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.normal.y > 0.7f && !hit.collider.transform.IsChildOf(pickup)) return hit.collider;
            return null;
        }

        public static bool TryNewSpot(Collider floor, Vector3 home, Transform self, out Vector3 spot)
        {
            spot = Vector3.zero;
            if (floor == null) return false;
            Physics.SyncTransforms();

            Bounds b = floor.bounds;
            float minX = Mathf.Max(b.min.x + b.size.x * EdgeMargin, home.x - MaxRoam);
            float maxX = Mathf.Min(b.max.x - b.size.x * EdgeMargin, home.x + MaxRoam);
            float minZ = Mathf.Max(b.min.z + b.size.z * EdgeMargin, home.z - MaxRoam);
            float maxZ = Mathf.Min(b.max.z - b.size.z * EdgeMargin, home.z + MaxRoam);
            if (minX > maxX || minZ > maxZ) return false;

            for (int i = 0; i < Attempts; i++)
            {
                Vector3 probe = new Vector3(Random.Range(minX, maxX), b.max.y + 3f, Random.Range(minZ, maxZ));
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider != floor) continue; // something (prop, wall) is standing there
                Vector3 body = hit.point + Vector3.up * 1f;
                bool blocked = false;
                foreach (var c in Physics.OverlapSphere(body, Clearance, ~0, QueryTriggerInteraction.Ignore))
                    if (c != floor && !c.transform.IsChildOf(self)) { blocked = true; break; }
                if (blocked || NearOtherPickup(hit.point, self)) continue;
                spot = hit.point + Vector3.up * 0.1f;
                return true;
            }
            return false;
        }

        private static bool NearOtherPickup(Vector3 point, Transform self)
        {
            foreach (var p in Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                if (p.transform != self && Flat(p.transform.position - point) < GapToOtherPickups) return true;
            foreach (var p in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                if (p.transform != self && Flat(p.transform.position - point) < GapToOtherPickups) return true;
            return false;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
