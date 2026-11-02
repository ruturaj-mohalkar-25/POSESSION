using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    // Person 1: enemies have a 45 degree vision cone. They only notice the Spirit inside
    // the cone with a clear line of sight, and can only be possessed from outside it.
    public class VisionConeTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GuardEnemy guard;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
            var guardObject = Create("Guard", new Vector3(0f, 1.05f, 0f));
            guardObject.SetActive(false);
            guard = guardObject.AddComponent<GuardEnemy>();
            guardObject.transform.rotation = Quaternion.LookRotation(Vector3.forward); // looks down +z
            guardObject.SetActive(true);
            guard.GetComponent<CharacterController>().minMoveDistance = 0f;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            EventBus.Clear();
        }

        [Test]
        public void VisionAngleDefaultIs45()
        {
            Assert.That(guard.VisionAngle, Is.EqualTo(45f).Within(0.01f));
        }

        [Test]
        public void SpiritInFrontIsSeenAndCannotBePossessed()
        {
            var spirit = CreateSpirit(new Vector3(0f, 1f, 4f)); // dead ahead
            Assert.That(guard.CanSee(spirit.transform), Is.True);
            Assert.That(guard.CanBePossessedFrom(spirit.transform), Is.False);
        }

        [Test]
        public void SpiritBehindIsNotSeenAndCanBePossessed()
        {
            var spirit = CreateSpirit(new Vector3(0f, 1f, -4f)); // behind
            Assert.That(guard.CanSee(spirit.transform), Is.False);
            Assert.That(guard.CanBePossessedFrom(spirit.transform), Is.True);
        }

        [Test]
        public void SpiritToTheSideIsOutsideTheCone()
        {
            var spirit = CreateSpirit(new Vector3(4f, 1f, 0f)); // 90 degrees off forward
            Assert.That(guard.CanSee(spirit.transform), Is.False);
            Assert.That(guard.CanBePossessedFrom(spirit.transform), Is.True);
        }

        [Test]
        public void SpiritInFrontButBeyondRangeIsNotSeen()
        {
            Set(guard, "detectionRange", 5f);
            var spirit = CreateSpirit(new Vector3(0f, 1f, 8f));
            Assert.That(guard.CanSee(spirit.transform), Is.False);
        }

        [Test]
        public void WallInFrontBlocksVision()
        {
            var wall = Create("Wall", new Vector3(0f, 1f, 2f));
            wall.AddComponent<BoxCollider>().size = new Vector3(4f, 3f, 0.5f);
            var spirit = CreateSpirit(new Vector3(0f, 1f, 5f));
            Assert.That(guard.CanSee(spirit.transform), Is.False);
        }

        [UnityTest]
        public IEnumerator EnemyChasesSpiritInTheConeButNotBehind()
        {
            var spirit = CreateSpirit(new Vector3(0f, 1f, 5f)); // in front
            yield return new WaitForSeconds(0.2f);
            Assert.That(guard.IsChasing, Is.True);

            spirit.transform.position = new Vector3(0f, 1f, -5f); // behind
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            Assert.That(guard.IsChasing, Is.False,
                "An enemy should not notice the Spirit behind it.");
        }

        [UnityTest]
        public IEnumerator PossessionWorksFromBehindButNotFromTheFront()
        {
            var spiritObject = Create("Spirit", new Vector3(0f, 1f, -3f));
            spiritObject.layer = 8;
            var rb = spiritObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            spiritObject.AddComponent<CapsuleCollider>();
            spiritObject.AddComponent<SpiritController>();
            var possession = spiritObject.AddComponent<PossessionController>();
            Set(possession, "enemyLayer", (LayerMask)(1 << guard.gameObject.layer));
            possession.enabled = false;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(Invoke<EnemyBase>(possession, "FindNearestEnemy"), Is.EqualTo(guard),
                "Possession should be allowed from behind.");

            spiritObject.transform.position = new Vector3(0f, 1f, 3f); // in front
            Physics.SyncTransforms();
            yield return null;
            Assert.That(Invoke<EnemyBase>(possession, "FindNearestEnemy"), Is.Null,
                "Possession should be blocked from the front, inside the cone.");
        }

        private GameObject Create(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            objects.Add(obj);
            return obj;
        }

        private SpiritController CreateSpirit(Vector3 position)
        {
            var obj = Create("Spirit", position);
            obj.layer = 8;
            var rb = obj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            var spirit = obj.AddComponent<SpiritController>();
            obj.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();
            return spirit;
        }

        private static void Set(object target, string field, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) { info.SetValue(target, value); return; }
            }
            throw new System.MissingFieldException(target.GetType().Name, field);
        }

        private static T Invoke<T>(object target, string method)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo info = type.GetMethod(method, BindingFlags.Instance
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) return (T)info.Invoke(target, null);
            }
            throw new System.MissingMethodException(target.GetType().Name, method);
        }
    }
}
