using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Tests
{
    public class PossessionTimerTests
    {
        private Scene level;
        private HUDManager hud;
        private PossessionController possession;
        private EnemyBase enemy;
        private RectTransform group;
        private PossessionTimerGraphic ring;

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
            Time.timeScale = 1f;
            if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator CountdownIsTextFreeGreenYellowRedAndArcShrinks()
        {
            Possess();
            int previousVertices = int.MaxValue;
            foreach (float fraction in new[] { 1f, 0.5f, 0.05f })
            {
                EventBus.PossessTimerUpdated?.Invoke(fraction * 10f, 10f);
                yield return null;
                Assert.That(group.gameObject.activeInHierarchy, Is.True);
                Assert.That(ring.Fraction, Is.EqualTo(fraction).Within(0.001f));
                Assert.That(ring.canvasRenderer, Is.Not.Null);
                Assert.That(group.rect.width, Is.EqualTo(group.rect.height));
                Assert.That(group.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(group.GetComponentsInChildren<Text>().Length, Is.Zero);
                Assert.That(group.GetComponentsInChildren<Image>().Length, Is.Zero);
                int vertices = MeshVertices();
                Assert.That(vertices, Is.GreaterThan(0).And.LessThan(previousVertices));
                previousVertices = vertices;
                if (fraction == 1f) Assert.That(ring.color.g, Is.GreaterThan(ring.color.r));
                else if (fraction == 0.5f)
                    Assert.That(ring.color.r, Is.GreaterThan(0.8f));
                else Assert.That(ring.color.r, Is.GreaterThan(ring.color.g));
            }
        }

        [UnityTest]
        public IEnumerator TimerStaysSteadyWhenPausedWithoutFlashing()
        {
            Possess();
            EventBus.PossessTimerUpdated?.Invoke(1f, 10f);
            yield return null;
            Color color = ring.color;
            Vector3 scale = group.localScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(group.gameObject.activeInHierarchy, Is.True);
            Assert.That(ring.color, Is.EqualTo(color));
            Assert.That(ring.color.a, Is.EqualTo(1f));
            Assert.That(group.localScale, Is.EqualTo(scale));
            Assert.That(ring.Fraction, Is.EqualTo(0.1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator WhiteBodyAndEquipmentSurviveCountdownAndBothEjectTypes()
        {
            Renderer renderer = enemy.GetComponent<Renderer>();
            Material normal = renderer.sharedMaterial;
            Renderer[] accessories = enemy.GetComponentsInChildren<Renderer>().Where(r => r != renderer).ToArray();
            Material[] materials = accessories.Select(r => r.sharedMaterial).ToArray();
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Glossiness", 0.123f);
            renderer.SetPropertyBlock(block);
            foreach (bool forced in new[] { false, true })
            {
                Possess();
                foreach (float fraction in new[] { 1f, 0.5f, 0.1f })
                {
                    EventBus.PossessTimerUpdated?.Invoke(fraction * 10f, 10f);
                    yield return null;
                    Assert.That(renderer.sharedMaterial.color, Is.EqualTo(Color.white));
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.HasColor("_Color"), Is.False);
                    Assert.That(block.GetFloat("_Glossiness"), Is.EqualTo(0.123f));
                    for (int i = 0; i < accessories.Length; i++)
                        Assert.That(accessories[i].sharedMaterial, Is.SameAs(materials[i]));
                }
                Assert.That(possession.TryEject(forced), Is.True);
                Assert.That(group.gameObject.activeSelf, Is.False);
                Assert.That(renderer.sharedMaterial, Is.SameAs(normal));
            }
        }

        [UnityTest]
        public IEnumerator TimerFollowsBelowGuardAndHeavyWithoutTouchingColliders()
        {
            foreach (EnemyBase host in new EnemyBase[] {
                FindAll<GuardEnemy>().First(), FindAll<HeavyEnemy>().First() })
            {
                enemy = host;
                int colliders = enemy.GetComponentsInChildren<Collider>().Length;
                Possess();
                yield return null;
                AssertBelowFeet();
                enemy.GetComponent<CharacterController>().Move(Vector3.right * 0.3f);
                Physics.SyncTransforms();
                yield return null;
                AssertBelowFeet();
                Assert.That(enemy.GetComponentsInChildren<Collider>().Length, Is.EqualTo(colliders));
                Assert.That(possession.TryEject(false), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ExpiredOrInvalidDurationsHideTimerAndNextPossessionResetsIt()
        {
            Possess();
            foreach (float time in new[] { 0f, -1f })
            {
                EventBus.PossessTimerUpdated?.Invoke(time, 10f);
                yield return null;
                Assert.That(group.gameObject.activeSelf, Is.False);
            }
            EventBus.PossessTimerUpdated?.Invoke(1f, 0f);
            yield return null;
            Assert.That(group.gameObject.activeSelf, Is.False);
            Assert.That(possession.TryEject(false), Is.True);
            Possess();
            yield return null;
            Assert.That(group.gameObject.activeInHierarchy, Is.True);
            Assert.That(ring.Fraction, Is.EqualTo(1f));
            Assert.That(ring.color.g, Is.GreaterThan(ring.color.r));
        }

        [UnityTest]
        public IEnumerator LevelEndAndDeathHideTimerImmediately()
        {
            foreach (System.Action end in new System.Action[] {
                () => EventBus.LevelCompleted?.Invoke(), () => EventBus.LevelFailed?.Invoke(),
                () => EventBus.SpiritDied?.Invoke(), () => EventBus.GameComplete?.Invoke() })
            {
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                yield return Load("Level_01");
                Possess();
                yield return null;
                Assert.That(group.gameObject.activeInHierarchy, Is.True);
                end();
                Assert.That(group.gameObject.activeSelf, Is.False);
                Assert.That(possession.TryEject(false), Is.True);
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator AllSavedLevelsAndTutorialReuseOneCircularTimer()
        {
            foreach (string name in new[] { "Level_01", "Level_02", "Level_03", "Tutorial", "Level_01" })
            {
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                yield return Load(name);
                Assert.That(group.GetComponents<PossessionTimerGraphic>(), Has.Length.EqualTo(1));
                Possess();
                EventBus.PossessTimerUpdated?.Invoke(5f, 10f);
                yield return null;
                Assert.That(group.gameObject.activeInHierarchy, Is.True, name);
                Assert.That(group.GetComponentsInChildren<Text>(), Is.Empty, name);
                Assert.That(group.GetComponentsInChildren<Image>(), Is.Empty, name);
                AssertBelowFeet();
                Assert.That(possession.TryEject(false), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator DisablingHudHidesTimerAndReenableAcceptsCurrentCountdown()
        {
            Possess();
            yield return null;
            Assert.That(group.gameObject.activeInHierarchy, Is.True);
            hud.enabled = false;
            Assert.That(group.gameObject.activeSelf, Is.False);
            hud.enabled = true;
            EventBus.PossessTimerUpdated?.Invoke(2f, 10f);
            yield return null;
            Assert.That(group.gameObject.activeInHierarchy, Is.True);
            Assert.That(ring.Fraction, Is.EqualTo(0.2f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CameraCanvasAlsoKeepsCircleBelowVisibleBody()
        {
            Canvas canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            Possess();
            yield return null;
            yield return null;
            AssertBelowFeet();
        }

        [UnityTest]
        public IEnumerator PausedHudReenableRestoresTimerWithoutAnotherEvent()
        {
            Possess();
            Set(possession, "possessTimer", 2f);
            EventBus.PossessTimerUpdated?.Invoke(2f, possession.PossessDuration);
            Time.timeScale = 0f;
            hud.enabled = false;
            Assert.That(group.gameObject.activeSelf, Is.False);
            hud.enabled = true;
            yield return null;
            Assert.That(group.gameObject.activeInHierarchy, Is.True);
            Assert.That(ring.Fraction, Is.EqualTo(2f / possession.PossessDuration).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HudDisabledAcrossLevelEndDoesNotResurrectTimerOrAmmo()
        {
            foreach (System.Action end in new System.Action[] { () => EventBus.LevelCompleted?.Invoke(),
                () => EventBus.LevelFailed?.Invoke(), () => EventBus.SpiritDied?.Invoke() })
            {
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                yield return Load("Level_01");
                Possess();
                hud.enabled = false;
                end();
                Assert.That(GameManager.Instance.CurrentState, Is.Not.EqualTo(LevelState.InProgress));
                hud.enabled = true;
                EventBus.PossessTimerUpdated?.Invoke(possession.PossessTimer, possession.PossessDuration);
                yield return null;
                Assert.That(group.gameObject.activeSelf, Is.False);
                Assert.That(Read<AmmoDisplay>(hud, "ammoDisplay").gameObject.activeSelf, Is.False);
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator CueDisableRestoresIdentityAndReenableUsesCurrentPossession()
        {
            var cues = FindAll<PossessionCues>().First();
            Renderer body = enemy.GetComponent<Renderer>();
            Material normal = body.sharedMaterial;
            Possess();
            yield return null;
            cues.enabled = false;
            Assert.That(body.sharedMaterial, Is.SameAs(normal));
            Assert.That(Read<Text>(cues, "hintText").gameObject.activeSelf, Is.False);
            Assert.That(Read<LineRenderer>(cues, "aimLine").enabled, Is.False);
            cues.enabled = true;
            yield return null;
            Assert.That(body.sharedMaterial.color, Is.EqualTo(Color.white));
            Assert.That(cues.GetComponentsInChildren<LineRenderer>(), Has.Length.EqualTo(1));
            cues.enabled = false;
            Assert.That(possession.TryEject(false), Is.True);
            cues.enabled = true;
            yield return null;
            Assert.That(body.sharedMaterial, Is.SameAs(normal));
            Assert.That(Read<Text>(cues, "hintText").gameObject.activeSelf, Is.False);
            Assert.That(Read<LineRenderer>(cues, "aimLine").enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator OffscreenHostHidesCuesWithoutRevivingAnExpiredTimer()
        {
            Possess();
            yield return null;
            Camera camera = Camera.main;
            Quaternion rotation = camera.transform.rotation;
            camera.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up) * rotation;
            yield return null;
            Assert.That(group.gameObject.activeSelf, Is.True);
            Assert.That(group.GetComponent<CanvasGroup>().alpha, Is.Zero);
            camera.transform.rotation = rotation;
            yield return null;
            Assert.That(group.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            EventBus.PossessTimerUpdated?.Invoke(0f, possession.PossessDuration);
            yield return null;
            Assert.That(group.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyingCuesReleasesAimLineAndItsMaterial()
        {
            var cues = FindAll<PossessionCues>().First();
            var line = Read<LineRenderer>(cues, "aimLine");
            Material material = line.sharedMaterial;
            Material normal = enemy.GetComponent<Renderer>().sharedMaterial;
            Possess();
            Object.Destroy(cues);
            yield return null;
            yield return null;
            Assert.That(line == null, Is.True);
            Assert.That(material == null, Is.True);
            Assert.That(enemy.GetComponent<Renderer>().sharedMaterial, Is.SameAs(normal));
        }

        [UnityTest]
        public IEnumerator AbilityHintStaysBelowCircleInsteadOfCoveringIt()
        {
            Possess();
            foreach (float scale in new[] { 1f, 1.32f })
            {
                group.localScale = Vector3.one * scale;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Text hint = Read<Text>(FindAll<PossessionCues>().First(), "hintText");
                Assert.That(hint.gameObject.activeInHierarchy, Is.True);
                Vector3[] corners = new Vector3[4];
                hint.rectTransform.GetWorldCorners(corners);
                Vector3[] circle = new Vector3[4];
                group.GetWorldCorners(circle);
                Camera uiCamera = hint.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hint.canvas.worldCamera;
                float hintTop = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[1]).y;
                float ringBottom = circle.Min(p => Camera.main.WorldToScreenPoint(p).y);
                Assert.That(hintTop, Is.LessThan(ringBottom), "The tutorial zoom must preserve the gap.");
            }
        }

        private IEnumerator Load(string name)
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            level = SceneManager.GetSceneByName(name);
            hud = FindAll<HUDManager>().First();
            possession = FindAll<PossessionController>().First();
            possession.enabled = false;
            foreach (var host in FindAll<EnemyBase>()) host.MoveSpeed = 0f;
            // Combat flashes own their property blocks; isolate countdown rendering from attacks.
            foreach (var attack in FindAll<EnemyContactDamage>())
            {
                attack.StopAllCoroutines();
                attack.enabled = false;
            }
            enemy = FindAll<EnemyBase>().First(e => e is GuardEnemy);
            group = Read<GameObject>(hud, "possessTimerGroup").GetComponent<RectTransform>();
            ring = group.GetComponent<PossessionTimerGraphic>();
            Assert.That(ring, Is.Not.Null);
            yield return null;
        }

        private void Possess()
        {
            Set(possession, "ejectCooldownTimer", 0f);
            var charge = possession.GetComponent<PossessionCharge>();
            if (charge != null && !charge.HasCharge) charge.TryCollect();
            C3Approach(possession.GetComponent<SpiritController>(), enemy);
            typeof(PossessionController).GetMethod("Possess", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(possession, new object[] { enemy });
            Assert.That(possession.PossessedEnemy, Is.SameAs(enemy));
        }

        private static void C3Approach(SpiritController spirit, EnemyBase host)
        {
            foreach (Vector3 dir in new[] { -host.transform.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.forward })
            {
                Vector3 at = host.transform.position + dir * 1.8f;
                at.y = 1.15f;
                if (Physics.OverlapCapsule(at + Vector3.up * 0.45f, at - Vector3.up * 0.45f, 0.5f,
                    1 << 0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(spirit.transform))) continue;
                spirit.RestoreAt(at);
                Physics.SyncTransforms();
                if (host.CanBePossessedFrom(spirit.transform)) return;
            }
            Assert.Fail("No unobstructed possession approach for " + host.name);
        }

        private void AssertBelowFeet()
        {
            Assert.That(group.GetComponentInParent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(group.GetComponentInParent<Canvas>().worldCamera, Is.SameAs(Camera.main));
            Assert.That(group.position.x, Is.EqualTo(enemy.transform.position.x).Within(0.01f));
            Assert.That(group.position.z, Is.EqualTo(enemy.transform.position.z).Within(0.01f));
            Assert.That(Mathf.Abs(Vector3.Dot(group.forward, Vector3.up)), Is.GreaterThan(0.999f));
            var corners = new Vector3[4];
            group.GetWorldCorners(corners);
            Assert.That(corners.Max(p => p.y) - corners.Min(p => p.y), Is.LessThan(0.001f));
            Assert.That(group.position.y, Is.InRange(-0.05f, 0.3f));
            Assert.That(Vector3.Distance(corners[0], corners[3]), Is.InRange(1.7f, 2.5f));
        }

        private int MeshVertices()
        {
            using (var helper = new VertexHelper())
            {
                typeof(PossessionTimerGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(ring, new object[] { helper });
                return helper.currentVertCount;
            }
        }

        private T[] FindAll<T>() where T : Component => level.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static T Read<T>(object target, string field) => (T)target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
