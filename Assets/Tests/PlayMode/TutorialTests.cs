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
        public IEnumerator TutorialAutoStartsWithoutAModalOrPausedCoaching()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            var manager = Object.FindFirstObjectByType<GameManager>();
            var overlay = Object.FindFirstObjectByType<TutorialOverlay>();
            Assert.That(guide.Progress.Started, Is.True);
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(guide.Possession.enabled, Is.True);
            Assert.That(Object.FindFirstObjectByType<TutorialCoach>(), Is.Null);
            Assert.That(overlay.GetComponentsInChildren<Transform>(true)
                .Any(t => t.name == "StartPractice" || t.name == "PracticeModal" || t.name == "CoachCard"), Is.False);
            var objectiveBars = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Where(t => t.name == "ObjectiveBar").ToArray();
            Assert.That(objectiveBars, Has.Length.EqualTo(1),
                "The tutorial should reuse the campaign objective bar instead of adding another HUD style.");
            Assert.That(objectiveBars[0].gameObject.activeInHierarchy, Is.True);
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(t => t.name == "ObjectiveText").text, Is.EqualTo(guide.Title));
            float remaining = manager.LevelTimer;
            yield return new WaitForSeconds(0.1f);
            Assert.That(manager.LevelTimer, Is.LessThan(remaining));
            Assert.That(guide.Possession.PossessDuration, Is.EqualTo(12f));
            var chargeHud = guide.Spirit.GetComponent<ChargeHUD>();
            Assert.That(chargeHud, Is.Not.Null);
            Assert.That(chargeHud.enabled, Is.True,
                "The tutorial should use the same charge banner as the campaign levels.");
            var hud = Object.FindFirstObjectByType<HUDManager>();
            Assert.That(Get<GameObject>(hud, "possessPrompt"), Is.Not.Null,
                "The tutorial should retain the campaign's possession prompt.");
            Assert.That(Object.FindObjectsByType<WorldLabel>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Any(label => label.gameObject.activeSelf), Is.True,
                "The tutorial should retain the same enemy labels used in the campaign.");
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
            yield return new WaitForSeconds(0.25f);
            Assert.That(guide.Guard.IsHostile, Is.False);
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
            Assert.That(Object.FindFirstObjectByType<GameManager>().CurrentState, Is.EqualTo(LevelState.Complete));
            yield return null;
            Assert.That(Object.FindFirstObjectByType<TutorialOverlay>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ForcedGuardCanDamageTheSpiritUsingItsSharedAttack()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>(); Begin(guide);
            var pc = guide.Possession;
            Set(pc, "possessDuration", 0.05f);
            Set(pc, "hostileTime", 4f);
            Possess(guide, guide.Guard);
            yield return new WaitForSeconds(0.1f);
            var spirit = pc.GetComponent<SpiritController>();
            spirit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            Assert.That(guide.Guard.IsHostile, Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spirit.MaxHP), "Ejection grants two seconds to escape.");
            // The first two shots can land within the two-second shield; wait for the next normal shot.
            yield return new WaitForSeconds(2.4f);
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
            Assert.That(guide.Progress.Started, Is.True);
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Object.FindFirstObjectByType<GameManager>().LevelTimer, Is.EqualTo(150f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator TutorialUsesAShortTwoBayLayoutWithProductionElementsOnly()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            Assert.That(GameObject.Find("CompactTutorialLayout"), Is.Not.Null);
            Assert.That(GameObject.Find("Bay1_Floor"), Is.Not.Null);
            Assert.That(GameObject.Find("Bay2_Floor"), Is.Not.Null);
            Assert.That(GameObject.Find("Room1_Floor"), Is.Null);
            Assert.That(GameObject.Find("Room2_Floor"), Is.Null);
            Assert.That(GameObject.Find("Room3_Floor"), Is.Null);
            Assert.That(GameObject.Find("SpiritOnlyGate"), Is.Null,
                "A tutorial-only gate would teach a visual rule that the campaign never uses.");
            Assert.That(Vector3.Distance(guide.Guard.transform.position, guide.Heavy.transform.position), Is.LessThan(12f));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Not.Empty,
                "The production charge pickup itself should teach the player to return to Spirit form.");
        }

        [UnityTest]
        public IEnumerator DeathIsPartOfPracticeAndQueuesAFastRetry()
        {
            yield return Load(TutorialScene.Name);
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            guide.Spirit.TakeDamage(guide.Spirit.MaxHP);
            yield return null;
            Assert.That(guide.Progress.Failed, Is.True);
            Assert.That(guide.FailureMessage, Does.Contain("CAUGHT"));
            var overlay = Object.FindFirstObjectByType<TutorialOverlay>();
            Assert.That(overlay.GetComponentsInChildren<Text>(true).Single(t => t.name == "ResultBody").text,
                Is.EqualTo("Restarting…"));
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
            int level1 = SceneUtility.GetBuildIndexByScenePath(CampaignLevels.ScenePaths[0]);
            int level2 = SceneUtility.GetBuildIndexByScenePath(CampaignLevels.ScenePaths[1]);
            int level3 = SceneUtility.GetBuildIndexByScenePath(CampaignLevels.ScenePaths[2]);
            Assert.That(tutorial, Is.EqualTo(1));
            Assert.That(level1, Is.EqualTo(2));
            Assert.That(level2, Is.EqualTo(level1 + 1));
            Assert.That(level3, Is.EqualTo(level2 + 1));
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
        {
            Dismiss(guide);
            var charge=guide.Spirit.GetComponent<PossessionCharge>();
            if(charge!=null&&!charge.HasCharge)charge.TryCollect();
            // This sequence isolates lesson transitions; cooldown duration has its own cross-level check.
            Set(guide.Possession,"ejectCooldownTimer",0f);
            guide.Spirit.RestoreAt(body.transform.position-body.transform.forward*2f);
            Physics.SyncTransforms();
            Assert.That(guide.Possession.TryPossess(body),Is.True,body.name);
            Dismiss(guide);
        }
        private static void Set(object item, string name, object value) => item.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
        private static T Get<T>(object item, string name) => (T)item.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);
        private static void Invoke(object item, string name, params object[] args) => item.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, args);
    }
}
