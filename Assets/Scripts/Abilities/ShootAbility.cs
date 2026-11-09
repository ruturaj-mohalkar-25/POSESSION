using UnityEngine;

namespace Possession
{
    public class ShootAbility : Ability
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private void Reset()
        {
            maxUses = 3;
        }

        private Camera mainCam;

        protected override void Execute()
        {
            if (mainCam == null) mainCam = Camera.main;
            Plane groundPlane = new Plane(Vector3.up, 0f);
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 aimDir = worldPoint - firePoint.position;
                aimDir.y = 0f;
                // Clicking right under the shooter leaves no direction: fire the way it is facing
                if (aimDir.sqrMagnitude < 0.0001f) aimDir = firePoint.forward;
                aimDir.y = 0f;
                if (aimDir.sqrMagnitude < 0.0001f) aimDir = transform.forward;
                aimDir.Normalize();

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
