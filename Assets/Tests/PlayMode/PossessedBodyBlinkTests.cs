using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Tests
{
    public class PossessedBodyBlinkTests
    {
        private Scene level;
        private PossessionController player;
        private PossessedBodyBlink blink;
        private EnemyBase host;
        private Renderer body;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            yield return Load("Level_01");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator InstallsOnceInEverySavedLevelAndTutorial()
        {
            foreach (string name in CampaignLevels.SceneNames.Concat(new[] { "Level_01", "Tutorial" }).Distinct())
            {
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                yield return Load(name);
                Assert.That(player.GetComponents<PossessedBodyBlink>(), Has.Length.EqualTo(1), name);
            }
        }

        [UnityTest]
        public IEnumerator IdleAndHealthyPossessionNeverBlink()
        {
            EventBus.PossessTimerUpdated?.Invoke(1f, 10f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.forceRenderingOff, Is.False);
            Possess(host);
            foreach (float fraction in new[] { 1f, 0.8f, 0.66f })
            {
                EventBus.PossessTimerUpdated?.Invoke(fraction * 10f, 10f);
                float until = Time.time + 0.3f;
                while (Time.time < until)
                {
                    yield return null;
                    Assert.That(body.forceRenderingOff, Is.False);
                }
            }
        }

        [UnityTest]
        public IEnumerator RootBodyBlinksFasterNearTimeoutWithoutChangingItsMaterialsOrEquipment()
        {
            foreach (var enemy in new EnemyBase[] { Find<GuardEnemy>(), Find<HeavyEnemy>() })
            {
                host = enemy;
                body = host.GetComponent<Renderer>();
                Material normal = body.sharedMaterial;
                Possess(host);
                var block = new MaterialPropertyBlock();
                block.SetFloat("_Glossiness", 0.123f);
                body.SetPropertyBlock(block);
                Material white = body.sharedMaterial;
                var parts = host.GetComponentsInChildren<Renderer>().Where(r => r != body).ToArray();
                var materials = parts.Select(r => r.sharedMaterial).ToArray();
                var hidden = parts.Select(r => r.forceRenderingOff).ToArray();
                int slow = 0, fast = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    EventBus.PossessTimerUpdated?.Invoke(pass == 0 ? 6.5f : 0.1f, 10f);
                    int transitions = 0;
                    bool previous = body.forceRenderingOff;
                    float until = Time.time + 1.1f;
                    while (Time.time < until)
                    {
                        yield return null;
                        if (body.forceRenderingOff != previous) transitions++;
                        previous = body.forceRenderingOff;
                        Assert.That(body.enabled, Is.True);
                        Assert.That(body.sharedMaterial, Is.SameAs(white));
                        Assert.That(white.color, Is.EqualTo(Color.white));
                        body.GetPropertyBlock(block);
                        Assert.That(block.HasColor("_Color"), Is.False);
                        Assert.That(block.GetFloat("_Glossiness"), Is.EqualTo(0.123f));
                        for (int i = 0; i < parts.Length; i++)
                        {
                            Assert.That(parts[i].sharedMaterial, Is.SameAs(materials[i]));
                            Assert.That(parts[i].forceRenderingOff, Is.EqualTo(hidden[i]));
                        }
                    }
                    if (pass == 0) slow = transitions;
                    else fast = transitions;
                }
                Assert.That(slow, Is.GreaterThanOrEqualTo(2), host.name);
                Assert.That(fast, Is.GreaterThan(slow + 3), host.name);
                Assert.That(player.TryEject(false), Is.True);
                Assert.That(body.forceRenderingOff, Is.False);
                Assert.That(body.sharedMaterial, Is.SameAs(normal));
            }
        }

        [UnityTest]
        public IEnumerator RealCountdownWarnsWithMoreThanHalfTheTimeLeftForBothBodies()
        {
            Time.captureFramerate = 60;
            foreach (var enemy in new EnemyBase[] { Find<GuardEnemy>(), Find<HeavyEnemy>() })
            {
                host = enemy;
                body = host.GetComponent<Renderer>();
                player.enabled = false;
                Possess(host);
                player.enabled = true;
                float firstBlinkRemaining = -1f;
                int frames = Mathf.CeilToInt((player.PossessDuration + 0.5f) * 60f);
                for (int frame = 0; frame < frames; frame++)
                {
                    yield return null;
                    if (firstBlinkRemaining < 0f && body.forceRenderingOff)
                        firstBlinkRemaining = player.PossessTimer;
                }
                Assert.That(firstBlinkRemaining / player.PossessDuration, Is.InRange(0.55f, 0.66f),
                    enemy.name + " needs a visible warning before half the possession time remains.");
                Assert.That(player.PossessedEnemy, Is.Null);
                Assert.That(body.forceRenderingOff, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator EarlyMiddleAndUrgentWarningsGetFasterWhileKeepingTheBodyTrackable()
        {
            Time.captureFramerate = 60;
            Possess(host);
            int previousPulses = 0;
            foreach (float remaining in new[] { 6.5f, 3.25f, 0.2f })
            {
                EventBus.PossessTimerUpdated?.Invoke(remaining, 10f);
                int pulses = 0, visibleFrames = 0;
                bool wasHidden = body.forceRenderingOff;
                for (int frame = 0; frame < 120; frame++)
                {
                    yield return null;
                    bool hidden = body.forceRenderingOff;
                    if (hidden && !wasHidden) pulses++;
                    if (!hidden) visibleFrames++;
                    wasHidden = hidden;
                }
                Assert.That(pulses, Is.GreaterThan(previousPulses), "Each warning band must be more urgent.");
                Assert.That(visibleFrames, Is.GreaterThanOrEqualTo(remaining == 6.5f ? 90 : 65),
                    "Brief blinks must leave the white body easy to follow.");
                previousPulses = pulses;
            }
        }

        [UnityTest]
        public IEnumerator ManualAndForcedEjectRestoreAHiddenBodyImmediately()
        {
            foreach (bool forced in new[] { false, true })
            {
                Possess(host);
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                yield return WaitForHiddenBody();
                Assert.That(player.TryEject(forced), Is.True);
                Assert.That(body.forceRenderingOff, Is.False);
                yield return null;
                Assert.That(body.forceRenderingOff, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator InvalidExpiredAndReplenishedTimersRestoreVisibility()
        {
            Possess(host);
            foreach (var timer in new[] { new Vector2(0f, 10f), new Vector2(-1f, 10f),
                new Vector2(1f, 0f), new Vector2(1f, -10f), new Vector2(10f, 10f) })
            {
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                yield return WaitForHiddenBody();
                EventBus.PossessTimerUpdated?.Invoke(timer.x, timer.y);
                Assert.That(body.forceRenderingOff, Is.False, timer.ToString());
                yield return null;
                Assert.That(body.forceRenderingOff, Is.False, timer.ToString());
            }
        }

        [UnityTest]
        public IEnumerator PauseFreezesBlinkAndResumeContinues()
        {
            Possess(host);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return WaitForHiddenBody();
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(body.forceRenderingOff, Is.True);
            Time.timeScale = 1f;
            float deadline = Time.time + 1f;
            while (body.forceRenderingOff && Time.time < deadline) yield return null;
            Assert.That(body.forceRenderingOff, Is.False);
        }

        [UnityTest]
        public IEnumerator DisableRestoresBodyAndReenableReadsTheLiveCountdown()
        {
            Possess(host);
            Set(player, "possessTimer", 2f);
            EventBus.PossessTimerUpdated?.Invoke(2f, player.PossessDuration);
            yield return WaitForHiddenBody();
            blink.enabled = false;
            Assert.That(body.forceRenderingOff, Is.False);
            blink.enabled = true;
            yield return WaitForHiddenBody();
            Object.Destroy(blink);
            yield return null;
            Assert.That(body.forceRenderingOff, Is.False);
        }

        [UnityTest]
        public IEnumerator EveryEndEventClearsBlinkWithoutReappearingOnReenable()
        {
            foreach (Action end in new Action[] { () => EventBus.LevelCompleted?.Invoke(),
                () => EventBus.LevelFailed?.Invoke(), () => EventBus.SpiritDied?.Invoke(),
                () => EventBus.GameComplete?.Invoke() })
            {
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                yield return Load("Level_01");
                Possess(host);
                Set(player, "possessTimer", 2f);
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                yield return WaitForHiddenBody();
                end();
                Assert.That(body.forceRenderingOff, Is.False);
                blink.enabled = false;
                blink.enabled = true;
                EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
                yield return null;
                Assert.That(body.forceRenderingOff, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator HostDisableAndDeathRestoreVisibility()
        {
            Possess(host);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return WaitForHiddenBody();
            host.enabled = false;
            yield return null;
            Assert.That(body.forceRenderingOff, Is.False);
            host.enabled = true;
            EventBus.Possessed?.Invoke(host, player.CurrentState);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return WaitForHiddenBody();
            host.TakeDamage(host.CurrentHP);
            yield return null;
            Assert.That(body == null || !body.forceRenderingOff, Is.True);
        }

        [UnityTest]
        public IEnumerator UnrelatedEnemyEventsDoNotClearTheCurrentBlink()
        {
            Possess(host);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return WaitForHiddenBody();
            var other = Find<HeavyEnemy>();
            EventBus.Ejected?.Invoke(other, true);
            EventBus.Possessed?.Invoke(other, PlayerState.Possessed_Heavy);
            Assert.That(body.forceRenderingOff, Is.True);
        }

        [UnityTest]
        public IEnumerator DisabledAndPreviouslyHiddenRenderersKeepTheirOriginalState()
        {
            body.enabled = false;
            body.forceRenderingOff = true;
            Possess(host);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return new WaitForSeconds(0.3f);
            Assert.That(body.enabled, Is.False);
            Assert.That(body.forceRenderingOff, Is.True);
            Assert.That(player.TryEject(false), Is.True);
            Assert.That(body.enabled, Is.False);
            Assert.That(body.forceRenderingOff, Is.True);
        }

        [UnityTest]
        public IEnumerator RuntimeSpawnedHostWithAChildMeshAlsoBlinks()
        {
            var go = new GameObject("Runtime host");
            SceneManager.MoveGameObjectToScene(go, level);
            go.transform.position = new Vector3(100f, 1f, 100f);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.transform.SetParent(go.transform, false);
            Object.Destroy(mesh.GetComponent<Collider>());
            host = go.AddComponent<GuardEnemy>();
            host.MoveSpeed = 0f;
            body = mesh.GetComponent<Renderer>();
            Set(player, "possessedEnemy", host);
            host.OnPossessed();
            EventBus.Possessed?.Invoke(host, PlayerState.Possessed_Guard);
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return WaitForHiddenBody();
            EventBus.Ejected?.Invoke(host, false);
            Assert.That(body.forceRenderingOff, Is.False);
        }

        [UnityTest]
        public IEnumerator RealTimerExpiryRestoresBodyAndKeepsTheGroundRingLifecycle()
        {
            Possess(host);
            var hud = Find<HUDManager>();
            var group = (GameObject)typeof(HUDManager).GetField("possessTimerGroup",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            Assert.That(group.GetComponent<PossessionTimerGraphic>(), Is.Not.Null);
            Set(player, "possessTimer", 0.4f);
            player.enabled = true;
            yield return WaitForHiddenBody();
            float deadline = Time.time + 2f;
            while (player.PossessedEnemy != null && Time.time < deadline) yield return null;
            Assert.That(player.PossessedEnemy, Is.Null);
            Assert.That(body.forceRenderingOff, Is.False);
            Assert.That(group.activeSelf, Is.False);
        }

        private IEnumerator Load(string name)
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            level = SceneManager.GetSceneByName(name);
            player = Find<PossessionController>();
            player.enabled = false;
            foreach (var enemy in FindAll<EnemyBase>()) enemy.MoveSpeed = 0f;
            foreach (var attack in FindAll<EnemyContactDamage>()) attack.enabled = false;
            host = Find<GuardEnemy>();
            body = host.GetComponent<Renderer>();
            blink = player.GetComponent<PossessedBodyBlink>();
            Assert.That(blink, Is.Not.Null, "The scene installer must attach the warning at runtime.");
            yield return null;
        }

        private void Possess(EnemyBase enemy)
        {
            Set(player, "ejectCooldownTimer", 0f);
            var charge = player.GetComponent<PossessionCharge>();
            if (charge != null && !charge.HasCharge) charge.TryCollect();
            // Reset post-eject hostility when a test immediately reuses the same host.
            Set(typeof(EnemyBase), enemy, "isHostile", false);
            var spirit = player.GetComponent<SpiritController>();
            foreach (Vector3 direction in new[] { -enemy.transform.forward, Vector3.right,
                Vector3.back, Vector3.left, Vector3.forward })
            {
                Vector3 at = enemy.transform.position + direction * 1.8f;
                at.y = 1.15f;
                if (Physics.OverlapCapsule(at + Vector3.up * 0.45f, at - Vector3.up * 0.45f,
                    0.5f, 1 << 0, QueryTriggerInteraction.Ignore)
                    .Any(c => !c.transform.IsChildOf(spirit.transform))) continue;
                spirit.RestoreAt(at);
                Physics.SyncTransforms();
                if (!enemy.CanBePossessedFrom(spirit.transform)) continue;
                typeof(PossessionController).GetMethod("Possess", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(player, new object[] { enemy });
                Assert.That(player.PossessedEnemy, Is.SameAs(enemy));
                return;
            }
            Assert.Fail("No clear possession approach for " + enemy.name);
        }

        private IEnumerator WaitForHiddenBody()
        {
            float deadline = Time.time + 1f;
            while (!body.forceRenderingOff && Time.time < deadline) yield return null;
            Assert.That(body.forceRenderingOff, Is.True, "Low possession time must visibly blink the body.");
        }

        private T Find<T>() where T : Component => FindAll<T>().First();
        private T[] FindAll<T>() where T : Component => level.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(object target, string name, object value) => Set(target.GetType(), target, name, value);
        private static void Set(Type type, object target, string name, object value) => type
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
