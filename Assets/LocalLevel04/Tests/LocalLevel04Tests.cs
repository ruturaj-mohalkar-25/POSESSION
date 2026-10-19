#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.LocalLevel04.Tests
{
    public class LocalLevel04Tests
    {
        private const string Path = "Assets/LocalLevel04/Level_04_Local.unity";
        private LocalLevel04Challenge level;
        private SpiritController spirit;
        private PossessionController player;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1; EventBus.Clear();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            level = Object.FindFirstObjectByType<LocalLevel04Challenge>();
            spirit = Object.FindFirstObjectByType<SpiritController>();
            player = spirit.GetComponent<PossessionController>();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneIsIsolatedAndHasAllRequiredRoles()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == Path), Is.False);
            Assert.That(level.hosts.OfType<HeavyEnemy>().Count(), Is.EqualTo(2));
            Assert.That(level.hosts.OfType<GuardEnemy>().Count(), Is.EqualTo(3));
            Assert.That(level.gates.Length, Is.EqualTo(4));
            Assert.That(player.PossessDuration, Is.EqualTo(10));
            Assert.That(Camera.main.orthographic, Is.False);
            Assert.That(Camera.main.fieldOfView, Is.EqualTo(40));
            Assert.That(GameManager.Instance.LevelTimer, Is.InRange(118f, 120f));
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach (var mat in renderer.sharedMaterials) Assert.That(mat, Is.Not.Null, renderer.name);
            Assert.That(level.TryFinish(spirit), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OrderedPuzzleRouteWorksWithProjectilesAndSmash()
        {
            foreach (var host in level.hosts) host.enabled = false;
            player.enabled = false;
            var firstHeavy = level.hosts.OfType<HeavyEnemy>().First();
            // Shooting at the Heavy gate cannot damage it or substitute for Heavy possession.
            Assert.That(level.gates[1].GetComponent<IDamageable>(), Is.Null);
            Assert.That(level.gates[1].TrySmash(firstHeavy), Is.False);
            yield return FireTarget(level.targets[0]);
            Assert.That(level.gates[0].IsOpen, Is.True);
            Move(firstHeavy, new Vector3(33.2f, 1.45f, 20), Vector3.left);
            Invoke("Possess", firstHeavy);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Possessed_Heavy));
            firstHeavy.GetComponent<Ability>().TryExecute();
            yield return null;
            Assert.That(level.gates[1].IsOpen, Is.True);
            Assert.That(level.gates[2].IsOpen, Is.False);
            Invoke("Eject", false);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(spirit.transform.position.y, Is.InRange(.9f, 1.5f));
            Assert.That(level.navigation.FindPath(new Vector3(35, 0, 20), new Vector3(20, 0, 20)).Count, Is.GreaterThan(0));
            yield return FireTarget(level.targets[1]);
            Assert.That(level.gates[2].IsOpen, Is.True);
            var lastHeavy = level.hosts.OfType<HeavyEnemy>().Last();
            Move(lastHeavy, new Vector3(53, 1.45f, 26.8f), Vector3.forward);
            Invoke("Possess", lastHeavy);
            lastHeavy.GetComponent<Ability>().TryExecute();
            yield return null;
            Assert.That(level.gates[3].IsOpen, Is.True);
            Assert.That(level.TryFinish(spirit), Is.False, "A possessed body must not finish the level.");
            Invoke("Eject", true);
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(lastHeavy.CanBePossessed, Is.False, "Forced ejection must make the Heavy hostile.");
            spirit.RestoreAt(new Vector3(53, 1.15f, 36));
            Physics.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + .5f;
            while (GameManager.Instance.CurrentState == LevelState.InProgress && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
        }

        [UnityTest]
        public IEnumerator ClosedCacheAndLongEastRouteCannotBeSkipped()
        {
            var nav = level.navigation;
            Assert.That(nav.FindPath(new Vector3(35, 0, 20), new Vector3(20, 0, 20)), Is.Empty);
            var route = nav.FindPath(new Vector3(45, 0, 5), new Vector3(61, 0, 23));
            Assert.That(route.Count, Is.GreaterThan(0));
            Assert.That(route.Any(p => p.x > 60), Is.True, "Route must go around the east baffle.");
            float length = 0; Vector3 previous = new Vector3(45, 2, 5);
            foreach (var p in route) { Assert.That(nav.Clear(previous, p), Is.True); length += Vector3.Distance(previous, p); previous = p; }
            Assert.That(length, Is.GreaterThan(30));
            foreach (var gate in level.gates) Assert.That(gate.GetComponent<Collider>().bounds.size.y, Is.GreaterThan(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AiPatrolsButStopsDuringPossessionAndRestartWorks()
        {
            var host = level.hosts[0];
            Vector3 before = host.transform.position;
            yield return new WaitForSeconds(.6f);
            Assert.That(Vector3.Distance(host.transform.position, before), Is.GreaterThan(.15f));
            Invoke("Possess", host);
            before = host.transform.position;
            yield return new WaitForSeconds(.3f);
            Vector3 delta = host.transform.position - before; delta.y = 0;
            Assert.That(delta.magnitude, Is.LessThan(.05f));
            EventBus.LevelFailed?.Invoke();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Failed));
            level.Restart();
            yield return null; yield return null;
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            Assert.That(GameManager.Instance.LevelTimer, Is.GreaterThan(118));
        }

        [UnityTest]
        public IEnumerator BothHeavyApproachesFitInsidePossessionWindow()
        {
            var heavies = level.hosts.OfType<HeavyEnemy>().ToArray();
            Vector3[] attacks = { new Vector3(33.2f, 2, 20), new Vector3(53, 2, 26.8f) };
            for (int i = 0; i < heavies.Length; i++)
            {
                var route = level.navigation.FindPath(heavies[i].transform.position, attacks[i]);
                Assert.That(route, Is.Not.Empty);
                Vector3 previous = heavies[i].transform.position; previous.y = 2;
                float length = 0;
                foreach (var point in route) { length += Vector3.Distance(previous, point); previous = point; }
                Assert.That(length / 2.2f, Is.LessThan(player.PossessDuration - 2),
                    "Heavy approach needs at least two seconds for aiming and smashing.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudTracksHostAndDamageWithoutDuplicateCanvases()
        {
            var hud = Object.FindFirstObjectByType<LocalLevel04HUD>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<LocalLevel04SignalMap>().canvasRenderer, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<LocalLevel04Ring>().canvasRenderer, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Count(c => c.name == "Local04_SpectralHUD"), Is.EqualTo(1));
            Invoke("Possess", level.hosts.OfType<HeavyEnemy>().First());
            yield return null;
            Assert.That(hud.HostSwaps, Is.EqualTo(1));
            var labels = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
            Assert.That(labels.Any(t => t.text == "HEAVY"), Is.True);
            Assert.That(labels.Any(t => t.text == "KINETIC SMASH"), Is.True);
            Invoke("Eject", false);
            spirit.TakeDamage(1);
            yield return null;
            Assert.That(labels.Any(t => t != null && t.text == "INTEGRITY  2/3"), Is.True);
        }

        [UnityTest]
        public IEnumerator PauseStopsClockAndGameplayThenRestoresControls()
        {
            var hud = Object.FindFirstObjectByType<LocalLevel04HUD>();
            float before = GameManager.Instance.LevelTimer;
            hud.TogglePause();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(GameManager.Instance.LevelTimer, Is.EqualTo(before));
            Assert.That(player.enabled, Is.False);
            Assert.That(spirit.enabled, Is.False);
            Assert.That(level.hosts.All(h => !h.enabled), Is.True);
            hud.TogglePause();
            Assert.That(player.enabled && spirit.enabled, Is.True);
            yield return new WaitForSeconds(.08f);
            Assert.That(GameManager.Instance.LevelTimer, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator EjectionBesideCacheAndBaffleStaysGroundedAndClear()
        {
            foreach (var host in level.hosts) host.enabled = false;
            player.enabled = false;
            var heavy = level.hosts.OfType<HeavyEnemy>().First();
            Vector3[] positions = { new Vector3(32.3f, 1.45f, 16), new Vector3(35, 1.45f, 20), new Vector3(61, 1.45f, 18) };
            for (int i = 0; i < positions.Length; i++)
            {
                Move(heavy, positions[i], Vector3.left);
                Invoke("Possess", heavy);
                Invoke("Eject", i == positions.Length - 1);
                Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Spirit));
                Physics.SyncTransforms();
                Assert.That(spirit.transform.position.y, Is.InRange(.9f, 1.5f));
                var capsule = spirit.GetComponent<CapsuleCollider>();
                foreach (var wall in level.navigation.barriers.Where(w => w.enabled))
                {
                    Assert.That(Physics.ComputePenetration(capsule, spirit.transform.position, spirit.transform.rotation,
                        wall, wall.transform.position, wall.transform.rotation, out _, out _), Is.False,
                        "Ejection must not intersect " + wall.name);
                }
            }
            yield return null;
        }

        private IEnumerator FireTarget(Target target)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            Vector3 origin = target.transform.position - Vector3.forward * 3;
            var projectile = LocalLevel04Shot.FireProjectile(prefab, origin, Vector3.forward, null);
            var probe = projectile.AddComponent<CollisionProbe>();
            Vector3 lastPosition = origin;
            float until = Time.time + 1f;
            string collided = "none";
            while (Time.time < until && !target.IsHit)
            {
                if (projectile != null) { lastPosition = projectile.transform.position; collided = probe.lastHit; }
                yield return null;
            }
            collided = probe.lastHit;
            Assert.That(target.IsHit, Is.True, "Actual projectile must hit " + target.name +
                "; last position " + lastPosition + "; collision " + collided);
            yield return null;
        }
        private void Invoke(string name, object value) => typeof(PossessionController)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, new[] { value });
        private static void Move(EnemyBase host, Vector3 position, Vector3 forward)
        {
            var cc = host.GetComponent<CharacterController>(); cc.enabled = false;
            host.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            cc.enabled = true; Physics.SyncTransforms();
        }
    }

    public sealed class CollisionProbe : MonoBehaviour
    {
        public string lastHit = "none";
        private void OnTriggerEnter(Collider other) => lastHit = other.name;
    }
}
#endif
