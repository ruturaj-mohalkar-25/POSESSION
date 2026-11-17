using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4GoldPart2Tests
    {
        private SpiritController spirit;
        private PossessionController player;
        private Level4FloorVisibility floorVisibility;

        [UnitySetUp]
        public IEnumerator LoadSavedLevel()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 60;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_04_Gold");
            foreach (var reporter in All<FirebaseReporter>()) Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in All<P5TimingTracker>()) tracker.PersistReports = false;
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var attack in All<EnemyContactDamage>()) attack.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            spirit = All<SpiritController>().Single();
            player = spirit.GetComponent<PossessionController>();
            floorVisibility = All<Level4FloorVisibility>().Single();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Level4GoldPart2Cleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator ActualUpperGuardWarningHidesItsCapBrimAndBadgeWithTheBody()
        {
            var guard = All<GuardEnemy>().Single(e => e.name == "Guard_ArchiveEast");
            yield return CheckPossessedWarning(guard, Plan(70f, 6.4f, 25f));
        }

        [UnityTest]
        public IEnumerator ActualUpperHeavyWarningHidesEveryArmourPieceWithTheBody()
        {
            var heavy = All<HeavyEnemy>().Single(e => e.name == "Heavy_UpperWatch");
            yield return CheckPossessedWarning(heavy, Plan(70f, 6.775f, 25f));
        }

        [UnityTest]
        public IEnumerator FloorCutawayHidesAllUpperEnemyAccentsAndRestoresThemOnlyUpstairs()
        {
            var upperEnemies = floorVisibility.UpperRoot.GetComponentsInChildren<EnemyBase>(true)
                .Where(e => e is GuardEnemy || e is HeavyEnemy).ToArray();
            Assert.That(upperEnemies.OfType<GuardEnemy>().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(upperEnemies.OfType<HeavyEnemy>().Count(), Is.GreaterThanOrEqualTo(1));
            foreach (var enemy in upperEnemies) AssertOwnedPieces(enemy);

            foreach (bool upstairs in new[] { false, true, false, true })
            {
                spirit.RestoreAt(upstairs ? Plan(70f, 6.4f, 25f) : Plan(9f, 1.4f, 7f));
                yield return null;
                yield return null;
                Assert.That(floorVisibility.UpperVisible, Is.EqualTo(upstairs));
                foreach (var enemy in upperEnemies)
                {
                    var body = enemy.GetComponent<Renderer>();
                    Assert.That(body.forceRenderingOff, Is.EqualTo(!upstairs), enemy.name + " body cutaway.");
                    foreach (var piece in OwnedPieces(enemy))
                    {
                        Assert.That(piece.enabled && piece.gameObject.activeInHierarchy, Is.True);
                        Assert.That(piece.forceRenderingOff, Is.EqualTo(!upstairs),
                            enemy.name + "/" + piece.name + " must not float above the lower floor.");
                    }
                }
            }
        }

        private IEnumerator CheckPossessedWarning(EnemyBase enemy, Vector3 position)
        {
            var body = enemy.GetComponent<Renderer>();
            AssertOwnedPieces(enemy);
            var pieces = OwnedPieces(enemy);
            Position(enemy, position, Vector3.right);
            spirit.RestoreAt(position + Vector3.left * 2.5f);
            enemy.enabled = true;
            yield return null;
            yield return null;
            Physics.SyncTransforms();
            Assert.That(floorVisibility.UpperVisible, Is.True);
            Assert.That(body.forceRenderingOff, Is.False);
            Assert.That(pieces.All(p => !p.forceRenderingOff), Is.True,
                "The complete host silhouette must be visible before possession.");
            Assert.That(player.TryPossess(enemy), Is.True, "Use the saved player's real possession rules.");
            Assert.That(player.PossessedEnemy, Is.SameAs(enemy));
            var material = body.sharedMaterial;
            SetPossessionTimer(4.5f);
            bool bodyVisible = false, bodyHidden = false;
            float initialTimer = player.PossessTimer;
            for (int frame = 0; frame < 100; frame++)
            {
                yield return null;
                Assert.That(player.PossessedEnemy, Is.SameAs(enemy));
                Assert.That(floorVisibility.UpperVisible, Is.True);
                Assert.That(body.enabled && body.gameObject.activeInHierarchy, Is.True);
                bodyHidden |= body.forceRenderingOff;
                bodyVisible |= !body.forceRenderingOff;
                foreach (var piece in pieces)
                {
                    Assert.That(piece.enabled && piece.gameObject.activeInHierarchy, Is.True,
                        "The warning changes rendering without disabling or deleting the presentation.");
                    Assert.That(piece.forceRenderingOff, Is.EqualTo(body.forceRenderingOff),
                        enemy.name + "/" + piece.name + " must follow every visible and hidden warning phase.");
                }
            }
            Assert.That(bodyVisible && bodyHidden, Is.True,
                "The live possession countdown must produce both warning phases during this check.");
            Assert.That(player.PossessTimer, Is.LessThan(initialTimer - 1f));
            Assert.That(body.sharedMaterial, Is.SameAs(material), "Warning blinking must preserve the possessed material.");
            Assert.That(player.TryEject(false), Is.True);
            Assert.That(player.IsEjectShielded, Is.True);
            Assert.That(spirit.DamageProtectionRemaining, Is.GreaterThan(1.9f));
            int hp = spirit.CurrentHP;
            spirit.TakeDamage(1);
            Assert.That(spirit.CurrentHP, Is.EqualTo(hp));
            yield return null;
            yield return null;
            Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(enemy.IsPossessed, Is.False);
            Assert.That(body.forceRenderingOff, Is.False);
            Assert.That(pieces.All(p => !p.forceRenderingOff), Is.True,
                "Ejection must restore all cap and armour pieces on the visible upper floor.");
        }

        private static void AssertOwnedPieces(EnemyBase enemy)
        {
            var look = enemy.GetComponent<GoldEnemyPresentation>();
            Assert.That(look, Is.Not.Null, enemy.name + " should use the newly merged shared Gold presentation.");
            Assert.That(look.IsBuilt, Is.True);
            var pieces = OwnedPieces(enemy);
            Assert.That(pieces.Length, Is.EqualTo(look.VisualPieceCount));
            Assert.That(pieces.Length, Is.EqualTo(enemy is GuardEnemy ? 3 : 5));
            Assert.That(pieces.All(p => p.GetComponent<Collider>() == null), Is.True);
        }

        private static Renderer[] OwnedPieces(EnemyBase enemy)
        {
            string[] names = enemy is GuardEnemy
                ? new[] { "Guard Cap", "Guard Cap Brim", "Guard Badge" }
                : new[] { "Heavy Left Shoulder", "Heavy Right Shoulder", "Heavy Chest Plate", "Heavy Chest Mark", "Heavy Belt" };
            return enemy.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.transform.parent == enemy.transform && names.Contains(r.name)).ToArray();
        }

        private void SetPossessionTimer(float seconds) => typeof(PossessionController)
            .GetField("possessTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, seconds);

        private static void Position(EnemyBase enemy, Vector3 position, Vector3 forward)
        {
            var controller = enemy.GetComponent<CharacterController>();
            bool enabled = controller.enabled;
            controller.enabled = false;
            enemy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            controller.enabled = enabled;
            Physics.SyncTransforms();
        }

        private static Vector3 Plan(float x, float y, float z) => Level4Layout.Map(new Vector3(x, y, z));

        private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }
}
