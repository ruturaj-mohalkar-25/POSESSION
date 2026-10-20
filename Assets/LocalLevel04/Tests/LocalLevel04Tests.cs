#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.LocalLevel04.Tests
{
    public class LocalLevel04Tests
    {
        private SpiritController spirit;
        private PossessionController player;
        private HUDManager hud;
        private EnemyBase[] hosts;
        private Door firstDoor, relayDoor;
        private BreakableWall cacheWall, finalWall;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_04_Local");
            yield return null;
            spirit = Object.FindFirstObjectByType<SpiritController>();
            player = spirit.GetComponent<PossessionController>();
            hud = Object.FindFirstObjectByType<HUDManager>();
            hosts = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            firstDoor = GameObject.Find("01_Guard_Door").GetComponent<Door>();
            relayDoor = GameObject.Find("03_Guard_Relay_Door").GetComponent<Door>();
            cacheWall = GameObject.Find("02_Heavy_Cache_Wall").GetComponent<BreakableWall>();
            finalWall = GameObject.Find("04_Heavy_Finish_Wall").GetComponent<BreakableWall>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            var level = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Level04_TestCleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator SceneUsesSharedSystemsAndConfig()
        {
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.GreaterThan(0));
            Assert.That(hosts.Count(e => e.GetType() == typeof(GuardEnemy)), Is.EqualTo(3));
            Assert.That(hosts.Count(e => e.GetType() == typeof(HeavyEnemy)), Is.EqualTo(2));
            Assert.That(hosts.All(e => e.GetComponent<EnemyContactDamage>() != null), Is.True);
            Assert.That(hosts.All(e => e.GetComponent<Ability>().GetType() ==
                (e is HeavyEnemy ? typeof(SmashAbility) : typeof(ShootAbility))), Is.True);
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Any(c => c.GetType().Namespace == "Possession.LocalLevel04"), Is.False);
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Config/LevelConfig_Level_04.asset");
            Assert.That(config, Is.Not.Null);
            Assert.That(player.PossessDuration, Is.EqualTo(config.PossessionTimer));
            Assert.That(GameManager.Instance.LevelTimer, Is.InRange(config.LevelTime - 2, config.LevelTime));
            foreach (var guard in hosts.OfType<GuardEnemy>())
                Assert.That(Read<float>(guard, "detectionRange"), Is.EqualTo(config.GuardDetectionRange));
            foreach (var heavy in hosts.OfType<HeavyEnemy>())
            {
                Assert.That(Read<float>(heavy, "moveSpeed"), Is.EqualTo(Read<float>(hosts.OfType<GuardEnemy>().First(), "moveSpeed") * .6f).Within(.001f));
                Assert.That(heavy.CurrentHP, Is.EqualTo(5));
            }
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials) Assert.That(material, Is.Not.Null, renderer.name);
            Assert.That(Object.FindFirstObjectByType<AnalyticsTracker>(), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SharedShotAndSmashCompleteWholeRouteAndPublishStats()
        {
            FreezeHosts(); player.enabled = false;
            int completions = 0; AnalyticsData? stats = null;
            EventBus.LevelCompleted += () => completions++;
            EventBus.AnalyticsReady += value => stats = value;
            AssertObjective("1/5");
            yield return Shoot("Guard_West", "Target_01_West", new Vector3(10, 1.15f, 32.5f), Vector3.forward);
            Assert.That(firstDoor.IsOpen, Is.True); Assert.That(relayDoor.IsOpen, Is.False);
            AssertObjective("2/5");
            Smash("Heavy_Cache", cacheWall, new Vector3(33.1f, 1.45f, 20), Vector3.left);
            AssertObjective("3/5");
            yield return Shoot("Guard_Relay", "Target_02_CacheRelay", new Vector3(21.5f, 1.15f, 16), Vector3.left);
            Assert.That(relayDoor.IsOpen, Is.True); AssertObjective("4/5");
            Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.False);
            Smash("Heavy_Finish", finalWall, new Vector3(53, 1.45f, 26.9f), Vector3.forward);
            AssertObjective("5/5");
            Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.True);
            spirit.RestoreAt(new Vector3(53, 1.15f, 36)); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(stats.HasValue, Is.True);
            Assert.That(stats.Value.possessionCount, Is.EqualTo(4));
            Assert.That(stats.Value.levelName, Is.EqualTo("Level_04_Local"));
            Assert.That(Read<GameObject>(hud, "levelCompletePanel").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SharedExitRejectsUnsolvedPuzzleAndPossessedHeavy()
        {
            FreezeHosts(); player.enabled = false;
            spirit.RestoreAt(new Vector3(53, 1.15f, 36)); Physics.SyncTransforms();
            yield return new WaitForSeconds(.1f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            foreach (var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) target.OnHit();
            cacheWall.TakeDamage(2); finalWall.TakeDamage(2);
            var heavy = hosts.OfType<HeavyEnemy>().First();
            Possess(heavy); Move(heavy, new Vector3(53, 1.45f, 36), Vector3.forward);
            yield return new WaitForSeconds(.1f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            Eject(false); spirit.RestoreAt(new Vector3(53, 1.15f, 36)); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
        }

        [UnityTest]
        public IEnumerator SharedEnemiesPatrolChaseAndSuspendAiWhenPossessed()
        {
            var guard = hosts.OfType<GuardEnemy>().First(e => e.name == "Guard_West");
            var before = guard.transform.position;
            yield return new WaitForSeconds(.7f);
            Assert.That(Vector3.Distance(before, guard.transform.position), Is.GreaterThan(.15f));
            spirit.RestoreAt(guard.transform.position + Vector3.back * 4); Physics.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(guard.IsChasing, Is.True);
            Possess(guard); before = guard.transform.position;
            yield return new WaitForSeconds(.2f);
            Vector3 delta = guard.transform.position - before; delta.y = 0;
            Assert.That(delta.magnitude, Is.LessThan(.05f));
            Assert.That(guard.IsChasing, Is.False);
        }

        [UnityTest]
        public IEnumerator NormalEnemyHealthAndSharedHudRespondToDamageAndForm()
        {
            var guard = hosts.OfType<GuardEnemy>().First();
            guard.TakeDamage(1); Assert.That(guard.CurrentHP, Is.EqualTo(2));
            guard.TakeDamage(2); Assert.That(guard.gameObject.activeSelf, Is.False);
            var heavy = hosts.OfType<HeavyEnemy>().First();
            heavy.TakeDamage(1); Assert.That(heavy.CurrentHP, Is.EqualTo(4));
            Possess(heavy); yield return null;
            Assert.That(Read<Text>(hud, "currentFormText").text, Does.Contain("Heavy"));
            Eject(false); spirit.TakeDamage(1); yield return null;
            Assert.That(Read<Text>(hud, "healthText").text, Is.EqualTo("HP 2/3"));
            Assert.That(Read<Text>(hud, "currentFormText").text, Is.EqualTo("Form: Spirit"));
        }

        [UnityTest]
        public IEnumerator ForcedEjectionUsesSharedHostileWindupAndDamage()
        {
            var heavy = hosts.OfType<HeavyEnemy>().First();
            Move(heavy, new Vector3(36, 1.45f, 20), Vector3.forward);
            Set(player, "possessDuration", .04f); Possess(heavy);
            yield return new WaitForSeconds(.15f);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(heavy.CanBePossessed, Is.False);
            Assert.That(spirit.CurrentHP, Is.EqualTo(3));
            yield return new WaitForSeconds(1.45f);
            Assert.That(spirit.CurrentHP, Is.LessThan(3));
            Assert.That(spirit.CurrentHP, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator SharedRestartWorksAfterFailureAndResetsPuzzleAndClock()
        {
            Object.FindObjectsByType<Target>(FindObjectsSortMode.None).First().OnHit();
            EventBus.LevelFailed?.Invoke();
            Assert.That(Read<GameObject>(hud, "gameOverPanel").activeSelf, Is.True);
            Read<GameObject>(hud, "gameOverPanel").GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "RestartButton").onClick.Invoke();
            yield return null; yield return null;
            float deadline = Time.realtimeSinceStartup + 3;
            while ((GameManager.Instance == null || GameManager.Instance.CurrentState != LevelState.InProgress)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            Assert.That(GameManager.Instance.LevelTimer, Is.GreaterThan(118));
            Assert.That(Object.FindObjectsByType<Target>(FindObjectsSortMode.None).All(t => !t.IsHit), Is.True);
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SharedEjectionStaysGroundedBesideCacheAndBaffle()
        {
            FreezeHosts(); player.enabled = false;
            var heavy = hosts.OfType<HeavyEnemy>().First();
            foreach (var position in new[] { new Vector3(32.3f, 1.45f, 16), new Vector3(35, 1.45f, 20), new Vector3(61, 1.45f, 18) })
            {
                Move(heavy, position, Vector3.left); Possess(heavy); Eject(false);
                Physics.SyncTransforms();
                Assert.That(spirit.transform.position.y, Is.InRange(.9f, 1.5f));
                var capsule = spirit.GetComponent<CapsuleCollider>();
                foreach (var wall in Physics.OverlapSphere(spirit.transform.position, 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (wall.transform.IsChildOf(spirit.transform)) continue;
                    Assert.That(Physics.ComputePenetration(capsule, spirit.transform.position, spirit.transform.rotation,
                        wall, wall.transform.position, wall.transform.rotation, out _, out float distance) && distance > .01f,
                        Is.False, wall.name);
                }
            }
            yield return null;
        }

        [Test]
        public void CacheAndEastBaffleStillRequireTheLongRoute()
        {
            Physics.SyncTransforms();
            Assert.That(Physics.Linecast(new Vector3(35, 1.5f, 20), new Vector3(20, 1.5f, 20), out var hit), Is.True);
            Assert.That(hit.collider.GetComponent<BreakableWall>(), Is.EqualTo(cacheWall));
            Assert.That(Physics.Linecast(new Vector3(45, 1.5f, 5), new Vector3(61, 1.5f, 23)), Is.True);
            Assert.That(Physics.Linecast(new Vector3(64, 1.5f, 5), new Vector3(64, 1.5f, 23)), Is.False);
            foreach (var door in new[] { firstDoor, relayDoor })
                Assert.That(door.GetComponent<Collider>().bounds.size.y, Is.EqualTo(6).Within(.01f));
        }

        [Test]
        public void MuzzleFlashHasNoActiveColliderOnItsSpawnFrame()
        {
            var material = new Material(Shader.Find("Standard"));
            SimpleFX.Burst(new Vector3(30, 10, 30), material, .45f, .05f, .08f);
            var burst = GameObject.Find("FX_Burst");
            Assert.That(burst, Is.Not.Null);
            Assert.That(burst.GetComponent<Collider>() == null || !burst.GetComponent<Collider>().enabled, Is.True);
            Object.Destroy(burst); Object.Destroy(material);
        }

        [UnityTest]
        public IEnumerator ExistingLevelOneHudKeepsItsThreeStepObjectives()
        {
            yield return SceneManager.LoadSceneAsync("Level_01");
            yield return null;
            var sharedHud = Object.FindFirstObjectByType<HUDManager>();
            var objective = Read<Text>(sharedHud, "objectiveText");
            Assert.That(objective.text, Does.StartWith("1/3"));
            Object.FindFirstObjectByType<Target>().OnHit();
            Assert.That(objective.text, Does.StartWith("2/3"));
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            wall.TakeDamage(wall.CurrentHP);
            Assert.That(objective.text, Does.StartWith("3/3"));
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        private IEnumerator Shoot(string hostName, string targetName, Vector3 position, Vector3 forward)
        {
            var guard = hosts.Single(e => e.name == hostName);
            var target = GameObject.Find(targetName).GetComponent<Target>();
            Move(guard, position, forward); Possess(guard);
            var cam = Camera.main; cam.GetComponent<CameraFollow>().enabled = false;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, 0);
            Assert.That(plane.Raycast(ray, out float distance), Is.True);
            var desired = target.transform.position; desired.y = 0;
            cam.transform.position += desired - ray.GetPoint(distance);
            guard.GetComponent<ShootAbility>().TryExecute();
            float until = Time.time + 1;
            while (!target.IsHit && Time.time < until) yield return null;
            Assert.That(target.IsHit, Is.True, "Shared ShootAbility projectile must reach " + targetName);
            Eject(false);
        }
        private void Smash(string hostName, BreakableWall wall, Vector3 position, Vector3 forward)
        {
            var heavy = hosts.Single(e => e.name == hostName);
            Move(heavy, position, forward); Possess(heavy);
            heavy.GetComponent<SmashAbility>().TryExecute();
            Assert.That(wall.CurrentHP, Is.LessThanOrEqualTo(0)); Eject(false);
        }
        private void FreezeHosts() { foreach (var host in hosts) host.enabled = false; }
        private void AssertObjective(string prefix) => Assert.That(Read<Text>(hud, "objectiveText").text, Does.StartWith(prefix));
        private void Possess(EnemyBase host) => Invoke("Possess", host);
        private void Eject(bool forced) => Invoke("Eject", forced);
        private void Invoke(string name, object value) => typeof(PossessionController)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, new[] { value });
        private static T Read<T>(object obj, string name)
        {
            for (var type = obj.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(obj);
            }
            throw new System.MissingFieldException(name);
        }
        private static void Set(object obj, string name, object value) => obj.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private static void Move(EnemyBase host, Vector3 position, Vector3 forward)
        {
            var cc = host.GetComponent<CharacterController>(); cc.enabled = false;
            host.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            cc.enabled = true; Physics.SyncTransforms();
        }
    }
}
#endif
