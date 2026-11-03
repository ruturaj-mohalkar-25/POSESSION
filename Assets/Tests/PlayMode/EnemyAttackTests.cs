using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    // Person 2: enemies attack from the start of the level once they spot the Spirit,
    // and search its last seen position before returning to patrol. An eject drops the
    // Spirit behind the body it left, and that body holds its attack for a short grace time.
    public class EnemyAttackTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GuardEnemy guard;
        private EnemyContactDamage attack;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
            var guardObject = Create("Guard", new Vector3(0f, 1.05f, 0f));
            guardObject.SetActive(false);
            guard = guardObject.AddComponent<GuardEnemy>();
            // No ShootAbility on this test body, so it uses the touch attack.
            attack = guardObject.AddComponent<EnemyContactDamage>();
            // Vision is directional once the P1 cone lands; face the Spirit these tests use (+x).
            guardObject.transform.rotation = Quaternion.LookRotation(Vector3.right);
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

        [UnityTest]
        public IEnumerator PatrollingEnemyAttacksSpottedSpiritWithoutForcedEject()
        {
            Set(attack, "spotReactionTime", 0.4f);
            var spirit = CreateSpirit(new Vector3(3f, 1f, 0f));

            yield return new WaitForSeconds(0.2f);
            Assert.That(guard.IsChasing, Is.True);
            Assert.That(guard.IsHostile, Is.False);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spirit.MaxHP),
                "Enemy should give a short warning before the first attack.");

            yield return new WaitForSeconds(0.6f);
            Assert.That(spirit.CurrentHP, Is.LessThan(spirit.MaxHP),
                "A patrolling enemy that spots the Spirit should attack it.");
        }

        [UnityTest]
        public IEnumerator EnemyDoesNotAttackSpiritItCannotSee()
        {
            Set(attack, "spotReactionTime", 0f);
            var spirit = CreateSpirit(new Vector3(30f, 1f, 0f));

            yield return new WaitForSeconds(0.8f);
            Assert.That(guard.IsChasing, Is.False);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spirit.MaxHP));
        }

        [UnityTest]
        public IEnumerator PossessedEnemyDoesNotAttack()
        {
            Set(attack, "spotReactionTime", 0f);
            var spirit = CreateSpirit(new Vector3(1.6f, 1f, 0f));
            guard.OnPossessed();

            yield return new WaitForSeconds(0.5f);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spirit.MaxHP));
        }

        [UnityTest]
        public IEnumerator LostSightSearchesLastSeenPositionThenResumesPatrol()
        {
            SetBase(guard, "waypoints", new[] {
                Create("Patrol WP", new Vector3(-20f, 1f, 0f)).transform });
            SetBase(guard, "loseSightSearchTime", 0.6f);
            Set(attack, "spotReactionTime", 5f);
            var spirit = CreateSpirit(new Vector3(6f, 1f, 0f));

            yield return new WaitForSeconds(0.3f);
            Assert.That(guard.IsChasing, Is.True);

            spirit.transform.position = new Vector3(60f, 1f, 0f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(guard.IsChasing, Is.False);
            Assert.That(guard.IsSearching, Is.True);

            float searchStartX = guard.transform.position.x;
            yield return new WaitForSeconds(0.25f);
            Assert.That(guard.transform.position.x, Is.GreaterThan(searchStartX),
                "Searching enemy should walk toward the last seen position.");

            yield return new WaitForSeconds(0.5f);
            Assert.That(guard.IsSearching, Is.False);
            float resumedX = guard.transform.position.x;
            yield return new WaitForSeconds(0.25f);
            Assert.That(guard.transform.position.x, Is.LessThan(resumedX - 0.05f),
                "Enemy should return to its patrol after the search.");
        }

        [UnityTest]
        public IEnumerator SpottingAgainDuringSearchResumesChase()
        {
            SetBase(guard, "loseSightSearchTime", 2f);
            var spirit = CreateSpirit(new Vector3(6f, 1f, 0f));
            yield return new WaitForSeconds(0.2f);

            spirit.transform.position = new Vector3(60f, 1f, 0f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(guard.IsSearching, Is.True);

            spirit.transform.position = guard.transform.position + new Vector3(4f, 0f, 0f);
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(guard.IsChasing, Is.True);
            Assert.That(guard.IsSearching, Is.False);
        }

        [UnityTest]
        public IEnumerator EjectedSpiritLandsBehindEnemyOutsideItsCone()
        {
            var spirit = CreateSpirit(new Vector3(3f, 1f, 0f));
            var possession = spirit.gameObject.AddComponent<PossessionController>();
            Invoke(possession, "Possess", guard);
            yield return null;

            Assert.That(possession.TryEject(false), Is.True);
            Physics.SyncTransforms();
            Assert.That(spirit.transform.position.x, Is.LessThan(guard.transform.position.x),
                "Spirit should be ejected behind the body it left.");
            Assert.That(guard.CanSee(spirit.transform), Is.False);
        }

        [UnityTest]
        public IEnumerator EjectedBodyWaitsBeforeAttacking()
        {
            Set(attack, "spotReactionTime", 0f);
            Set(attack, "ejectGraceTime", 0.8f);
            var spirit = CreateSpirit(new Vector3(3f, 1f, 0f));
            var possession = spirit.gameObject.AddComponent<PossessionController>();
            Invoke(possession, "Possess", guard);
            yield return null;
            Assert.That(possession.TryEject(false), Is.True);

            // Step into the body's view and touch range straight after the eject.
            spirit.transform.position = guard.transform.position + new Vector3(1.6f, 0f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.5f);
            Assert.That(guard.IsChasing, Is.True);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spirit.MaxHP),
                "The body the Spirit just left should hold its attack during the grace time.");

            yield return new WaitForSeconds(0.6f);
            Assert.That(spirit.CurrentHP, Is.LessThan(spirit.MaxHP),
                "The body should attack once the grace time ends.");
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
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.MissingFieldException(target.GetType().Name, field);
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, args);
        }

        private static void SetBase(object target, string field, object value)
        {
            typeof(EnemyBase).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
