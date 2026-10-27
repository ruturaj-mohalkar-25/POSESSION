using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Possession.Tests
{
    public class TutorialTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var active = SceneManager.GetActiveScene();
            if (active.name == TutorialScene.Name || active.name == "MainMenu" || active.name == "Level_01")
            {
                var empty = SceneManager.CreateScene("TutorialTestCleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(active);
            }
            EventBus.Clear();
        }

        [Test]
        public void MovementNeedsEveryDirectionAndJump()
        {
            var p = new TutorialProgress();
            p.Move(TutorialMovement.All);
            Assert.That(p.Movement, Is.EqualTo(TutorialMovement.None));
            p.Begin();
            p.Move(TutorialMovement.Forward | TutorialMovement.Left | TutorialMovement.Back | TutorialMovement.Right);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.Movement));
            p.Move(TutorialMovement.Jump);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.FindGuard));
        }

        [Test]
        public void GuidedSequenceTeachesAllSharedMechanics()
        {
            var p = new TutorialProgress(); p.Begin(); p.Move(TutorialMovement.All);
            p.Possessed(PlayerState.Possessed_Guard);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.ShootTarget));
            p.TargetWasHit();
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.WaitForTimer));
            p.Ejected(true, false);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.FindHeavy));
            p.Possessed(PlayerState.Possessed_Guard);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.WaitForTimer));
            p.Ejected(true, true);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.EscapeHostile));
            p.HostilityEnded();
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.FindHeavy));
            p.Possessed(PlayerState.Possessed_Heavy); p.WallWasBroken();
            Assert.That(p.CanFinish, Is.True);
            p.Finish(); Assert.That(p.Completed, Is.False, "Finishing still requires Spirit form.");
            p.Ejected(false, false); p.Finish();
            Assert.That(p.Completed, Is.True);
            Assert.That(p.ClearedSkills, Is.EqualTo(9));
        }

        [Test]
        public void EjectionChecklistDoesNotBlockTheWallBreakerLesson()
        {
            var p = new TutorialProgress(); p.Begin(); p.Move(TutorialMovement.All);
            p.Possessed(PlayerState.Possessed_Guard); p.TargetWasHit();
            p.Ejected(true, true); p.HostilityEnded();
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.FindHeavy));
            p.Possessed(PlayerState.Possessed_Guard);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.ManualEject));
            p.Ejected(true, false);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.FindHeavy));
        }

        [Test]
        public void WallBreakerAlsoTeachesForcedEjection()
        {
            var p = new TutorialProgress(); p.Begin(); p.Possessed(PlayerState.Possessed_Heavy);
            p.Ejected(false, true);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.EscapeHostile));
            p.HostilityEnded();
            Assert.That(p.ForcedOut, Is.True);
            Assert.That(p.SurvivedHostility, Is.True);
            p.Fail(); p.Move(TutorialMovement.All); p.Finish();
            Assert.That(p.Completed, Is.False);
            Assert.That(p.Lesson, Is.EqualTo(TutorialLesson.Failed));
        }

        [UnityTest]
        public IEnumerator MainMenuEntryWorksAndDoesNotDuplicateOrReplacePlay()
        {
            yield return Load("MainMenu");
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var scene = SceneManager.GetSceneByName("MainMenu");
            var first = TutorialMenuEntry.Install(scene);
            Assert.That(TutorialMenuEntry.Install(scene), Is.SameAs(first));
            Assert.That(menu.GetComponentsInChildren<Button>(true).Count(b => b.name == "HowToPlayButton"), Is.EqualTo(1));
            Assert.That(first.interactable, Is.True);
            menu.OnPlayButton(); Assert.That(first.gameObject.activeInHierarchy, Is.False);
            menu.OnBackButton(); Assert.That(first.gameObject.activeInHierarchy, Is.True);
            first.onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(TutorialScene.Name));
            Assert.That(Object.FindFirstObjectByType<TutorialGuide>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator WelcomePausesClockAndBeginStartsSharedGameplay()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            var manager = Object.FindFirstObjectByType<GameManager>();
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.Welcome));
            Assert.That(Time.timeScale, Is.Zero);
            float remaining = manager.LevelTimer;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(manager.LevelTimer, Is.EqualTo(remaining));
            guide.BeginPractice();
            Assert.That(guide.CoachingFocus, Is.EqualTo(TutorialFocus.LevelClock));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(guide.Possession.enabled, Is.False);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(manager.LevelTimer, Is.EqualTo(remaining));
            guide.ContinueCoaching();
            Assert.That(guide.CoachingFocus, Is.EqualTo(TutorialFocus.Health));
            var hp = guide.FocusWidgets(TutorialFocus.Health)[1];
            Vector3 originalScale = hp.localScale;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(hp.localScale.x, Is.GreaterThan(originalScale.x));
            guide.ContinueCoaching();
            Assert.That(hp.localScale, Is.EqualTo(originalScale));
            Assert.That(guide.Possession.enabled, Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(manager.LevelTimer, Is.LessThan(remaining));
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.Movement));
            Assert.That(guide.Possession.PossessDuration, Is.EqualTo(12f));
        }

        [UnityTest]
        public IEnumerator SharedGuardReallyForcesEjectionAndBecomesHostile()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>(); Begin(guide);
            guide.Progress.Move(TutorialMovement.All);
            var pc = guide.Possession;
            Set(pc, "possessDuration", 0.1f);
            Set(pc, "hostileTime", 0.2f);
            Possess(guide, guide.Guard);
            yield return new WaitForSeconds(0.15f);
            Assert.That(pc.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(guide.Guard.IsHostile, Is.True);
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.EscapeHostile));
            yield return new WaitForSeconds(0.25f);
            Assert.That(guide.Guard.IsHostile, Is.False);
            Assert.That(guide.Progress.SurvivedHostility, Is.True);
        }

        [UnityTest]
        public IEnumerator SharedPuzzleEventsGateTheTutorialExitAndCompletion()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>(); Begin(guide);
            var target = Object.FindFirstObjectByType<Target>();
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            var exit = Object.FindFirstObjectByType<LevelExit>();
            var trigger = exit.GetComponent<BoxCollider>();
            var pc = guide.Possession;
            Assert.That(trigger.enabled, Is.True);
            Assert.That(exit.IsReady, Is.False);
            guide.Progress.Move(TutorialMovement.All);
            Possess(guide, guide.Guard);
            target.OnHit();
            Assert.That(guide.Progress.HitTarget, Is.True);
            Invoke(pc, "Eject", false);
            Possess(guide, guide.Guard);
            Set(pc, "possessTimer", 0.01f); Set(pc, "hostileTime", 0.05f);
            yield return new WaitForSeconds(0.12f);
            Possess(guide, guide.Heavy);
            wall.TakeDamage(2);
            yield return null;
            Assert.That(guide.Progress.BrokeWall, Is.True);
            Assert.That(guide.Progress.CanFinish, Is.True);
            Assert.That(trigger.enabled, Is.True);
            Invoke(pc, "Eject", false);
            var spirit = pc.GetComponent<SpiritController>();
            spirit.RestoreAt(exit.transform.position);
            Physics.SyncTransforms();
            Invoke(exit, "OnTriggerStay", spirit.GetComponent<Collider>());
            Assert.That(guide.Progress.Completed, Is.True);
            Assert.That(guide.Progress.ClearedSkills, Is.EqualTo(9));
            Assert.That(Object.FindFirstObjectByType<GameManager>().CurrentState, Is.EqualTo(LevelState.Complete));
            yield return null;
            var overlay = Object.FindFirstObjectByType<TutorialOverlay>();
            var play = overlay.GetComponentsInChildren<Button>(true).First(b => b.name == "StartPractice");
            Assert.That(play.GetComponentInChildren<Text>().text, Is.EqualTo("PLAY LEVEL 01"));
        }

        [UnityTest]
        public IEnumerator ForcedGuardCanDamageTheSpiritUsingItsSharedAttack()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>(); Begin(guide);
            var pc = guide.Possession;
            Set(pc, "possessDuration", 0.05f);
            Set(pc, "hostileTime", 2f);
            Possess(guide, guide.Guard);
            yield return new WaitForSeconds(0.1f);
            var spirit = pc.GetComponent<SpiritController>();
            spirit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            Assert.That(guide.Guard.IsHostile, Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(spirit.CurrentHP, Is.LessThan(spirit.MaxHP), "The lesson must use the Guard's real attack, not only a warning message.");
        }

        [UnityTest]
        public IEnumerator TimeoutShowsRetryAndReplayResetsTheWholeLesson()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>(); Begin(guide);
            var manager = Object.FindFirstObjectByType<GameManager>();
            Set(manager, "levelTimer", 0.01f);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(guide.Progress.Failed, Is.True);
            guide.Replay(); yield return null;
            guide = Object.FindFirstObjectByType<TutorialGuide>();
            Assert.That(guide.Progress.Lesson, Is.EqualTo(TutorialLesson.Welcome));
            Assert.That(Object.FindFirstObjectByType<GameManager>().LevelTimer, Is.EqualTo(300f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator LeavingPracticeRestoresTimeAndNormalCampaignOrder()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            guide.MainMenu(); yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            int tutorial = SceneUtility.GetBuildIndexByScenePath(TutorialScene.Path);
            int level1 = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Level_01.unity");
            int level3 = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Level_03.unity");
            Assert.That(tutorial, Is.EqualTo(1));
            Assert.That(level1, Is.EqualTo(2));
            // The test runner adds its own temporary scene after authored levels.
            Assert.That(level3, Is.EqualTo(level1 + 2));
            Assert.That(tutorial, Is.LessThan(level1));
        }

        private static IEnumerator Load(string name)
        {
            Time.timeScale = 1f; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(name);
            yield return null;
        }
        private static void Begin(TutorialGuide guide) { guide.BeginPractice(); Dismiss(guide); }
        private static void Dismiss(TutorialGuide guide)
        { for (int i = 0; i < 5 && guide.IsCoaching; i++) guide.ContinueCoaching(); }
        private static void Possess(TutorialGuide guide, EnemyBase body)
        { Invoke(guide.Possession, "Possess", body); Dismiss(guide); }
        private static void Set(object item, string name, object value) => item.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
        private static void Invoke(object item, string name, params object[] args) => item.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, args);
    }
}
