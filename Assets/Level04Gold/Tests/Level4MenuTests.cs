using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4MenuTests
    {
        private const string ProgressKey = "Possession.LevelsUnlocked";
        private bool hadSavedProgress;
        private int savedProgress;

        [SetUp]
        public void Setup()
        {
            hadSavedProgress = PlayerPrefs.HasKey(ProgressKey);
            savedProgress = PlayerPrefs.GetInt(ProgressKey);
            CampaignProgress.Reset();
            Time.captureFramerate = 60;
            Time.timeScale = 1f;
            EventBus.Clear();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (hadSavedProgress) PlayerPrefs.SetInt(ProgressKey, savedProgress);
            else PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Level4MenuCleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator SavedPlayAndFourthLevelButtonsLoadTheSavedLevelAndRestartAndMenuWork()
        {
            CampaignProgress.Set(CampaignLevels.PlayableSceneNames.Length);
            yield return Load("MainMenu");
            var menu = All<MainMenuController>().Single();
            var selection = Read<GameObject>(menu, "levelSelectPanel");
            var main = Read<GameObject>(menu, "mainButtons");
            Assert.That(main.activeInHierarchy, Is.True);
            Assert.That(selection.activeSelf, Is.False);
            var play = BoundButton(menu.transform, menu, nameof(MainMenuController.OnPlayButton));
            Assert.That(play.interactable && play.gameObject.activeInHierarchy, Is.True);
            play.onClick.Invoke();
            Assert.That(selection.activeInHierarchy, Is.True);
            Assert.That(main.activeSelf, Is.False);
            var buttons = Read<Button[]>(menu, "levelButtons");
            Assert.That(buttons.Length, Is.EqualTo(4), "The saved Play section must expose all four stages.");
            for (int i = 0; i < buttons.Length; i++)
            {
                Assert.That(buttons[i].interactable && buttons[i].gameObject.activeInHierarchy, Is.True);
                Assert.That(buttons[i].GetComponentInChildren<Text>().text, Is.EqualTo("LEVEL " + (i + 1)));
            }
            buttons[3].onClick.Invoke();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "A card selects; the Play button launches.");
            menu.OnPlaySelectedLevel();
            yield return AwaitScene(CampaignLevels.FourthScene);
            AssertSavedLevelFourIsReady();

            var target = All<Target>().OrderBy(t => t.name).First();
            string targetName = target.name;
            int originalTargetId = target.GetInstanceID();
            target.OnHit();
            Assert.That(target.IsHit, Is.True);
            EventBus.LevelFailed?.Invoke();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Failed));
            Assert.That(Time.timeScale, Is.Zero);
            var hud = All<HUDManager>().Single();
            var gameOver = Read<GameObject>(hud, "gameOverPanel");
            Assert.That(gameOver.activeInHierarchy, Is.True);
            BoundButton(gameOver.transform, hud, nameof(HUDManager.OnRestartButton)).onClick.Invoke();
            yield return AwaitScene(CampaignLevels.FourthScene);
            AssertSavedLevelFourIsReady();
            var restored = All<Target>().Single(t => t.name == targetName);
            Assert.That(restored.GetInstanceID(), Is.Not.EqualTo(originalTargetId));
            Assert.That(restored.IsHit, Is.False, "Restart must reload the saved puzzle, not keep the previous attempt.");

            EventBus.LevelFailed?.Invoke();
            hud = All<HUDManager>().Single();
            gameOver = Read<GameObject>(hud, "gameOverPanel");
            BoundButton(gameOver.transform, hud, nameof(HUDManager.OnQuitButton)).onClick.Invoke();
            yield return AwaitScene("MainMenu");
            menu = All<MainMenuController>().Single();
            Assert.That(Read<GameObject>(menu, "mainButtons").activeInHierarchy, Is.True);
            Assert.That(Read<GameObject>(menu, "levelSelectPanel").activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FreshProfileLocksFourthUntilThirdCompletionThenTheSavedMenuLoadsIt()
        {
            yield return Load("MainMenu");
            var menu = All<MainMenuController>().Single();
            menu.OnPlayButton();
            var buttons = Read<Button[]>(menu, "levelButtons");
            Assert.That(buttons.Length, Is.EqualTo(4));
            Assert.That(buttons[0].interactable, Is.True);
            for (int i = 1; i < buttons.Length; i++)
                Assert.That(buttons[i].interactable, Is.False, "An unearned stage must stay locked.");
            Assert.That(buttons[3].GetComponentsInChildren<Text>().Any(t => t.text == "LOCKED"), Is.True);
            // UnityEvent.Invoke bypasses the disabled Button, so the controller must also reject it.
            buttons[3].onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));

            yield return Load(CampaignLevels.ThirdScene);
            EventBus.LevelCompleted?.Invoke();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
            Assert.That(CampaignProgress.Unlocked, Is.EqualTo(4));
            var hud = All<HUDManager>().Single();
            var complete = Read<GameObject>(hud, "levelCompletePanel");
            BoundButton(complete.transform, hud, nameof(HUDManager.OnQuitButton)).onClick.Invoke();
            yield return AwaitScene("MainMenu");
            menu = All<MainMenuController>().Single();
            menu.OnPlayButton();
            buttons = Read<Button[]>(menu, "levelButtons");
            Assert.That(buttons[3].interactable, Is.True);
            Assert.That(buttons[3].GetComponentInChildren<Text>().text, Is.EqualTo("LEVEL 4"));
            buttons[3].onClick.Invoke();
            menu.OnPlaySelectedLevel();
            yield return AwaitScene(CampaignLevels.FourthScene);
            AssertSavedLevelFourIsReady();
        }

        [UnityTest]
        public IEnumerator ThirdLevelNextButtonAdvancesToFourthInsteadOfFinishingTheCampaign()
        {
            yield return Load(CampaignLevels.ThirdScene);
            Assert.That(GameManager.Instance.IsLastLevel, Is.False);
            Assert.That(CampaignLevels.NextScene(CampaignLevels.ThirdScene), Is.EqualTo(CampaignLevels.FourthScene));
            EventBus.LevelCompleted?.Invoke();
            Assert.That(Time.timeScale, Is.Zero);
            var hud = All<HUDManager>().Single();
            var next = Read<Text>(hud, "nextButtonLabel");
            Assert.That(next.text, Is.EqualTo("NEXT LEVEL"));
            var button = next.GetComponentInParent<Button>();
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
            button.onClick.Invoke();
            yield return AwaitScene(CampaignLevels.FourthScene);
            AssertSavedLevelFourIsReady();
            Assert.That(GameManager.Instance.IsLastLevel, Is.True);
        }

        [UnityTest]
        public IEnumerator FourthVaultCompletionShowsFinishAndGameCompleteCanReplayTheFirstStage()
        {
            yield return Load(CampaignLevels.FourthScene);
            Assert.That(GameManager.Instance.IsLastLevel, Is.True);
            Assert.That(CampaignLevels.NextScene(CampaignLevels.FourthScene), Is.Null);
            var spirit = All<SpiritController>().Single();
            var player = spirit.GetComponent<PossessionController>();
            var engineer = All<EngineerEnemy>().Single(e => e.name == "Engineer_Archive");
            var cipher = All<Level4Cipher>().Single();
            var near = cipher.RecoveryPoint.position + Vector3.left * 1.5f;
            Position(engineer, near, Vector3.right);
            spirit.RestoreAt(near + Vector3.left * 2.5f);
            engineer.enabled = true;
            yield return null;
            yield return null;
            Assert.That(player.TryPossess(engineer), Is.True);
            Assert.That(cipher.TryRecover(engineer.transform), Is.True);
            var vault = All<Level4Vault>().Single();
            Position(engineer, vault.OperatorPoint.position, Vector3.forward);
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            vault.RotateDial(-120f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(vault.TryOpenHandle(), Is.True);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(1.2f);
            var hud = All<HUDManager>().Single();
            var complete = Read<GameObject>(hud, "levelCompletePanel");
            var gameComplete = Read<GameObject>(hud, "gameCompletePanel");
            var next = Read<Text>(hud, "nextButtonLabel");
            Assert.That(complete.activeInHierarchy, Is.True);
            Assert.That(complete.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(gameComplete.activeSelf, Is.False);
            Assert.That(next.text, Is.EqualTo("FINISH"));
            int finishEvents = 0;
            EventBus.GameComplete += () => finishEvents++;
            next.GetComponentInParent<Button>().onClick.Invoke();
            Assert.That(finishEvents, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(CampaignLevels.FourthScene));
            Assert.That(complete.activeSelf, Is.False);
            Assert.That(gameComplete.activeInHierarchy, Is.True);
            BoundButton(gameComplete.transform, hud, nameof(HUDManager.OnPlayAgainButton)).onClick.Invoke();
            yield return AwaitScene(CampaignLevels.FirstScene);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            Assert.That(GameManager.Instance.IsLastLevel, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static void AssertSavedLevelFourIsReady()
        {
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(CampaignLevels.FourthScenePath));
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(All<HUDManager>().Length, Is.EqualTo(1));
            Assert.That(All<Level4WorldPresentation>().Single().IsApplied, Is.True);
            Assert.That(All<GoldFloorSurface>().Length, Is.EqualTo(2), "Campaign navigation must keep the two-storey adapter.");
            Assert.That(All<SpiritController>().Single().GetComponent<PossessionCharge>().Count, Is.EqualTo(1));
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(scene);
            yield return AwaitScene(scene);
        }

        private static IEnumerator AwaitScene(string expected)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            // A synchronous LoadScene requested by a button takes effect on the
            // next frame, including when the destination is the same scene.
            yield return null;
            while (SceneManager.GetActiveScene().name != expected && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(expected));
            foreach (var reporter in All<FirebaseReporter>()) Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in All<P5TimingTracker>()) tracker.PersistReports = false;
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var attack in All<EnemyContactDamage>()) attack.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            yield return null;
            yield return null;
        }

        private static Button BoundButton(Transform parent, Object target, string method)
        {
            var matches = parent.GetComponentsInChildren<Button>(true).Where(button =>
                Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(i =>
                    button.onClick.GetPersistentTarget(i) == target && button.onClick.GetPersistentMethodName(i) == method)).ToArray();
            Assert.That(matches.Length, Is.EqualTo(1), "The saved UI must wire exactly one " + method + " button in this panel.");
            Assert.That(matches[0].interactable && matches[0].gameObject.activeInHierarchy, Is.True);
            return matches[0];
        }

        private static void Position(EnemyBase enemy, Vector3 position, Vector3 forward)
        {
            var cc = enemy.GetComponent<CharacterController>();
            bool enabled = cc.enabled;
            cc.enabled = false;
            enemy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            cc.enabled = enabled;
            Physics.SyncTransforms();
        }

        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static T Read<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
