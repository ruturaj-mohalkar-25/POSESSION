using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class EnemyRouteW3Tests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var level = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("EnemyRouteW3_Cleanup");
            SceneManager.SetActiveScene(empty);
            if (level.IsValid() && level.isLoaded)
                yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator Level01GuardsCompleteW3BehaviorChecks()
        {
            yield return CheckLevel("Level_01", 1);
        }

        [UnityTest]
        public IEnumerator Level02GuardsCompleteW3BehaviorChecks()
        {
            yield return CheckLevel("Level_02", 2);
        }

        [UnityTest]
        public IEnumerator Level03GuardsCompleteW3BehaviorChecks()
        {
            yield return CheckLevel("Level_03", 2);
        }

        private static IEnumerator CheckLevel(string levelName, int expectedCount)
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(levelName);
            yield return null;

            var guards = Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None)
                .OrderBy(guard => guard.name).ToArray();
            Assert.That(guards, Has.Length.EqualTo(expectedCount));

            var spirit = Object.FindFirstObjectByType<SpiritController>();
            Assert.That(spirit, Is.Not.Null);
            var possession = spirit.GetComponent<PossessionController>();
            if (possession != null) possession.enabled = false;

            spirit.gameObject.SetActive(false);
            var starts = guards.Select(guard => guard.transform.position).ToArray();
            foreach (GuardEnemy guard in guards)
                AssertRouteIsConfiguredAndClear(guard);

            float longestDelay = guards.Max(guard => guard.PatrolStartDelay);
            yield return new WaitForSeconds(longestDelay + 0.8f);
            for (int i = 0; i < guards.Length; i++)
            {
                Vector3 displacement = Flat(guards[i].transform.position - starts[i]);
                Assert.That(displacement.magnitude, Is.GreaterThan(0.15f),
                    levelName + "/" + guards[i].name + " did not patrol its saved route.");
            }

            foreach (GuardEnemy guard in guards)
            {
                foreach (GuardEnemy other in guards)
                    other.enabled = other == guard;

                Transform[] route = Read<Transform[]>(guard, "waypoints");
                Vector3 towardRoute = Flat(route[1].position - guard.transform.position).normalized;
                if (towardRoute.sqrMagnitude < 0.01f) towardRoute = Vector3.forward;

                spirit.gameObject.SetActive(true);
                MoveSpirit(spirit, guard.transform.position + towardRoute * 4f);
                Vector3 beforeDetection = guard.transform.position;
                yield return new WaitForSeconds(0.25f);
                Assert.That(guard.IsChasing, Is.True,
                    levelName + "/" + guard.name + " did not detect the Spirit.");
                Assert.That(Flat(guard.transform.position - beforeDetection).magnitude,
                    Is.GreaterThan(0.05f), levelName + "/" + guard.name + " did not chase.");

                guard.OnPossessed();
                Vector3 possessedAt = guard.transform.position;
                yield return new WaitForSeconds(0.15f);
                Assert.That(guard.CanBePossessed, Is.False);
                Assert.That(Flat(guard.transform.position - possessedAt).magnitude, Is.LessThan(0.02f),
                    levelName + "/" + guard.name + " AI moved while possessed.");

                MoveSpirit(spirit, guard.transform.position + towardRoute * 4f);
                guard.OnForcedEject(0.25f);
                Vector3 hostileAt = guard.transform.position;
                yield return new WaitForSeconds(0.12f);
                Assert.That(guard.CanBePossessed, Is.False);
                Assert.That(Flat(guard.transform.position - hostileAt).magnitude, Is.GreaterThan(0.05f),
                    levelName + "/" + guard.name + " did not become hostile after forced eject.");

                spirit.gameObject.SetActive(false);
                yield return new WaitForSeconds(0.2f);
                Assert.That(guard.CanBePossessed, Is.True,
                    levelName + "/" + guard.name + " stayed hostile after its timer expired.");
                Assert.That(guard.IsChasing, Is.False);

                float distanceBeforeResume = NearestRouteDistance(guard.transform.position, route);
                yield return new WaitForSeconds(0.25f);
                float distanceAfterResume = NearestRouteDistance(guard.transform.position, route);
                Assert.That(distanceAfterResume, Is.LessThan(distanceBeforeResume - 0.03f),
                    levelName + "/" + guard.name + " did not resume toward its nearest waypoint.");
            }
        }

        private static void AssertRouteIsConfiguredAndClear(GuardEnemy guard)
        {
            Transform[] route = Read<Transform[]>(guard, "waypoints");
            Assert.That(route, Has.Length.EqualTo(2));
            Assert.That(route[0], Is.Not.Null);
            Assert.That(route[1], Is.Not.Null);
            Assert.That(route[0].IsChildOf(guard.transform), Is.False);
            Assert.That(route[1].IsChildOf(guard.transform), Is.False);

            Vector3 segment = route[1].position - route[0].position;
            segment.y = 0f;
            Assert.That(segment.magnitude, Is.GreaterThan(0.5f));
            Assert.That(Physics.SphereCast(route[0].position, 0.35f, segment.normalized,
                out RaycastHit hit, segment.magnitude, 1 << 0, QueryTriggerInteraction.Ignore), Is.False,
                guard.name + " patrol is blocked by " + (hit.collider != null ? hit.collider.name : "unknown"));
        }

        private static void MoveSpirit(SpiritController spirit, Vector3 position)
        {
            var controller = spirit.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            position.y = spirit.transform.position.y;
            spirit.transform.position = position;
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static float NearestRouteDistance(Vector3 position, Transform[] route)
        {
            return route.Min(point => Flat(point.position - position).magnitude);
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static T Read<T>(object target, string name)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(target);
            }
            throw new System.MissingFieldException(target.GetType().Name, name);
        }
    }
}
