using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Possession.Tests
{
    public class GuardEnemyTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GuardEnemy guard;
        private Scene levelScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
            var guardObject = Create("Guard", new Vector3(0f, 1.05f, 0f));
            guardObject.SetActive(false);
            guard = guardObject.AddComponent<GuardEnemy>();
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

        [UnityTearDown]
        public IEnumerator UnloadLevel()
        {
            if (levelScene.IsValid() && levelScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(levelScene);
        }

#if UNITY_EDITOR
        [Test]
        public void SavedPrefabHasControllerThreeHPAndEnemyLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Guard.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab, new Vector3(20f, 1.1f, 0f),
                Quaternion.identity);
            objects.Add(instance);
            var enemy = instance.GetComponent<GuardEnemy>();
            Assert.That(enemy.CurrentHP, Is.EqualTo(3));
            Assert.That(instance.layer, Is.EqualTo(6));
            Assert.That(instance.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(instance.GetComponent<CapsuleCollider>(), Is.Null);
            Assert.That(enemy.CanBePossessed, Is.True);
        }
#endif

        [UnityTest]
        public IEnumerator Level01HasConfiguredGuardAndPatrolRoute()
        {
            yield return SceneManager.LoadSceneAsync("Level_01", LoadSceneMode.Additive);
            levelScene = SceneManager.GetSceneByName("Level_01");
            GuardEnemy levelGuard = null;
            foreach (var root in levelScene.GetRootGameObjects())
            {
                var enemy = root.GetComponent<GuardEnemy>();
                if (enemy != null) levelGuard = enemy;
            }

            Assert.That(levelGuard, Is.Not.Null);
            Assert.That(levelGuard.CurrentHP, Is.EqualTo(3));
            Assert.That(levelGuard.GetComponent<CharacterController>(), Is.Not.Null);
            Transform[] route = (Transform[])typeof(EnemyBase).GetField("waypoints",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(levelGuard);
            Assert.That(route, Has.Length.EqualTo(2));
            Assert.That(route[0], Is.Not.Null);
            Assert.That(route[1], Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Level02HasTwoConfiguredStaggeredGuardPatrols()
        {
            yield return SceneManager.LoadSceneAsync("Level_02", LoadSceneMode.Additive);
            levelScene = SceneManager.GetSceneByName("Level_02");
            var guards = FindLevelGuards(levelScene);

            Assert.That(guards, Has.Count.EqualTo(2));
            AssertConfiguredRoutes(guards);
            Assert.That(guards[0].PatrolStartDelay, Is.Not.EqualTo(guards[1].PatrolStartDelay),
                "Level 2 Guard patrols should start at different times.");
            foreach (GuardEnemy levelGuard in guards)
            {
                Assert.That(levelGuard.CurrentHP, Is.EqualTo(3));
                Assert.That(levelGuard.DetectionRange, Is.EqualTo(12f).Within(0.01f));
                Assert.That(Read<float>(levelGuard, "moveSpeed"), Is.EqualTo(3f).Within(0.01f));
                Assert.That(Read<float>(levelGuard, "waypointWaitTime"), Is.EqualTo(0.75f).Within(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator Level03HasTwoTightTwelveMetreGuardPatrols()
        {
            yield return SceneManager.LoadSceneAsync("Level_03", LoadSceneMode.Additive);
            levelScene = SceneManager.GetSceneByName("Level_03");
            var guards = FindLevelGuards(levelScene);

            Assert.That(guards, Has.Count.EqualTo(2));
            AssertConfiguredRoutes(guards);
            foreach (GuardEnemy levelGuard in guards)
            {
                Assert.That(levelGuard.CurrentHP, Is.EqualTo(3));
                Assert.That(levelGuard.DetectionRange, Is.EqualTo(12f).Within(0.01f));
                Assert.That(Read<float>(levelGuard, "moveSpeed"), Is.EqualTo(3.25f).Within(0.01f));
                Assert.That(Read<float>(levelGuard, "waypointWaitTime"), Is.EqualTo(0.5f).Within(0.01f));
                Assert.That(Read<float>(levelGuard, "chaseStopDistance"), Is.EqualTo(1.25f).Within(0.01f));
                Transform[] route = GetRoute(levelGuard);
                Assert.That(Vector3.Distance(route[0].position, route[1].position), Is.LessThan(8f),
                    levelGuard.name + " should use a compact Level 3 patrol route.");
            }
        }

        [UnityTest]
        public IEnumerator PatrolMovesIdlesAtWaypointAndContinues()
        {
            Set(guard, "waypointArrivalDistance", 0.03f);
            Set(guard, "waypointWaitTime", 0.25f);
            SetBase(guard, "waypoints", new[] {
                Create("Near WP", new Vector3(0.3f, 1f, 0f)).transform,
                Create("Return WP", new Vector3(-3f, 1f, 0f)).transform });

            float deadline = Time.time + 2f;
            while (guard.transform.position.x < 0.27f && Time.time < deadline)
                yield return null;
            Assert.That(guard.transform.position.x, Is.GreaterThanOrEqualTo(0.27f));
            yield return null;
            Assert.That(guard.IsIdle, Is.True);
            float x = guard.transform.position.x;
            yield return new WaitForSeconds(0.1f);
            Assert.That(guard.transform.position.x, Is.EqualTo(x).Within(0.01f));
            yield return new WaitForSeconds(0.7f);
            Assert.That(guard.transform.position.x, Is.LessThan(x - 0.05f),
                "Guard should leave idle and continue toward the next waypoint.");
        }

        [UnityTest]
        public IEnumerator OverlapSphereDetectsChildColliderAndReturnsToPatrol()
        {
            SetBase(guard, "waypoints", new[] {
                Create("Patrol WP", new Vector3(-20f, 1f, 0f)).transform });
            var spirit = CreateSpirit(new Vector3(5f, 1f, 0f), true);
            yield return new WaitForSeconds(0.3f);
            Assert.That(guard.IsChasing, Is.True);
            Assert.That(guard.transform.position.x, Is.GreaterThan(0.2f));

            float x = guard.transform.position.x;
            spirit.transform.position = new Vector3(30f, 1f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.3f);
            Assert.That(guard.IsChasing, Is.False);
            Assert.That(guard.transform.position.x, Is.LessThan(x));
        }

        [UnityTest]
        public IEnumerator PossessionSuspendsAIAndEjectionRestoresIt()
        {
            CreateSpirit(new Vector3(5f, 1f, 0f));
            yield return null;
            guard.OnPossessed();
            Assert.That(guard.CanBePossessed, Is.False);
            float x = guard.transform.position.x;
            yield return new WaitForSeconds(0.2f);
            Assert.That(guard.IsChasing, Is.False);
            Assert.That(guard.transform.position.x, Is.EqualTo(x).Within(0.01f));

            guard.OnEjected();
            Assert.That(guard.CanBePossessed, Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(guard.IsChasing, Is.True);
            Assert.That(guard.transform.position.x, Is.GreaterThan(x));
        }

        [UnityTest]
        public IEnumerator HostileExpiryResumesAtNearestWaypoint()
        {
            SetBase(guard, "waypoints", new[] {
                Create("Nearest WP", new Vector3(-1f, 1f, 0f)).transform,
                Create("Far WP", new Vector3(8f, 1f, 0f)).transform });
            var spirit = CreateSpirit(new Vector3(15f, 1f, 0f));
            guard.OnForcedEject(0.25f);

            yield return new WaitForSeconds(0.15f);
            Assert.That(guard.transform.position.x, Is.GreaterThan(0.1f),
                "Guard should chase the spirit while hostile.");

            spirit.transform.position = new Vector3(50f, 1f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            float resumedAt = guard.transform.position.x;
            yield return new WaitForSeconds(0.2f);

            Assert.That(guard.IsChasing, Is.False);
            Assert.That(guard.CanBePossessed, Is.True);
            Assert.That(guard.transform.position.x, Is.LessThan(resumedAt - 0.05f),
                "Guard should resume toward the closest waypoint after hostility.");
        }

        [UnityTest]
        public IEnumerator SpiritLayerFiltersDetectionAndPauseStopsMovement()
        {
            var spirit = CreateSpirit(new Vector3(5f, 1f, 0f));
            spirit.gameObject.layer = 8;
            Set(guard, "spiritLayer", (LayerMask)(1 << 9));
            yield return null;
            Assert.That(guard.IsChasing, Is.False);
            Set(guard, "spiritLayer", (LayerMask)(1 << 8));
            yield return null;
            Assert.That(guard.IsChasing, Is.True);

            Time.timeScale = 0f;
            Vector3 position = guard.transform.position;
            yield return null;
            yield return null;
            Assert.That(guard.transform.position, Is.EqualTo(position));
        }

        [UnityTest]
        public IEnumerator CharacterControllerStopsGuardAtWalls()
        {
            var wall = Create("Wall", new Vector3(1.5f, 1f, 0f));
            wall.AddComponent<BoxCollider>().size = new Vector3(0.2f, 4f, 10f);
            SetBase(guard, "waypoints", new[] {
                Create("Blocked WP", new Vector3(5f, 1f, 0f)).transform });
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1f);
            Assert.That(guard.transform.position.x, Is.InRange(0.5f, 1.1f));
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
            var rb = obj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            var spirit = obj.AddComponent<SpiritController>();
            if (childCollider)
            {
                var child = Create("Spirit collider", position);
                child.transform.SetParent(obj.transform);
                child.transform.localPosition = Vector3.zero;
                child.AddComponent<CapsuleCollider>();
            }
            else obj.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();
            return spirit;
        }

        private static List<GuardEnemy> FindLevelGuards(Scene scene)
        {
            var guards = new List<GuardEnemy>();
            foreach (GameObject root in scene.GetRootGameObjects())
                guards.AddRange(root.GetComponentsInChildren<GuardEnemy>(true));
            return guards;
        }

        private static void AssertConfiguredRoutes(IEnumerable<GuardEnemy> guards)
        {
            foreach (GuardEnemy levelGuard in guards)
            {
                Transform[] route = GetRoute(levelGuard);
                Assert.That(route, Has.Length.EqualTo(2));
                Assert.That(route[0], Is.Not.Null);
                Assert.That(route[1], Is.Not.Null);
            }
        }

        private static Transform[] GetRoute(GuardEnemy levelGuard)
        {
            return Read<Transform[]>(levelGuard, "waypoints");
        }

        private static T Read<T>(object target, string field)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) return (T)info.GetValue(target);
                type = type.BaseType;
            }
            throw new System.MissingFieldException(target.GetType().Name, field);
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

        private static void SetBase(object target, string field, object value)
        {
            typeof(EnemyBase).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
