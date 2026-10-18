using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class HeavyLevelIntegrationTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var level = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("P3_HeavyTestCleanup");
            SceneManager.SetActiveScene(empty);
            if (level.IsValid() && level.isLoaded)
                yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest] public IEnumerator Level02HasConfiguredHeavyAndPatrol() => CheckConfiguration("Level_02", 1, 2f);
        [UnityTest] public IEnumerator Level03HasTwoConfiguredHeaviesAndPatrols() => CheckConfiguration("Level_03", 2, 2.2f);
        [UnityTest] public IEnumerator Level02HeavySmashesItsWall() => CheckSmashes("Level_02");
        [UnityTest] public IEnumerator Level03HeaviesSmashBothWallsInSequence() => CheckSmashes("Level_03");
        [UnityTest] public IEnumerator Level02ManualAndForcedEjectionStayClearOfWalls() => CheckEjections("Level_02");
        [UnityTest] public IEnumerator Level03ManualAndForcedEjectionStayClearOfWalls() => CheckEjections("Level_03");

        private static IEnumerator Load(string name)
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(name);
            yield return null;
        }

        private static HeavyEnemy[] Heavies() => Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None)
            .OrderBy(enemy => enemy.name).ToArray();

        private static BreakableWall[] Walls() => Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None)
            .OrderBy(wall => wall.transform.position.z).ToArray();

        private static IEnumerator CheckConfiguration(string name, int count, float speed)
        {
            yield return Load(name);
            var heavies = Heavies();
            Assert.That(heavies.Length, Is.EqualTo(count));
            Assert.That(Walls().Length, Is.EqualTo(count));
            var starts = heavies.Select(enemy => enemy.transform.position).ToArray();
            foreach (var heavy in heavies)
            {
                Assert.That(heavy.CurrentHP, Is.EqualTo(5));
                Assert.That(Read<float>(heavy, "moveSpeed"), Is.EqualTo(speed));
                Assert.That(heavy.gameObject.layer, Is.EqualTo(6));
                Assert.That(heavy.GetComponent<CharacterController>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<EnemyContactDamage>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<HeavySmashIndicator>(), Is.Not.Null);
                Assert.That(Read<LayerMask>(heavy.GetComponent<SmashAbility>(), "smashableLayer").value,
                    Is.EqualTo(1 << 7));
                var waypoints = Read<Transform[]>(heavy, "waypoints");
                Assert.That(waypoints.Length, Is.EqualTo(2));
                foreach (var point in waypoints)
                {
                    Assert.That(point, Is.Not.Null);
                    Assert.That(point.IsChildOf(heavy.transform), Is.False);
                    Assert.That(point.gameObject.scene, Is.EqualTo(heavy.gameObject.scene));
                }
            }
            // Level_02 intentionally starts at WP1 and pauses before walking to WP2.
            yield return new WaitForSeconds(1.15f);
            for (int i = 0; i < heavies.Length; i++)
            {
                Vector3 movement = heavies[i].transform.position - starts[i];
                movement.y = 0f;
                Assert.That(movement.magnitude, Is.GreaterThan(0.15f));
            }
        }

        private static IEnumerator CheckSmashes(string name)
        {
            yield return Load(name);
            OpenDoor();
            var possession = Object.FindFirstObjectByType<PossessionController>();
            var heavies = Heavies();
            var walls = Walls();
            for (int i = 0; i < heavies.Length; i++)
            {
                Teleport(heavies[i], new Vector3(0f, 1.45f, walls[i].transform.position.z - 1.5f));
                Invoke(possession, "Possess", heavies[i]);
                Assert.That(heavies[i].GetComponent<HeavySmashIndicator>().IsVisible, Is.True);
                heavies[i].GetComponent<SmashAbility>().TryExecute();
                Assert.That(walls[i].CurrentHP, Is.LessThanOrEqualTo(0));
                Assert.That(walls[i].gameObject.activeSelf, Is.False);
                Physics.SyncTransforms();
                Assert.That(Physics.Raycast(new Vector3(0f, 1.5f, walls[i].transform.position.z - 0.6f),
                    Vector3.forward, 1.2f, 1 << 7, QueryTriggerInteraction.Ignore), Is.False);
                Invoke(possession, "Eject", false);
                Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
                AssertSafeSpirit(Object.FindFirstObjectByType<SpiritController>());
                Assert.That(heavies[i].GetComponent<HeavySmashIndicator>().IsVisible, Is.False);
                yield return null;
            }
            Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.True);
        }

        private static IEnumerator CheckEjections(string name)
        {
            yield return Load(name);
            OpenDoor();
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            var possession = spirit.GetComponent<PossessionController>();
            Set(possession, "hostileTime", 0.1f);
            var heavies = Heavies();
            var walls = Walls();
            for (int i = 0; i < heavies.Length; i++)
            {
                float wallZ = walls[i].transform.position.z;
                var positions = new[]
                {
                    new Vector3(0f, 1.45f, wallZ - 1.3f), // Central puzzle passage.
                    new Vector3(5.1f, 1.45f, wallZ - 3f), // Perimeter wall.
                    new Vector3(5.1f, 1.45f, wallZ - 1.1f) // Perimeter/divider corner.
                };
                foreach (var position in positions)
                    foreach (bool forced in new[] { false, true })
                    {
                        Teleport(heavies[i], position);
                        Set(possession, "possessDuration", forced ? 0.04f : 10f);
                        Invoke(possession, "Possess", heavies[i]);
                        if (!forced) Invoke(possession, "Eject", false);
                        float deadline = Time.realtimeSinceStartup + 1f;
                        while (possession.CurrentState != PlayerState.Spirit && Time.realtimeSinceStartup < deadline)
                            yield return null;
                        Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
                        AssertSafeSpirit(spirit);
                        Assert.That(heavies[i].GetComponent<HeavySmashIndicator>().IsVisible, Is.False);
                        yield return new WaitForSeconds(0.15f);
                        Assert.That(spirit.transform.position.y, Is.LessThan(1.7f),
                            "Ejection must not launch the Spirit above floor height.");
                        Assert.That(spirit.CurrentHP, Is.GreaterThan(0));
                    }
            }
        }

        private static void AssertSafeSpirit(SpiritController spirit)
        {
            Physics.SyncTransforms();
            var capsule = spirit.GetComponent<CapsuleCollider>();
            foreach (var other in Physics.OverlapSphere(spirit.transform.position, 2.5f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (other.transform.IsChildOf(spirit.transform)) continue;
                Assert.That(Physics.ComputePenetration(capsule, spirit.transform.position,
                    spirit.transform.rotation, other, other.transform.position, other.transform.rotation,
                    out _, out float distance) && distance > 0.01f, Is.False,
                    "Spirit intersects " + other.name);
            }
        }

        private static void OpenDoor()
        {
            foreach (var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) target.OnHit();
        }

        private static void Teleport(HeavyEnemy heavy, Vector3 position)
        {
            var controller = heavy.GetComponent<CharacterController>();
            controller.enabled = false;
            heavy.transform.SetPositionAndRotation(position, Quaternion.identity);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static T Read<T>(object obj, string field)
        {
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (info != null) return (T)info.GetValue(obj);
            }
            throw new System.MissingFieldException(field);
        }

        private static void Set(object obj, string field, object value) => obj.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);

        private static void Invoke(object obj, string method, params object[] args) => obj.GetType()
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, args);
    }
}
