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
    public class PlaytestFeedbackTests
    {
        private TutorialGuide guide;
        private PossessionController possession;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(TutorialScene.Name);
            yield return null;
            guide = Object.FindFirstObjectByType<TutorialGuide>();
            possession = guide.Possession;
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                enemy.MoveSpeed = 0f;
            foreach (var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))
                attack.enabled = false;
            guide.Spirit.GrantDamageProtection(100f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("FeedbackTestCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator FirstInstructionExplainsApproachAndMovementBeforeAdvertisingE()
        {
            Assert.That(Vector3.Distance(guide.Spirit.transform.position, guide.Guard.transform.position),
                Is.GreaterThan(possession.PossessRange));
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.OutOfRange));
            Assert.That(guide.Title, Does.Contain("MOVE CLOSER"));
            Assert.That(guide.Title, Does.Not.Contain("PRESS E"));
            yield return null;
            Assert.That(Hint().text, Does.Contain("WASD").And.Contain("SPACE"));
        }

        [UnityTest]
        public IEnumerator ReadyInstructionLeadsToRealPossessionAndExplainsTheCountdown()
        {
            TutorialTestSupport.Approach(guide, guide.Guard);
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.None));
            Assert.That(guide.Title, Is.EqualTo("PRESS E - POSSESS GUARD"));
            Assert.That(guide.Nearby(out _, out string key, out _), Is.True);
            Assert.That(key, Is.EqualTo("E"));
            Assert.That(possession.TryPossess(guide.Guard), Is.True);
            yield return null;
            Assert.That(guide.Title, Does.Contain("HIT THE TARGET"));
            Assert.That(Hint().text, Does.Contain("eject early").And.Contain("Empty ring = forced eject"));
        }

        [Test]
        public void WallBlockExplainsTheObstacleInsteadOfPromisingPossession()
        {
            TutorialTestSupport.Approach(guide, guide.Guard);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Feedback test obstruction";
            wall.transform.position = (possession.transform.position + guide.Guard.transform.position) * .5f;
            wall.transform.localScale = new Vector3(.4f, 5f, .4f);
            Physics.SyncTransforms();
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.Obstructed));
            Assert.That(guide.Title, Does.Contain("GO AROUND WALL"));
            Assert.That(possession.TryPossess(guide.Guard), Is.False);
            guide.Nearby(out _, out string key, out _);
            Assert.That(key, Is.Empty);
        }

        [Test]
        public void VisionConeExplainsHowToApproachSafely()
        {
            guide.Guard.transform.SetPositionAndRotation(new Vector3(100f, 1f, 100f), Quaternion.identity);
            guide.Guard.ConfigureHomeBounds(new Bounds(new Vector3(100f, 1f, 100f), Vector3.one * 30f));
            Set(guide.Guard, "detectionRange", 9f);
            guide.Spirit.RestoreAt(new Vector3(100f, 1f, 103f));
            Physics.SyncTransforms();
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.InVisionCone));
            Assert.That(guide.Title, Does.Contain("AVOID RED CONE"));
            Assert.That(possession.TryPossess(guide.Guard), Is.False);
        }

        [Test]
        public void MissingChargeAndEjectionRecoveryNeverAdvertiseE()
        {
            TutorialTestSupport.Approach(guide, guide.Guard);
            var charge = possession.GetComponent<PossessionCharge>();
            charge.Clear();
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.NoCharges));
            Assert.That(guide.Title, Does.Contain("COLLECT WHITE SOUL"));
            guide.Nearby(out _, out string key, out _);
            Assert.That(key, Is.Empty);
            Assert.That(charge.TryCollect(), Is.True);
            Assert.That(possession.TryPossess(guide.Guard), Is.True);
            Assert.That(possession.TryEject(false), Is.True);
            Assert.That(charge.TryCollect(), Is.True);
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.Recovering));
            Assert.That(guide.Title, Does.Contain("RECOVERING"));
            guide.Nearby(out _, out key, out _);
            Assert.That(key, Is.Empty);
            Assert.That(guide.Spirit.DamageProtectionRemaining, Is.GreaterThan(1.9f));
        }

        [UnityTest]
        public IEnumerator HudMarkerUsesTheValidTargetEvenWhenAnotherBodyIsCloser()
        {
            guide.Spirit.RestoreAt(new Vector3(100f, 1f, 100f));
            guide.Guard.transform.SetPositionAndRotation(new Vector3(100f, 1f, 104f), Quaternion.identity);
            Object.FindFirstObjectByType<CameraFollow>().enabled = false;
            Camera.main.transform.position = possession.transform.position + new Vector3(0f, 15f, -11f);
            Camera.main.transform.LookAt(possession.transform.position);
            var decoy = new GameObject("Closer body facing the Spirit") { layer = guide.Guard.gameObject.layer };
            decoy.transform.position = new Vector3(101.5f, 1f, 101.5f);
            decoy.transform.forward = (possession.transform.position - decoy.transform.position).normalized;
            var enemy = decoy.AddComponent<GuardEnemy>();
            enemy.enabled = false;
            Set(enemy, "detectionRange", 9f);
            Physics.SyncTransforms();
            Assert.That(enemy.CanBePossessedFrom(possession.transform), Is.False);
            Assert.That(possession.FindPossessableEnemy(), Is.SameAs(guide.Guard));
            yield return null;
            yield return null; // Observe the marker after the HUD's LateUpdate has positioned it.
            var hud = Object.FindFirstObjectByType<HUDManager>();
            var prompt = (GameObject)Field(hud, "possessPrompt").GetValue(hud);
            Assert.That(prompt.activeInHierarchy, Is.True);
            float height = (float)Field(hud, "promptHeight").GetValue(hud);
            Vector3 expected = Camera.main.WorldToScreenPoint(guide.Guard.transform.position
                + Vector3.up * height * guide.Guard.transform.lossyScale.y);
            Assert.That(Vector2.Distance(prompt.transform.position, expected), Is.LessThan(1f));
        }

        [Test]
        public void TutorialDoesNotAdvertiseEForABodyWhenAnotherValidBodyIsNearer()
        {
            guide.Spirit.RestoreAt(new Vector3(100f, 1f, 100f));
            guide.Guard.transform.SetPositionAndRotation(new Vector3(100f, 1f, 104f), Quaternion.identity);
            guide.Heavy.transform.SetPositionAndRotation(new Vector3(102f, 1f, 100f), Quaternion.identity);
            Physics.SyncTransforms();
            Assert.That(possession.GetPossessionBlockReason(guide.Guard), Is.EqualTo(PossessionBlockReason.None));
            Assert.That(possession.FindPossessableEnemy(), Is.SameAs(guide.Heavy));
            Assert.That(guide.Title, Does.Contain("MOVE CLOSER").And.Not.Contain("PRESS E"));
        }

        [Test]
        public void EnteringTheWrongBodyExplainsHowToReturnToTheGuardLesson()
        {
            TutorialTestSupport.Approach(guide, guide.Heavy);
            Assert.That(possession.TryPossess(guide.Heavy), Is.True);
            Assert.That(guide.Progress.HitTarget, Is.False);
            Assert.That(guide.Title, Is.EqualTo("E - RETURN TO SPIRIT"));
        }

        [Test]
        public void FixedCircleKeepsItsShapeAndContrastWhileTheColouredArcShrinks()
        {
            var ring = Object.FindFirstObjectByType<PossessionTimerGraphic>(FindObjectsInactive.Include);
            var full = Mesh(ring, 1f);
            var low = Mesh(ring, .08f);
            try
            {
                Assert.That(low.vertexCount, Is.LessThan(full.vertexCount));
                const int rimVertices = 64 * 4;
                var expected = full.vertices.Skip(full.vertexCount - rimVertices).ToArray();
                var actual = low.vertices.Skip(low.vertexCount - rimVertices).ToArray();
                Assert.That(actual, Is.EqualTo(expected));
                foreach (Color32 color in low.colors32.Skip(low.vertexCount - rimVertices))
                {
                    Assert.That(color.r, Is.GreaterThan(190));
                    Assert.That(color.g, Is.GreaterThan(190));
                    Assert.That(color.b, Is.GreaterThan(190));
                }
            }
            finally { Object.DestroyImmediate(full); Object.DestroyImmediate(low); }
        }

        private static Mesh Mesh(PossessionTimerGraphic ring, float fraction)
        {
            ring.SetProgress(fraction, Color.red);
            var mesh = new Mesh();
            using (var helper = new VertexHelper())
            {
                typeof(PossessionTimerGraphic).GetMethod("OnPopulateMesh",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Invoke(ring, new object[] { helper });
                helper.FillMesh(mesh);
            }
            return mesh;
        }

        private static Text Hint() => Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
            FindObjectsSortMode.None).Single(t => t.name == "HintBody");

        private static FieldInfo Field(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new System.MissingFieldException(name);
        }

        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    }
}
