using UnityEngine;

namespace Possession
{
    public class ShootAbility : Ability
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private Camera mainCam;

        protected override void Execute()
        {
            if (mainCam == null) mainCam = Camera.main;
            Plane groundPlane = new Plane(Vector3.up, 0f);
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 aimDir = (worldPoint - firePoint.position).normalized;
                aimDir.y = 0f;

                if (projectilePrefab != null)
                {
                    GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(aimDir));
                    var projectile = proj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        var col = GetComponent<Collider>();
                        if (col == null) col = GetComponentInParent<Collider>();
                        if (col != null) projectile.SetOwner(col);
                    }
                }
            }
        }
    }
}
