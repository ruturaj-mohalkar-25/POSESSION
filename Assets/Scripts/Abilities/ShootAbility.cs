using UnityEngine;

namespace Possession
{
    public class ShootAbility : Ability
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        protected override void Execute()
        {
            // Aim: raycast from camera through mouse onto ground plane
            Plane groundPlane = new Plane(Vector3.up, 0f);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 aimDir = (worldPoint - firePoint.position).normalized;
                aimDir.y = 0f;

                if (projectilePrefab != null)
                {
                    GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(aimDir));
                    // Projectile script handles its own movement
                }
            }
        }
    }
}
