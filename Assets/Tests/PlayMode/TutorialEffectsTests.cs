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
    public class TutorialEffectsTests
    {
        private TutorialGuide guide;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1f; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(TutorialScene.Name);
            yield return null;
            guide = Object.FindFirstObjectByType<TutorialGuide>();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("TutorialEffectsCleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
            EventBus.Clear(); Time.timeScale = 1f;
        }

        [Test]
        public void MovementKeysAreRequestedOneAtATimeInWasdOrder()
        {
            var p = new TutorialProgress(); p.Begin();
            Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.Forward));
            p.Move(TutorialMovement.Forward); Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.Left));
            p.Move(TutorialMovement.Left); Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.Back));
            p.Move(TutorialMovement.Back); Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.Right));
            p.Move(TutorialMovement.Right); Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.Jump));
            p.Move(TutorialMovement.Jump); Assert.That(p.NextMovement, Is.EqualTo(TutorialMovement.None));
        }

        [UnityTest]
        public IEnumerator StartingPracticeDoesNotOpenAnotherBlockingPopup()
        {
            guide.BeginPractice();
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(guide.Spirit.enabled, Is.True);
            Assert.That(guide.Possession.enabled, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovementKeyPopupChangesAfterDemonstratingEachDirection()
        {
            Begin(); yield return null;
            var coach = guide.GetComponent<TutorialCoach>();
            Text key = coach.GetComponentsInChildren<Text>(true).Single(t => t.name == "RequiredKey");
            Assert.That(key.text, Is.EqualTo("W"));
            guide.Progress.Move(TutorialMovement.Forward);
            yield return null;
            Assert.That(key.text, Is.EqualTo("A"));
            guide.Progress.Move(TutorialMovement.Left);
            yield return null;
            Assert.That(key.text, Is.EqualTo("S"));
            guide.Progress.Move(TutorialMovement.Back);
            yield return null;
            Assert.That(key.text, Is.EqualTo("D"));
        }

        [UnityTest]
        public IEnumerator NearbyGuardShowsEThenPossessionUsesNonBlockingLmbGuidance()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            guide.Guard.MoveSpeed = 0f;
            guide.Spirit.RestoreAt(guide.Guard.transform.position + Vector3.back * 2.5f);
            Physics.SyncTransforms(); yield return null;
            var key = guide.GetComponent<TutorialCoach>().GetComponentsInChildren<Text>(true).Single(t => t.name == "RequiredKey");
            Assert.That(key.text, Is.EqualTo("E"));
            Invoke(guide.Possession, "Possess", guide.Guard);
            Assert.That(guide.IsCoaching, Is.False);
            float timer = guide.Possession.PossessTimer;
            yield return new WaitForSeconds(0.15f);
            Assert.That(guide.Possession.PossessTimer, Is.LessThan(timer));
            StringAssert.Contains("LEFT CLICK", guide.Instruction);
            Assert.That(key.text, Is.EqualTo("LMB"));
            Assert.That(guide.GetComponent<TutorialCoach>().GetComponentsInChildren<TutorialRingGraphic>(true)
                .All(ring => ring.canvasRenderer != null), Is.True, "Every halo needs a CanvasRenderer to be visible.");
            typeof(Ability).GetField("usesLeft", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(guide.Guard.GetComponent<ShootAbility>(), 0);
            yield return null;
            Assert.That(key.text, Is.EqualTo("E"), "The large prompt must offer refill recovery when shots run out.");
        }

        [UnityTest]
        public IEnumerator PossessingDoesNotOpenTheBlockingAbilityCard()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            guide.Guard.MoveSpeed = 0f;
            guide.Spirit.RestoreAt(guide.Guard.transform.position + Vector3.back * 3f);
            yield return new WaitForSeconds(0.25f);
            Invoke(guide.Possession, "Possess", guide.Guard);
            yield return null;
            var coach = guide.GetComponent<TutorialCoach>();
            var card = coach.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "CoachCard");
            Assert.That(card.gameObject.activeInHierarchy, Is.False);
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator PossessionClockStaysVisibleWithoutPausingTheCountdown()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            Invoke(guide.Possession, "Possess", guide.Guard); Dismiss();
            Object.FindFirstObjectByType<Target>().OnHit();
            yield return null;
            Assert.That(guide.IsCoaching, Is.False);
            var bar = guide.FocusWidgets(TutorialFocus.PossessionClock).Single();
            Assert.That(bar.gameObject.activeInHierarchy, Is.True);
            float timer = guide.Possession.PossessTimer;
            yield return new WaitForSeconds(0.2f);
            Assert.That(guide.Possession.PossessTimer, Is.LessThan(timer));
            Assert.That(bar.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void BeginningPracticeKeepsControlsAndTimeRunning()
        {
            guide.BeginPractice();
            Assert.That(guide.Possession.enabled, Is.True);
            Assert.That(guide.Spirit.enabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator EnteringReadyExitFinishesEvenWhenOptionalEjectionLessonsAreIncomplete()
        {
            Begin();
            Invoke(guide.Possession, "Possess", guide.Guard); Dismiss();
            Object.FindFirstObjectByType<Target>().OnHit();
            Invoke(guide.Possession, "Eject", false);
            Invoke(guide.Possession, "Possess", guide.Heavy); Dismiss();
            Object.FindFirstObjectByType<BreakableWall>().TakeDamage(2);
            Invoke(guide.Possession, "Eject", false);
            Assert.That(guide.Progress.ForcedOut, Is.False);
            Assert.That(guide.Progress.Movement, Is.EqualTo(TutorialMovement.None));
            var exit = Object.FindFirstObjectByType<LevelExit>();
            Assert.That(exit.IsReady, Is.True);
            guide.Spirit.RestoreAt(exit.transform.position);
            Physics.SyncTransforms();
            Invoke(exit, "OnTriggerStay", guide.Spirit.GetComponent<Collider>());
            Assert.That(guide.Progress.Completed, Is.True);
            Assert.That(Object.FindFirstObjectByType<GameManager>().CurrentState, Is.EqualTo(LevelState.Complete));
            yield return null;
            var modal = guide.GetComponent<TutorialOverlay>().GetComponentsInChildren<Text>(true).Single(t => t.name == "Title");
            Assert.That(modal.text, Is.EqualTo("YOU'RE READY."));
        }

        [UnityTest]
        public IEnumerator WallBreakerPromptsAndRealSmashWorkWithoutTheGuardEjectionChecklist()
        {
            Begin();
            Invoke(guide.Possession, "Possess", guide.Guard); Dismiss();
            Object.FindFirstObjectByType<Target>().OnHit();
            Invoke(guide.Possession, "Eject", false);
            Assert.That(guide.Charges.TryCollect(), Is.True, "Simulate collecting the tutorial Soul Coin.");
            guide.Heavy.MoveSpeed = 0f;
            guide.Spirit.RestoreAt(guide.Heavy.transform.position + Vector3.back * 2.5f);
            Physics.SyncTransforms(); yield return null;
            Assert.That(guide.Progress.Movement, Is.EqualTo(TutorialMovement.None));
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.FindHeavy));
            var coach = guide.GetComponent<TutorialCoach>();
            var key = coach.GetComponentsInChildren<Text>(true).Single(t => t.name == "RequiredKey");
            var label = coach.GetComponentsInChildren<Text>(true).Single(t => t.name == "Action");
            Assert.That(key.text, Is.EqualTo("E"));
            Assert.That(label.text, Is.EqualTo("WALL BREAKER"));
            Invoke(guide.Possession, "Possess", guide.Heavy);
            Assert.That(guide.IsCoaching, Is.False);
            yield return null;
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.SmashWall));
            StringAssert.Contains("brick wall", guide.Instruction);
            Assert.That(key.text, Is.EqualTo("LMB"));
            Assert.That(label.text, Is.EqualTo("SMASH THIS WALL"));
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            var controller = guide.Heavy.GetComponent<CharacterController>();
            controller.enabled = false;
            guide.Heavy.transform.position = new Vector3(wall.transform.position.x, 1.3f, wall.transform.position.z - 2.4f);
            guide.Heavy.transform.rotation = Quaternion.identity;
            controller.enabled = true; Physics.SyncTransforms();
            guide.Heavy.GetComponent<SmashAbility>().TryExecute();
            Assert.That(guide.Progress.BrokeWall, Is.True, "The Wall Breaker must really smash the scene wall through the shared ability.");
        }

        [UnityTest]
        public IEnumerator WallBreakerCountdownAndForcedEjectionFollowTheCorrectEnemy()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            Invoke(guide.Possession, "Possess", guide.Heavy); Dismiss();
            typeof(PossessionController).GetField("possessTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(guide.Possession, 2.5f);
            yield return null;
            var coach = guide.GetComponent<TutorialCoach>();
            Assert.That(coach.GetComponentsInChildren<Text>(true).Single(t => t.name == "RequiredKey").text, Is.EqualTo("3"));
            Assert.That(coach.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "AutoEjectClockHalo").gameObject.activeInHierarchy, Is.True);
            typeof(PossessionController).GetField("possessTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(guide.Possession, 0.01f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(guide.Possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(guide.Heavy.IsHostile, Is.True);
            Assert.That(guide.HostileEnemy, Is.SameAs(guide.Heavy));
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.EscapeHostile));
            StringAssert.Contains("wall breaker", guide.Instruction);
            Assert.That(coach.GetComponentsInChildren<Text>(true).Single(t => t.name == "Action").text, Is.EqualTo("AUTO-EJECTED!"));
            typeof(EnemyBase).GetField("hostileTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(guide.Heavy, 0.01f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(guide.Progress.SurvivedHostility, Is.True);
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.FindGuard));
        }

        private void Begin() { guide.BeginPractice(); Dismiss(); }
        private void Dismiss() { for (int i = 0; i < 5 && guide.IsCoaching; i++) guide.ContinueCoaching(); }
        private static void Invoke(object item, string method, params object[] args)
        {
            if (method == "Possess" && item is PossessionController controller)
            {
                typeof(PossessionController).GetField("ejectCooldownTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 0f);
                var charges = controller.GetComponent<PossessionCharge>();
                if (charges != null && !charges.HasCharge)
                    Assert.That(charges.TryCollect(), Is.True, "Simulate collecting the tutorial Soul Coin.");
                if (args.Length > 0 && args[0] is EnemyBase body)
                {
                    controller.GetComponent<SpiritController>().RestoreAt(
                        body.transform.position - body.transform.forward * 2.5f);
                    Physics.SyncTransforms();
                }
            }
            item.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, args);
        }
    }
}
