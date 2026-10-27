using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class PossessRadiusTests
    {
        private readonly List<GameObject> objects = new();
        private PossessionController possession;
        private EnemyReadabilityController readability;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.Clear();

            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(200f, 1f, 200f);

            var spirit = Create("Spirit", new Vector3(0f, 1f, 0f));
            spirit.layer = 8;
            spirit.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            spirit.AddComponent<CapsuleCollider>();
            spirit.AddComponent<SpiritController>();
            possession = spirit.AddComponent<PossessionController>();
            Set(possession, "enemyLayer", (LayerMask)(1 << 6));
            possession.enabled = false;

            var controllerGo = Create("ReadabilityController", Vector3.zero);
            readability = controllerGo.AddComponent<EnemyReadabilityController>();

            Physics.SyncTransforms();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            EventBus.Clear();
        }

        [Test]
        public void PossessRangeDefaultIs7()
        {
            float range = Read<float>(possession, "possessRange");
            Assert.That(range, Is.EqualTo(7f));
        }

        [Test]
        public void ReadabilityRangeMatchesPossessRange()
        {
            float possessRange = Read<float>(possession, "possessRange");
            float readabilityRange = Read<float>(readability, "possessRange");
            Assert.That(readabilityRange, Is.EqualTo(possessRange),
                "EnemyReadabilityController range must match PossessionController");
        }

        [UnityTest]
        public IEnumerator EnemyAt6fIsDetectedAsPossessable()
        {
            var enemy = CreateEnemy(new Vector3(6f, 1f, 0f));
            Physics.SyncTransforms();
            yield return null;

            var nearest = CallFindNearest();
            Assert.That(nearest, Is.Not.Null,
                "Enemy at 6f should be within the 7f possess range");
        }

        [UnityTest]
        public IEnumerator EnemyAt8fIsNotDetected()
        {
            CreateEnemy(new Vector3(8f, 1f, 0f));
            Physics.SyncTransforms();
            yield return null;

            var nearest = CallFindNearest();
            Assert.That(nearest, Is.Null,
                "Enemy at 8f should be outside the 7f possess range");
        }

        [UnityTest]
        public IEnumerator NearestOfTwoEnemiesIsSelected()
        {
            var near = CreateEnemy(new Vector3(4f, 1f, 0f));
            CreateEnemy(new Vector3(6.5f, 1f, 0f));
            Physics.SyncTransforms();
            yield return null;

            var nearest = CallFindNearest();
            Assert.That(nearest, Is.EqualTo(near),
                "Closer enemy should be selected when both are in range");
        }

        [UnityTest]
        public IEnumerator EnemyBehindWallIsNotPossessable()
        {
            var wall = Create("Wall", new Vector3(3f, 1f, 0f));
            wall.AddComponent<BoxCollider>().size = new Vector3(0.5f, 4f, 10f);
            CreateEnemy(new Vector3(5f, 1f, 0f));
            Physics.SyncTransforms();
            yield return null;

            var nearest = CallFindNearest();
            Assert.That(nearest, Is.Null,
                "Enemy behind a wall should fail the line-of-sight check");
        }

        [UnityTest]
        public IEnumerator ReadabilityCueAppearsAt6f()
        {
            var enemy = CreateEnemy(new Vector3(6f, 1f, 0f));
            var cue = enemy.GetComponent<EnemyReadability>();
            Physics.SyncTransforms();
            yield return null;
            yield return null;

            var mode = Read<EnemyReadability.Mode>(cue, "mode");
            Assert.That(mode, Is.EqualTo(EnemyReadability.Mode.Possessable),
                "Green cue should show on enemy within 7f range");
        }

        [UnityTest]
        public IEnumerator ReadabilityCueAbsentAt8f()
        {
            var enemy = CreateEnemy(new Vector3(20f, 1f, 0f));
            var cue = enemy.GetComponent<EnemyReadability>();
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);

            var mode = Read<EnemyReadability.Mode>(cue, "mode");
            Assert.That(mode, Is.EqualTo(EnemyReadability.Mode.None),
                "No cue should show on enemy well beyond 7f range");
        }

        private GuardEnemy CreateEnemy(Vector3 position)
        {
            var go = Create("Guard", position);
            go.layer = 6;
            go.SetActive(false);
            var enemy = go.AddComponent<GuardEnemy>();
            go.AddComponent<EnemyReadability>();
            go.SetActive(true);
            enemy.GetComponent<CharacterController>().minMoveDistance = 0f;
            return enemy;
        }

        private EnemyBase CallFindNearest()
        {
            possession.enabled = true;
            var method = typeof(PossessionController).GetMethod("FindNearestEnemy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var result = (EnemyBase)method.Invoke(possession, null);
            possession.enabled = false;
            return result;
        }

        private GameObject Create(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            objects.Add(obj);
            return obj;
        }

        private static T Read<T>(object target, string field)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(field,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) return (T)info.GetValue(target);
                type = type.BaseType;
            }
            throw new System.MissingFieldException(target.GetType().Name, field);
        }

        private static void Set(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(field,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) { info.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }
    }
}
