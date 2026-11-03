using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class ProjectileTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
        }

        [UnityTest]
        public IEnumerator ProjectileHitsWallAndIsDestroyed()
        {
            var wall = CreateWall(new Vector3(0f, 1f, 3f));
            var proj = FireProjectile(Vector3.zero, Vector3.forward);
            yield return new WaitForSeconds(0.5f);
            Assert.That(proj == null, Is.True, "Projectile should be destroyed on wall hit");
            Assert.That(wall != null && wall.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator ProjectileDoesNotPassThroughThinWall()
        {
            CreateWall(new Vector3(0f, 1f, 2f), thickness: 0.1f);
            var proj = FireProjectile(Vector3.zero, Vector3.forward);
            yield return new WaitForSeconds(0.5f);
            Assert.That(proj == null, Is.True,
                "Projectile should not clip through a thin wall");
        }

        [UnityTest]
        public IEnumerator ProjectilePointBlankStillCollides()
        {
            CreateWall(new Vector3(0f, 1f, 0.3f), thickness: 0.2f);
            var proj = FireProjectile(Vector3.zero, Vector3.forward);
            yield return new WaitForSeconds(0.3f);
            Assert.That(proj == null, Is.True,
                "Point-blank shot should still register a collision");
        }

        [UnityTest]
        public IEnumerator ProjectileTravelsInOpenSpaceThenDespawns()
        {
            var proj = FireProjectile(Vector3.zero, Vector3.forward, lifetime: 0.3f);
            yield return new WaitForSeconds(0.15f);
            Assert.That(proj != null, Is.True, "Projectile should still exist mid-flight");
            float z = proj.transform.position.z;
            Assert.That(z, Is.GreaterThan(1f), "Projectile should have moved forward");
            yield return new WaitForSeconds(0.3f);
            Assert.That(proj == null, Is.True, "Projectile should despawn after lifetime");
        }

        [UnityTest]
        public IEnumerator ProjectileDestroysTargetOnHit()
        {
            var targetGo = Create("Target", new Vector3(0f, 1f, 3f));
            targetGo.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.5f);
            targetGo.AddComponent<Target>();
            Physics.SyncTransforms();
            var proj = FireProjectile(Vector3.zero, Vector3.forward);
            yield return new WaitForSeconds(0.5f);
            Assert.That(proj == null, Is.True, "Projectile should be destroyed after hitting target");
        }

        [UnityTest]
        public IEnumerator ProjectileIgnoresOwnerCollider()
        {
            var owner = Create("Owner", new Vector3(0f, 1f, 0f));
            var ownerCol = owner.AddComponent<BoxCollider>();
            ownerCol.size = new Vector3(1f, 2f, 1f);
            Physics.SyncTransforms();

            var projGo = Create("Projectile", new Vector3(0f, 1f, 0f));
            projGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var proj = projGo.AddComponent<Projectile>();
            SetField(proj, "speed", 15f);
            SetField(proj, "lifetime", 1f);
            proj.SetOwner(ownerCol);

            yield return new WaitForSeconds(0.3f);
            Assert.That(projGo != null, Is.True,
                "Projectile should pass through its owner's collider");
            Assert.That(projGo.transform.position.z, Is.GreaterThan(2f));
        }

        [UnityTest]
        public IEnumerator ProjectileIgnoresTriggerVolumes()
        {
            var trigger = Create("TriggerZone", new Vector3(0f, 1f, 2f));
            var col = trigger.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 4f, 1f);
            Physics.SyncTransforms();

            var proj = FireProjectile(Vector3.zero, Vector3.forward);
            yield return new WaitForSeconds(0.5f);
            Assert.That(proj != null, Is.True,
                "Projectile should not be destroyed by a bare trigger with no Target/IDamageable");
        }

        [UnityTest]
        public IEnumerator RaycastCatchesCollisionAtHighSpeed()
        {
            CreateWall(new Vector3(0f, 1f, 3f), thickness: 0.05f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            var projGo = Create("Projectile", new Vector3(0f, 1f, 0f));
            projGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var proj = projGo.AddComponent<Projectile>();
            SetField(proj, "speed", 100f);
            SetField(proj, "lifetime", 2f);

            yield return new WaitForSeconds(0.3f);
            Assert.That(projGo == null, Is.True,
                "High-speed projectile should still hit thin wall via raycast");
        }

        private GameObject FireProjectile(Vector3 origin, Vector3 direction, float lifetime = 3f)
        {
            var go = Create("Projectile", origin);
            go.transform.rotation = Quaternion.LookRotation(direction);
            var proj = go.AddComponent<Projectile>();
            SetField(proj, "speed", 15f);
            SetField(proj, "lifetime", lifetime);
            Physics.SyncTransforms();
            return go;
        }

        private GameObject CreateWall(Vector3 position, float thickness = 1f)
        {
            var wall = Create("Wall", position);
            var col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3(4f, 4f, thickness);
            Physics.SyncTransforms();
            return wall;
        }

        private GameObject Create(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            objects.Add(obj);
            return obj;
        }

        private static void SetField(object target, string name, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(name,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);
                if (info != null) { info.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }
    }
}
