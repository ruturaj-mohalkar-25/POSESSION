using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Possession.Tests
{
    public class HeavyEnemyTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private HeavyEnemy heavy;
        private Scene levelScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
            var heavyObject = Create("Heavy", new Vector3(0f, 1.05f, 0f));
            heavyObject.SetActive(false);
            heavy = heavyObject.AddComponent<HeavyEnemy>();
            Set(heavy, "moveSpeed", 1.8f);
            Set(heavy, "maxHP", 5);
            heavyObject.SetActive(true);
            heavy.GetComponent<CharacterController>().minMoveDistance = 0f;
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

        [UnityTearDown]
        public IEnumerator UnloadLevel()
        {
            if (levelScene.IsValid() && levelScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(levelScene);
        }

#if UNITY_EDITOR
        [Test]
        public void SavedPrefabHasControllerFiveHPAndEnemyLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Heavy.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab, new Vector3(20f, 1.3f, 0f), Quaternion.identity);
            objects.Add(instance);
            var enemy = instance.GetComponent<HeavyEnemy>();
            Assert.That(enemy.CurrentHP, Is.EqualTo(5));
            Assert.That(instance.layer, Is.EqualTo(6));
            Assert.That(instance.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(instance.GetComponent<CapsuleCollider>(), Is.Null);
            Assert.That(enemy.CanBePossessed, Is.True);
        }
#endif

        [UnityTest]
        public IEnumerator Level01HasConfiguredHeavyAndPatrolRoute()
        {
            yield return SceneManager.LoadSceneAsync("Level_01", LoadSceneMode.Additive);
            levelScene = SceneManager.GetSceneByName("Level_01");
            HeavyEnemy levelHeavy = null;
            foreach (var root in levelScene.GetRootGameObjects())
            {
                var enemy = root.GetComponent<HeavyEnemy>();
                if (enemy != null) levelHeavy = enemy;
            }
            Assert.That(levelHeavy, Is.Not.Null);
            Assert.That(levelHeavy.CurrentHP, Is.EqualTo(5));
            Assert.That(levelHeavy.GetComponent<CharacterController>(), Is.Not.Null);
            Vector3 start = levelHeavy.transform.position;
            yield return new WaitForSeconds(0.2f);
            Vector3 movement = levelHeavy.transform.position - start;
            movement.y = 0f;
            Assert.That(movement.magnitude, Is.GreaterThan(0.1f));
        }

        [Test]
        public void HeavyHasFiveHPAndDiesOnFifthHit()
        {
            Assert.That(heavy.CurrentHP, Is.EqualTo(5));
            for (int i = 0; i < 4; i++) heavy.TakeDamage(1);
            Assert.That(heavy.CurrentHP, Is.EqualTo(1));
            Assert.That(heavy.gameObject.activeSelf, Is.True);
            heavy.TakeDamage(1);
            Assert.That(heavy.gameObject.activeSelf, Is.False);
            Assert.That(heavy.CanBePossessed, Is.False);
        }

        [Test]
        public void HeavyKeepsConfiguredInspectorStats()
        {
            var obj = Create("Configured Heavy", new Vector3(20f, 1f, 0f));
            obj.SetActive(false);
            var enemy = obj.AddComponent<HeavyEnemy>();
            Set(enemy, "maxHP", 8);
            Set(enemy, "moveSpeed", 1.2f);
            obj.SetActive(true);
            Assert.That(enemy.CurrentHP, Is.EqualTo(8));
            Assert.That(typeof(EnemyBase).GetField("moveSpeed", BindingFlags.Instance
                | BindingFlags.NonPublic).GetValue(enemy), Is.EqualTo(1.2f));
        }

        [UnityTest]
        public IEnumerator IntegratedPossessionKeepsControllerRunningAndAllowsManualEject()
        {
            var spirit = CreateSpirit(new Vector3(3f, 1f, 0f));
            var possession = spirit.gameObject.AddComponent<PossessionController>();
            Invoke(possession, "Possess", heavy);
            Assert.That(spirit.gameObject.activeInHierarchy, Is.True);
            Assert.That(spirit.GetComponent<Collider>().enabled, Is.False);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Possessed_Heavy));
            float initialTimer = possession.PossessTimer;
            yield return new WaitForSeconds(0.1f);
            Assert.That(possession.PossessTimer, Is.LessThan(initialTimer));
            Invoke(possession, "Eject", false);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(spirit.GetComponent<Collider>().enabled, Is.True);
            Assert.That(spirit.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(heavy.CanBePossessed, Is.True);
        }

        [UnityTest]
        public IEnumerator IntegratedTimerForcesEjectionAndHeavyHostilityExpires()
        {
            var spirit = CreateSpirit(new Vector3(3f, 1f, 0f));
            var possession = spirit.gameObject.AddComponent<PossessionController>();
            Set(possession, "possessDuration", 0.1f);
            Set(possession, "hostileTime", 0.3f);
            Invoke(possession, "Possess", heavy);
            yield return new WaitForSeconds(0.2f);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(spirit.GetComponent<Collider>().enabled, Is.True);
            Assert.That(heavy.CanBePossessed, Is.False);
            yield return new WaitForSeconds(0.3f);
            Assert.That(heavy.CanBePossessed, Is.True);
        }

        [UnityTest]
        public IEnumerator PatrolMovesAtSixtyPercentOfGuardSpeed()
        {
            var guard = Create("Guard", new Vector3(0f, 1f, 20f)).AddComponent<GuardEnemy>();
            Set(guard, "waypoints", new[] { Create("Guard WP", new Vector3(50f, 1f, 20f)).transform });
            Set(heavy, "waypoints", new[] { Create("Heavy WP", new Vector3(50f, 1f, 0f)).transform });
            yield return new WaitForSeconds(0.5f);
            Assert.That(heavy.transform.position.x, Is.GreaterThan(0.5f));
            Assert.That(heavy.transform.position.x / guard.transform.position.x,
                Is.EqualTo(0.6f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator PatrolReachesWaypointsWaitsAndLoops()
        {
            Set(heavy, "waypointArrivalDistance", 0.03f);
            Set(heavy, "waypointWaitTime", 0.25f);
            Set(heavy, "waypoints", new[] {
                Create("Near WP", new Vector3(0.3f, 1f, 0f)).transform,
                Create("Return WP", new Vector3(-0.3f, 1f, 0f)).transform });
            float deadline = Time.time + 2f;
            while (heavy.transform.position.x < 0.27f && Time.time < deadline)
                yield return null;
            Assert.That(heavy.transform.position.x, Is.GreaterThanOrEqualTo(0.27f));
            yield return null; // Advance the patrol index and enter its wait state.
            float x = heavy.transform.position.x;
            yield return new WaitForSeconds(0.1f);
            Assert.That(heavy.transform.position.x, Is.EqualTo(x).Within(0.01f));
            yield return new WaitForSeconds(0.7f);
            Assert.That(heavy.transform.position.x, Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator EmptyAndMissingWaypointsAreSafe()
        {
            yield return new WaitForSeconds(0.1f);
            Assert.That(heavy.transform.position.x, Is.EqualTo(0f).Within(0.01f));
            Set(heavy, "waypoints", new Transform[] { null, null });
            yield return new WaitForSeconds(0.1f);
            Set(heavy, "waypoints", new[] { null,
                Create("Valid WP", new Vector3(5f, 1f, 0f)).transform });
            yield return new WaitForSeconds(0.2f);
            Assert.That(heavy.transform.position.x, Is.GreaterThan(0.1f));
        }

        [UnityTest]
        public IEnumerator DetectsChildColliderChasesAndResumesPatrolWhenSpiritLeaves()
        {
            Set(heavy, "waypoints", new[] { Create("Patrol WP", new Vector3(-20f, 1f, 0f)).transform });
            var spirit = CreateSpirit(new Vector3(5f, 1f, 0f), true);
            yield return new WaitForSeconds(0.3f);
            Assert.That(heavy.IsChasing, Is.True);
            Assert.That(heavy.transform.position.x, Is.GreaterThan(0.2f));
            float x = heavy.transform.position.x;
            spirit.transform.position = new Vector3(30f, 1f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.3f);
            Assert.That(heavy.IsChasing, Is.False);
            Assert.That(heavy.transform.position.x, Is.LessThan(x));
        }

        [UnityTest]
        public IEnumerator PossessionSuspendsAIAndManualEjectionRestoresIt()
        {
            CreateSpirit(new Vector3(5f, 1f, 0f));
            yield return null;
            heavy.OnPossessed();
            Assert.That(heavy.CanBePossessed, Is.False);
            float x = heavy.transform.position.x;
            yield return new WaitForSeconds(0.2f);
            Assert.That(heavy.IsChasing, Is.False);
            Assert.That(heavy.transform.position.x, Is.EqualTo(x).Within(0.01f));
            heavy.OnEjected();
            Assert.That(heavy.CanBePossessed, Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(heavy.IsChasing, Is.True);
            Assert.That(heavy.transform.position.x, Is.GreaterThan(x));
        }

        [UnityTest]
        public IEnumerator ForcedEjectionFindsHiddenSpiritChasesAndExpires()
        {
            var spirit = CreateSpirit(new Vector3(15f, 1f, 0f)); // Outside normal detection.
            spirit.gameObject.SetActive(false);
            heavy.OnPossessed();
            heavy.OnForcedEject(0.3f);
            Assert.That(heavy.CanBePossessed, Is.False);
            spirit.gameObject.SetActive(true);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.15f);
            Assert.That(heavy.IsChasing, Is.True);
            Assert.That(heavy.transform.position.x, Is.GreaterThan(0.1f));
            yield return new WaitForSeconds(0.25f);
            Assert.That(heavy.CanBePossessed, Is.True);
            Assert.That(heavy.IsChasing, Is.False);
        }

        [UnityTest]
        public IEnumerator ChaseStopsBeforeOverlappingSpirit()
        {
            var spirit = CreateSpirit(new Vector3(2f, 1f, 0f));
            yield return new WaitForSeconds(0.5f);
            float separation = spirit.transform.position.x - heavy.transform.position.x;
            Assert.That(separation, Is.EqualTo(1.5f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator MovementCannotPassThroughWalls()
        {
            var wall = Create("Wall", new Vector3(1.5f, 1f, 0f));
            wall.AddComponent<BoxCollider>().size = new Vector3(0.2f, 4f, 10f);
            Set(heavy, "waypoints", new[] { Create("Blocked WP", new Vector3(5f, 1f, 0f)).transform });
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1f);
            Assert.That(heavy.transform.position.x, Is.InRange(0.5f, 1.1f));
        }

        [UnityTest]
        public IEnumerator SpiritLayerFiltersDetectionAndPauseStopsMovement()
        {
            var spirit = CreateSpirit(new Vector3(5f, 1f, 0f));
            spirit.gameObject.layer = 8;
            Set(heavy, "spiritLayer", (LayerMask)(1 << 9));
            yield return null;
            Assert.That(heavy.IsChasing, Is.False);
            Set(heavy, "spiritLayer", (LayerMask)(1 << 8));
            yield return null;
            Assert.That(heavy.IsChasing, Is.True);
            Time.timeScale = 0f;
            Vector3 position = heavy.transform.position;
            yield return null;
            yield return null;
            Assert.That(heavy.transform.position, Is.EqualTo(position));
        }

        private GameObject Create(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            objects.Add(obj);
            return obj;
        }

        private SpiritController CreateSpirit(Vector3 position, bool childCollider = false)
        {
            var obj = Create("Spirit", position);
            obj.AddComponent<Rigidbody>().isKinematic = true;
            var spirit = obj.AddComponent<SpiritController>();
            // Spirit movement would overwrite the test target's position; keep the real
            // component enabled (detection checks it) and freeze its Rigidbody instead.
            var rb = obj.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            if (childCollider)
            {
                var child = Create("Spirit collider", position);
                child.transform.SetParent(obj.transform);
                child.AddComponent<CapsuleCollider>();
            }
            else obj.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();
            return spirit;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void Invoke(object target, string method, params object[] arguments)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }
    }
}
