using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class PossessionLowTimeTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private EnemyBase enemy;
        private PossessionController possession;
        private PossessedBodyBlink warning;
        private Renderer bodyRenderer;
        private Renderer tool;
        private Material normal;
        private Material white;
        private Scene level;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            var ground = Create("Ground", new Vector3(0f, -0.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
            var host = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            objects.Add(host);
            host.name = "Guard";
            host.transform.position = Vector3.up;
            normal = Material(new Color(0.1f, 0.3f, 0.8f));
            white = Material(Color.white);
            bodyRenderer = host.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = normal;
            enemy = host.AddComponent<GuardEnemy>();
            enemy.enabled = false;
            var character = host.GetComponent<CharacterController>();
            character.height = 2f;
            character.radius = 0.5f;
            var accessory = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accessory.transform.SetParent(host.transform, false);
            Object.Destroy(accessory.GetComponent<Collider>());
            tool = accessory.GetComponent<Renderer>();
            tool.sharedMaterial = Material(Color.black);

            var spirit = Create("Spirit", new Vector3(3f, 1f, 0f));
            spirit.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            var collider = spirit.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            spirit.AddComponent<SpiritController>();
            possession = spirit.AddComponent<PossessionController>();
            possession.enabled = false;
            var cues = Create("Possession cues", Vector3.zero).AddComponent<PossessionCues>();
            Set(cues, "possessedMaterial", white);
            var aim = (LineRenderer)typeof(PossessionCues).GetField("aimLine",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cues);
            objects.Add(aim.gameObject);
            materials.Add(aim.sharedMaterial);
            warning = host.AddComponent<PossessedBodyBlink>();
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
            foreach (var material in materials) if (material != null) Object.Destroy(material);
            materials.Clear();
            yield return null;
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator RootBodyGetsWarningOnlyInOrangeBand()
        {
            Possess();
            foreach (float pct in new[] { 1f, 0.5f, 0.36f, 0.35f, 0.2f, 0.01f, 0f, -0.1f })
            {
                EventBus.PossessTimerUpdated?.Invoke(pct * 10f, 10f);
                yield return null;
                Assert.That(Halo.enabled, Is.EqualTo(pct > 0f && pct <= 0.35f), "Remaining fraction " + pct);
                AssertWhiteBody();
            }
            EventBus.PossessTimerUpdated?.Invoke(1f, 0f);
            yield return null;
            Assert.That(Halo.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator WhiteBodyAndToolColoursSurviveWarningAndBothEjectTypes()
        {
            Material toolMaterial = tool.sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Glossiness", 0.123f);
            bodyRenderer.SetPropertyBlock(block);
            foreach (bool forced in new[] { false, true })
            {
                Possess();
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                for (int i = 0; i < 15; i++)
                {
                    yield return null;
                    AssertWhiteBody();
                    Assert.That(tool.sharedMaterial, Is.SameAs(toolMaterial));
                    Assert.That(tool.HasPropertyBlock(), Is.False);
                    bodyRenderer.GetPropertyBlock(block);
                    Assert.That(block.GetFloat("_Glossiness"), Is.EqualTo(0.123f));
                }
                if (forced) enemy.OnForcedEject(5f); else enemy.OnEjected();
                EventBus.Ejected?.Invoke(enemy, forced);
                Assert.That(Halo.enabled, Is.False);
                Assert.That(bodyRenderer.sharedMaterial, Is.SameAs(normal));
            }
        }

        [UnityTest]
        public IEnumerator PulseChangesGraduallyWithoutDisappearingOrTintingBody()
        {
            Possess();
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            float min = 1f, max = 0f;
            for (float elapsed = 0f; elapsed < 1f; elapsed += Time.deltaTime)
            {
                yield return null;
                float alpha = Halo.startColor.a;
                min = Mathf.Min(min, alpha);
                max = Mathf.Max(max, alpha);
                Assert.That(Halo.enabled, Is.True);
                // LineRenderer quantizes vertex colours to 8 bits.
                Assert.That(alpha, Is.InRange(0.54f, 1f));
                AssertWhiteBody();
            }
            Assert.That(max - min, Is.GreaterThan(0.25f));
        }

        [UnityTest]
        public IEnumerator WarningFollowsScaledBodyWithoutAddingColliders()
        {
            int colliders = enemy.GetComponentsInChildren<Collider>().Length;
            Possess();
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            enemy.transform.localScale = Vector3.one * 1.3f;
            enemy.transform.position = new Vector3(6f, 1.3f, 4f);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(Halo.sharedMaterial, Is.Not.Null);
            Assert.That(Halo.sharedMaterial.shader.isSupported, Is.True);
            Vector3 center = enemy.transform.position;
            center.y = enemy.GetComponent<CharacterController>().bounds.min.y + 0.07f;
            Vector3 average = Vector3.zero;
            for (int i = 0; i < Halo.positionCount; i++) average += Halo.GetPosition(i);
            average /= Halo.positionCount;
            Assert.That(Vector3.Distance(average, center), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(Halo.GetPosition(0), center), Is.GreaterThan(0.65f));
            Assert.That(enemy.GetComponentsInChildren<Collider>().Length, Is.EqualTo(colliders));
        }

        [UnityTest]
        public IEnumerator OtherEnemyEjectionDoesNotHideCurrentWarning()
        {
            Possess();
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return null;
            var other = Create("Other Guard", Vector3.right * 20f).AddComponent<GuardEnemy>();
            EventBus.Ejected?.Invoke(other, true);
            Assert.That(Halo.enabled, Is.True);
            EventBus.Ejected?.Invoke(enemy, false);
            Assert.That(Halo.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingAndLevelEndEventsHideWarningImmediately()
        {
            foreach (System.Action end in new System.Action[] {
                () => EventBus.LevelCompleted?.Invoke(), () => EventBus.LevelFailed?.Invoke(),
                () => EventBus.SpiritDied?.Invoke(), () => EventBus.GameComplete?.Invoke(),
                () => warning.enabled = false })
            {
                Possess();
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                yield return null;
                Assert.That(Halo.enabled, Is.True);
                end();
                Assert.That(Halo.enabled, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ReenableDuringPossessionResumesAtCurrentTime()
        {
            Invoke(possession, "Possess", enemy);
            Set(possession, "possessTimer", 2f);
            warning.enabled = false;
            warning.enabled = true;
            yield return null;
            Assert.That(Halo.enabled, Is.True);
            AssertWhiteBody();
        }

        [UnityTest]
        public IEnumerator InstallerSurvivesSceneChangesEventBusResetAndSpawnedBodies()
        {
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            yield return null;
            foreach (string name in new[] { "Level_01", "Level_02", "Level_03", "Level_01" })
            {
                EventBus.Clear();
                yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                level = SceneManager.GetSceneByName(name);
                var enemies = new List<EnemyBase>();
                foreach (var root in level.GetRootGameObjects()) enemies.AddRange(root.GetComponentsInChildren<EnemyBase>(true));
                Assert.That(enemies.Count, Is.GreaterThan(0));
                foreach (var host in enemies)
                    Assert.That(host.GetComponents<PossessedBodyBlink>(), Has.Length.EqualTo(1), name + "/" + host.name);
                var spawned = Create("Spawned Guard", Vector3.right * 100f).AddComponent<GuardEnemy>();
                spawned.OnPossessed();
                EventBus.Possessed?.Invoke(spawned, PlayerState.Possessed_Guard);
                Assert.That(spawned.GetComponents<PossessedBodyBlink>(), Has.Length.EqualTo(1));
                Object.Destroy(spawned.gameObject);
                yield return SceneManager.UnloadSceneAsync(level);
                level = default;
            }
        }

        private LineRenderer Halo => enemy.transform.Find("Possession Low Time Halo").GetComponent<LineRenderer>();

        private void AssertWhiteBody()
        {
            Assert.That(bodyRenderer.sharedMaterial, Is.SameAs(white));
            var block = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(block);
            Assert.That(block.HasColor("_Color"), Is.False, "Warning must leave possession colour to PossessionCues.");
        }

        private void Possess()
        {
            enemy.OnPossessed();
            EventBus.Possessed?.Invoke(enemy, PlayerState.Possessed_Guard);
        }

        private Material Material(Color color)
        {
            var material = new Material(Shader.Find("Standard")) { color = color };
            materials.Add(material);
            return material;
        }

        private GameObject Create(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
