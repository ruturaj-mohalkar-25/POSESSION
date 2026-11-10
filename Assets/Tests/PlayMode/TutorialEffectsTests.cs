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
        public IEnumerator CompactTutorialNeverPausesForAHudSpotlight()
        {
            var clock = guide.FocusWidgets(TutorialFocus.LevelClock).Single();
            Vector3 original = clock.localScale;
            float timer = guide.LevelSeconds;
            yield return new WaitForSeconds(0.2f);
            Assert.That(clock.localScale, Is.EqualTo(original));
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(guide.LevelSeconds, Is.LessThan(timer));
        }

        [UnityTest]
        public IEnumerator MovementDoesNotCreateAKeyByKeyPopup()
        {
            Begin(); yield return null;
            Assert.That(guide.GetComponent<TutorialCoach>(), Is.Null);
            Assert.That(guide.Title, Is.EqualTo("WHITE SPIRIT · WASD TO MOVE"));
            guide.Progress.Move(TutorialMovement.Forward);
            guide.Progress.Move(TutorialMovement.Left);
            guide.Progress.Move(TutorialMovement.Back);
            guide.Progress.Move(TutorialMovement.Right);
            guide.Progress.Move(TutorialMovement.Jump);
            yield return null;
            Assert.That(guide.Progress.Movement, Is.EqualTo(TutorialMovement.All));
            Assert.That(guide.Title, Is.EqualTo("GREEN = SAFE TO POSSESS · E · COSTS 1 CHARGE"));
        }

        [UnityTest]
        public IEnumerator FirstInstructionIsNotCompetingWithAPossessionPrompt()
        {
            Begin(); yield return null;
            Assert.That(guide.Possession.PossessRange, Is.EqualTo(5f));
            Assert.That(Vector3.Distance(guide.Spirit.transform.position, guide.Guard.transform.position),
                Is.GreaterThan(guide.Possession.PossessRange));
            Assert.That(guide.Title, Is.EqualTo("WHITE SPIRIT · WASD TO MOVE"));
        }

        [UnityTest]
        public IEnumerator MovingFirstTeachesThatTheRedConeIsEnemyVision()
        {
            Begin();
            Vector3 start = guide.Spirit.transform.position;
            guide.Spirit.RestoreAt(start + Vector3.right * 1.5f);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(guide.Progress.Movement, Is.EqualTo(TutorialMovement.All));
            Assert.That(guide.Title, Is.EqualTo("RED CONE = ENEMY VISION · STAY OUT"));
        }

        [UnityTest]
        public IEnumerator NearbyGuardShowsEAndPossessionImmediatelyChangesToLmb()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            guide.Guard.MoveSpeed = 0f;
            TutorialTestSupport.Approach(guide, guide.Guard);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(guide.Nearby(out _, out string prompt, out _), Is.True);
            Assert.That(prompt, Is.EqualTo("E"));
            yield return TutorialTestSupport.Possess(guide, guide.Guard);
            Assert.That(guide.Title, Is.EqualTo("LMB · HIT THE TARGET"));
            float timer = guide.Possession.PossessTimer;
            yield return new WaitForSeconds(0.15f);
            Assert.That(guide.Possession.PossessTimer, Is.LessThan(timer),
                "The shared timer should keep running instead of pausing for a text card.");
        }

        [UnityTest]
        public IEnumerator NoAbilityCardOrPulsingTutorialRingsAreCreated()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            yield return TutorialTestSupport.Possess(guide, guide.Guard);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<TutorialCoach>(), Is.Null);
            Assert.That(Object.FindObjectsByType<TutorialRingGraphic>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty);
            var overlay = guide.GetComponent<TutorialOverlay>();
            Assert.That(overlay.GetComponentsInChildren<Transform>(true)
                .Any(t => t.name == "CoachCard" || t.name.Contains("Halo")), Is.False);
        }

        [UnityTest]
        public IEnumerator PossessionClockStaysVisibleWithoutAPausedExplanation()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            yield return TutorialTestSupport.Possess(guide, guide.Guard);
            var bar = guide.FocusWidgets(TutorialFocus.PossessionClock).Single();
            Assert.That(bar.gameObject.activeInHierarchy, Is.True);
            float timer = guide.Possession.PossessTimer;
            yield return new WaitForSeconds(0.2f);
            Assert.That(guide.Possession.PossessTimer, Is.LessThan(timer));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(bar.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void DisablingTheGuideLeavesSharedControlsAndTimeUsable()
        {
            guide.BeginPractice();
            Assert.That(guide.Possession.enabled, Is.True);
            guide.enabled = false;
            Assert.That(guide.Possession.enabled, Is.True);
            Assert.That(guide.Spirit.enabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator EnteringReadyExitFinishesEvenWhenOptionalEjectionLessonsAreIncomplete()
        {
            Begin();
            yield return TutorialTestSupport.Possess(guide, guide.Guard);
            Object.FindFirstObjectByType<Target>().OnHit();
            Invoke(guide.Possession, "Eject", false);
            yield return TutorialTestSupport.Possess(guide, guide.Heavy);
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
            Assert.That(guide.GetComponent<TutorialOverlay>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator WallBreakerUsesOneShortObjectiveAndTheRealSmashAbility()
        {
            Begin();
            yield return TutorialTestSupport.Possess(guide, guide.Guard);
            Object.FindFirstObjectByType<Target>().OnHit();
            Invoke(guide.Possession, "Eject", false);
            yield return TutorialTestSupport.PreparePossession(guide, guide.Heavy);
            yield return null;
            Assert.That(guide.Title, Is.EqualTo("GREEN = SAFE TO POSSESS HEAVY · E"));
            yield return TutorialTestSupport.Possess(guide, guide.Heavy);
            Assert.That(guide.Title, Is.EqualTo("LMB · SMASH THE CRACKED WALL"));
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
        public IEnumerator ForcedEjectionTracksTheCorrectHeavyWithoutAnotherPopup()
        {
            Begin(); guide.Progress.Move(TutorialMovement.All);
            yield return TutorialTestSupport.Possess(guide, guide.Heavy);
            typeof(PossessionController).GetField("possessTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(guide.Possession, 0.01f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(guide.Possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(guide.Heavy.IsHostile, Is.True);
            Assert.That(guide.HostileEnemy, Is.SameAs(guide.Heavy));
            Assert.That(guide.Progress.ForcedOut, Is.True);
            Assert.That(Object.FindFirstObjectByType<TutorialCoach>(), Is.Null);
            typeof(EnemyBase).GetField("hostileTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(guide.Heavy, 0.01f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(guide.Heavy.IsHostile, Is.False);
        }

        private void Begin() { guide.BeginPractice(); Dismiss(); }
        private void Dismiss() { for (int i = 0; i < 5 && guide.IsCoaching; i++) guide.ContinueCoaching(); }
        private static void Invoke(object item, string method, params object[] args)
        {
            if(item is PossessionController pc && method=="Possess")
            {
                var body=(EnemyBase)args[0];var spirit=pc.GetComponent<SpiritController>();
                var charge=pc.GetComponent<PossessionCharge>();if(charge!=null&&!charge.HasCharge)charge.TryCollect();
                typeof(PossessionController).GetField("ejectCooldownTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pc,0f);
                spirit.RestoreAt(body.transform.position-body.transform.forward*2f);Physics.SyncTransforms();
                Assert.That(pc.TryPossess(body),Is.True,body.name);return;
            }
            item.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(item,args);
        }
    }
}
