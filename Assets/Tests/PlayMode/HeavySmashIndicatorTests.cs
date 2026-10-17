using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class HeavySmashIndicatorTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private HeavyEnemy heavy;
        private SmashAbility smash;
        private HeavySmashIndicator indicator;
        private PossessionController possession;
        private Scene level;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            Create("Ground", new Vector3(0f, -0.5f, 0f)).AddComponent<BoxCollider>()
                .size = new Vector3(100f, 1f, 100f);
            var body = Create("Heavy", new Vector3(0f, 1.3f, 0f));
            body.transform.localScale = Vector3.one * 1.3f;
            heavy = body.AddComponent<HeavyEnemy>();
            heavy.enabled = false; // Isolate the guide from enemy locomotion.
            smash = body.AddComponent<SmashAbility>();
            indicator = body.AddComponent<HeavySmashIndicator>();

            var spirit = Create("Spirit", new Vector3(4f, 1f, 0f));
            var rb = spirit.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            spirit.AddComponent<CapsuleCollider>();
            spirit.AddComponent<SpiritController>();
            possession = spirit.AddComponent<PossessionController>();
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in objects)
                if (obj != null) Object.Destroy(obj);
            objects.Clear();
            if (level.IsValid() && level.isLoaded)
                yield return SceneManager.UnloadSceneAsync(level);
            yield return null;
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator OnlyPossessedHeavyShowsGuideAndManualEjectHidesIt()
        {
            Assert.That(indicator.IsVisible, Is.False);
            Invoke(possession, "Possess", heavy);
            Assert.That(indicator.IsVisible, Is.True);
            var guard = Create("Guard", new Vector3(20f, 1f, 0f)).AddComponent<GuardEnemy>();
            EventBus.Ejected?.Invoke(guard, false);
            Assert.That(indicator.IsVisible, Is.True, "Another enemy's ejection must not hide this guide.");
            Invoke(possession, "Eject", false);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(indicator.IsVisible, Is.False);
            Invoke(possession, "Possess", guard);
            yield return null;
            Assert.That(indicator.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ForcedEjectionHidesGuide()
        {
            Set(possession, "possessDuration", 0.05f);
            Invoke(possession, "Possess", heavy);
            Assert.That(indicator.IsVisible, Is.True);
            yield return new WaitForSeconds(0.15f);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(indicator.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator GuideUsesAttackRangeInWorldUnitsAndFollowsFacingAndPosition()
        {
            Invoke(possession, "Possess", heavy);
            var arc = heavy.transform.Find("Heavy Smash Guide/Range arc").GetComponent<LineRenderer>();
            var body = heavy.GetComponent<CharacterController>();
            Assert.That(arc.sharedMaterial, Is.Not.Null);
            Assert.That(arc.sharedMaterial.shader.isSupported, Is.True);
            Assert.That(arc.sharedMaterial.shader.name, Is.EqualTo("Possession/P3/SmashIndicator"));

            heavy.transform.SetPositionAndRotation(new Vector3(6f, 1.3f, 4f), Quaternion.Euler(0f, 90f, 0f));
            Set(smash, "smashRange", 3f);
            Physics.SyncTransforms();
            yield return null;
            Vector3 center = heavy.transform.position;
            center.y = body.bounds.min.y + 0.06f;
            Vector3 tip = arc.GetPosition(arc.positionCount / 2);
            Assert.That(Vector3.Distance(tip, center + Vector3.right * 3f), Is.LessThan(0.001f));
            for (int i = 0; i < arc.positionCount; i++)
            {
                Assert.That(Vector3.Distance(arc.GetPosition(i), center), Is.EqualTo(smash.Range).Within(0.001f));
                Assert.That(arc.GetPosition(i).y, Is.EqualTo(center.y).Within(0.001f));
            }
            Assert.That(heavy.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1),
                "The guide must not add colliders that interfere with attacks or ejection.");
        }

        [UnityTest]
        public IEnumerator CooldownDimsGuideAndReadyColorReturns()
        {
            Invoke(possession, "Possess", heavy);
            var arc = heavy.transform.Find("Heavy Smash Guide/Range arc").GetComponent<LineRenderer>();
            Color ready = arc.startColor;
            smash.TryExecute();
            yield return null;
            Assert.That(indicator.IsVisible, Is.True);
            Assert.That(arc.startColor, Is.Not.EqualTo(ready));
            yield return new WaitForSeconds(0.55f);
            Assert.That(arc.startColor, Is.EqualTo(ready));
        }

        [UnityTest]
        public IEnumerator DisableReenableAndDeathCleanUpGuide()
        {
            Invoke(possession, "Possess", heavy);
            indicator.enabled = false;
            Assert.That(indicator.IsVisible, Is.False);
            indicator.enabled = true;
            Assert.That(indicator.IsVisible, Is.True);
            heavy.TakeDamage(99);
            yield return null;
            Assert.That(indicator.IsVisible, Is.False);
            Assert.That(heavy.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator EndEventsHideGuide()
        {
            Invoke(possession, "Possess", heavy);
            EventBus.LevelCompleted?.Invoke();
            Assert.That(indicator.IsVisible, Is.False);
            EventBus.Possessed?.Invoke(heavy, PlayerState.Possessed_Heavy);
            EventBus.LevelFailed?.Invoke();
            Assert.That(indicator.IsVisible, Is.False);
            EventBus.Possessed?.Invoke(heavy, PlayerState.Possessed_Heavy);
            EventBus.SpiritDied?.Invoke();
            yield return null;
            Assert.That(indicator.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator SceneInstallAndEventBusResetWorkWithoutEditingSavedScene()
        {
            yield return SceneManager.LoadSceneAsync("Level_01", LoadSceneMode.Additive);
            level = SceneManager.GetSceneByName("Level_01");
            AssertSceneGuide();
            yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear(); // Same reset used by RestartLevel/QuitToMenu.
            yield return SceneManager.LoadSceneAsync("Level_01", LoadSceneMode.Additive);
            level = SceneManager.GetSceneByName("Level_01");
            AssertSceneGuide();

            var spawned = Create("Spawned Heavy", new Vector3(20f, 1.3f, 20f));
            var enemy = spawned.AddComponent<HeavyEnemy>();
            spawned.AddComponent<SmashAbility>();
            EventBus.Possessed?.Invoke(enemy, PlayerState.Possessed_Heavy);
            Assert.That(spawned.GetComponent<HeavySmashIndicator>(), Is.Not.Null);
            Assert.That(spawned.GetComponents<HeavySmashIndicator>().Length, Is.EqualTo(1));
            Assert.That(spawned.GetComponent<HeavySmashIndicator>().IsVisible, Is.True);
        }

        private void AssertSceneGuide()
        {
            HeavyEnemy enemy = null;
            foreach (var root in level.GetRootGameObjects())
            {
                var candidate = root.GetComponentInChildren<HeavyEnemy>(true);
                if (candidate != null) enemy = candidate;
            }
            Assert.That(enemy, Is.Not.Null);
            var guide = enemy.GetComponent<HeavySmashIndicator>();
            Assert.That(guide, Is.Not.Null, "Saved Level_01 must receive its guide automatically.");
            Assert.That(enemy.GetComponents<HeavySmashIndicator>().Length, Is.EqualTo(1));
            EventBus.Possessed?.Invoke(enemy, PlayerState.Possessed_Heavy);
            Assert.That(guide.IsVisible, Is.True);
            EventBus.Ejected?.Invoke(enemy, true);
            Assert.That(guide.IsVisible, Is.False);
        }

        private GameObject Create(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            objects.Add(obj);
            return obj;
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
