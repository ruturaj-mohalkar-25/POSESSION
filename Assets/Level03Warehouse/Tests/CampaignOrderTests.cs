#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Warehouse.Tests
{
    public sealed class CampaignOrderTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            if(!CampaignLevels.PlayableSceneNames.Contains(old.name)&&old.name!="MainMenu"&&old.name!=TutorialScene.Name){EventBus.Clear();yield break;}
            SceneManager.SetActiveScene(SceneManager.CreateScene("CampaignCleanup"));
            if(old.IsValid()&&old.isLoaded)yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [Test] public void FourPlayableStagesPreserveTheFoundationOrderAndBuildEntries()
        {
            Assert.That(CampaignLevels.SceneNames,Is.EqualTo(new[]{"Level_03","Level_02","Level_03_Warehouse"}));
            Assert.That(CampaignLevels.PlayableSceneNames,Is.EqualTo(new[]{"Level_03","Level_02","Level_03_Warehouse","Level_04_Gold"}));
            Assert.That(CampaignLevels.PlayableScenePaths,Is.EqualTo(CampaignLevels.ScenePaths.Concat(new[]{CampaignLevels.FourthScenePath})));
            Assert.That(CampaignLevels.NumberFor("Level_01"),Is.Zero,"Original intro is no longer a playable campaign stage.");
            for(int i=0;i<4;i++)Assert.That(CampaignLevels.NumberFor(CampaignLevels.PlayableSceneNames[i]),Is.EqualTo(i+1));
            Assert.That(CampaignLevels.NextScene(CampaignLevels.ThirdScene),Is.EqualTo(CampaignLevels.FourthScene));
            Assert.That(CampaignLevels.NextScene(CampaignLevels.FourthScene),Is.Null);
            Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Take(5).Select(s=>s.path),Is.EqualTo(
                new[]{"Assets/Scenes/MainMenu.unity",TutorialScene.Path}.Concat(CampaignLevels.ScenePaths)));
            Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path),
                Is.SupersetOf(CampaignLevels.PlayableScenePaths),"Every selectable stage must be included without requiring the fixture scene to move.");
        }
        [UnityTest] public IEnumerator AllFourMenuButtonsStartTheirDisplayedStages()
        {
            for(int selected=0;selected<4;selected++)
            {
                yield return Load("MainMenu");
                var menu=Object.FindFirstObjectByType<MainMenuController>();menu.OnPlayButton();
                var buttons=(Button[])typeof(MainMenuController).GetField("levelButtons",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
                Assert.That(buttons.Length,Is.EqualTo(4));
                for(int i=0;i<4;i++)
                {
                    Assert.That(buttons[i].interactable,Is.True);
                    Assert.That(buttons[i].GetComponentInChildren<Text>().text,Is.EqualTo("LEVEL "+(i+1)));
                }
                buttons[selected].onClick.Invoke();yield return null;yield return null;
                SuppressReports();
                Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.PlayableSceneNames[selected]));
                Assert.That(GameManager.Instance.IsLastLevel,Is.EqualTo(selected==3));
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                Assert.That(spirit.GetComponents<Component>(),Is.All.Not.Null,"Player components must resolve after the charge-system merge.");
                Assert.That(spirit.GetComponents<PossessionCharge>().Length,Is.EqualTo(1));
            }
        }
        [UnityTest] public IEnumerator NextContinuesThroughWarehouseToLevelFourAndFinishReplaysLevelOne()
        {
            yield return Load(CampaignLevels.FirstScene);
            GameManager.Instance.LoadNextLevel();yield return null;yield return null;
            SuppressReports();
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.SecondScene));
            Assert.That(Object.FindFirstObjectByType<Level02Director>(),Is.Not.Null);
            GameManager.Instance.LoadNextLevel();yield return null;yield return null;
            SuppressReports();
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.ThirdScene));
            Assert.That(GameManager.Instance.IsLastLevel,Is.False);
            var level=Object.FindFirstObjectByType<WarehouseLevel>();
            foreach(var key in new[]{"inspection","exit-passage"})
                foreach(var target in GameObject.Find(key+"_Door").GetComponent<WarehouseDoorView>().Targets)target.OnHit();
            level.Spirit.RestoreAt(level.transform.position);Physics.SyncTransforms();
            Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);
            var hud=Object.FindFirstObjectByType<HUDManager>();
            var label=(Text)typeof(HUDManager).GetField("nextButtonLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            Assert.That(label.text,Is.EqualTo("NEXT LEVEL"));
            label.GetComponentInParent<Button>().onClick.Invoke();yield return null;yield return null;
            SuppressReports();
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.FourthScene));
            Assert.That(GameManager.Instance.IsLastLevel,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1f));
            hud=Object.FindFirstObjectByType<HUDManager>();
            // The vault has its own completion tests; here the real shared event
            // checks the final-stage results button and campaign navigation.
            EventBus.LevelCompleted?.Invoke();
            label=(Text)typeof(HUDManager).GetField("nextButtonLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            Assert.That(label.text,Is.EqualTo("FINISH"));int finished=0;EventBus.GameComplete+=()=>finished++;
            label.GetComponentInParent<Button>().onClick.Invoke();
            Assert.That(finished,Is.EqualTo(1));
            Assert.That(((GameObject)typeof(HUDManager).GetField("gameCompletePanel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud)).activeSelf,Is.True);
            hud.OnPlayAgainButton();yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.FirstScene));
            Assert.That(Time.timeScale,Is.EqualTo(1f));
        }
        [UnityTest] public IEnumerator TutorialPlayLevelOneStartsThePromotedFirstStage()
        {
            yield return Load(TutorialScene.Name);
            Object.FindFirstObjectByType<TutorialGuide>().PlayLevelOne();yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.FirstScene));
            Assert.That(Time.timeScale,Is.EqualTo(1f));
        }
        private static IEnumerator Load(string name)
        {Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(name);yield return null;SuppressReports();}
        private static void SuppressReports()
        {
            foreach(var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(reporter.gameObject);
            foreach(var tracker in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None))tracker.PersistReports=false;
        }
    }
}
#endif
