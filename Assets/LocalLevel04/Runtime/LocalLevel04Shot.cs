using UnityEngine;

namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04Shot : ShootAbility
    {
        public GameObject projectile;
        protected override void Execute()
        {
            if (Camera.main == null || projectile == null) return;
            // Match the cursor to projectile height, so clicking a target aims at its centre.
            Vector3 origin = transform.position + Vector3.up * .35f;
            var plane = new Plane(Vector3.up, origin);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out float distance)) return;
            Vector3 direction = ray.GetPoint(distance) - origin;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) return;
            FireProjectile(projectile, origin, direction, GetComponent<Collider>());
        }

        public static GameObject FireProjectile(GameObject prefab, Vector3 origin, Vector3 direction, Collider owner)
        {
            var shot = Instantiate(prefab, origin, Quaternion.LookRotation(direction));
            // The shared muzzle burst briefly retains its primitive collider during
            // its spawn frame. Skip that effect for local shots to prevent self-hits.
            if (shot.TryGetComponent<ProjectileFX>(out var flash)) flash.enabled = false;
            shot.GetComponent<Projectile>().SetOwner(owner);
            return shot;
        }
    }
}
