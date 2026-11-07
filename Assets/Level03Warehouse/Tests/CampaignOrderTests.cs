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
            if(!CampaignLevels.SceneNames.Contains(old.name)&&old.name!="MainMenu"&&old.name!=TutorialScene.Name){EventBus.Clear();yield break;}
            SceneManager.SetActiveScene(SceneManager.CreateScene("CampaignCleanup"));
            if(old.IsValid()&&old.isLoaded)yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [Test] public void ExactlyThreeMenuStagesFollowThePromotedOrderAndBuildList()
        {
            Assert.That(CampaignLevels.SceneNames,Is.EqualTo(new[]{"Level_03","Level_02","Level_03_Warehouse"}));
            Assert.That(CampaignLevels.NumberFor("Level_01"),Is.Zero,"Original intro is no longer a playable campaign stage.");
            for(int i=0;i<3;i++)Assert.That(CampaignLevels.NumberFor(CampaignLevels.SceneNames[i]),Is.EqualTo(i+1));
            Assert.That(CampaignLevels.NextScene(CampaignLevels.ThirdScene),Is.Null);
            Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Take(5).Select(s=>s.path),Is.EqualTo(
                new[]{"Assets/Scenes/MainMenu.unity",TutorialScene.Path}.Concat(CampaignLevels.ScenePaths)));
        }
        [UnityTest] public IEnumerator AllThreeMenuButtonsStartTheirPromotedStages()
        {
            for(int selected=0;selected<3;selected++)
            {
                yield return Load("MainMenu");
                var menu=Object.FindFirstObjectByType<MainMenuController>();menu.OnPlayButton();
                var buttons=(Button[])typeof(MainMenuController).GetField("levelButtons",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
                Assert.That(buttons.Length,Is.EqualTo(3));
                for(int i=0;i<3;i++)
                {
                    Assert.That(buttons[i].interactable,Is.True);
                    Assert.That(buttons[i].GetComponentInChildren<Text>().text,Is.EqualTo("LEVEL "+(i+1)));
                }
                buttons[selected].onClick.Invoke();yield return null;yield return null;
                Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.SceneNames[selected]));
                Assert.That(GameManager.Instance.IsLastLevel,Is.EqualTo(selected==2));
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                Assert.That(spirit.GetComponents<Component>(),Is.All.Not.Null,"Player components must resolve after the charge-system merge.");
                Assert.That(spirit.GetComponents<PossessionCharge>().Length,Is.EqualTo(1));
            }
        }
        [UnityTest] public IEnumerator NextLoadsHuntedThenWarehouseAndFinishReturnsPlayAgainToLevelOne()
        {
            yield return Load(CampaignLevels.FirstScene);
            GameManager.Instance.LoadNextLevel();yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.SecondScene));
            Assert.That(Object.FindFirstObjectByType<Level02Director>(),Is.Not.Null);
            GameManager.Instance.LoadNextLevel();yield return null;yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(CampaignLevels.ThirdScene));
            Assert.That(GameManager.Instance.IsLastLevel,Is.True);
            var level=Object.FindFirstObjectByType<WarehouseLevel>();
            foreach(var key in new[]{"inspection","exit-passage","extraction"})
                foreach(var target in GameObject.Find(key+"_Door").GetComponent<WarehouseDoorView>().Targets)target.OnHit();
            level.Spirit.RestoreAt(level.transform.position);Physics.SyncTransforms();
            Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);
            var hud=Object.FindFirstObjectByType<HUDManager>();
            var label=(Text)typeof(HUDManager).GetField("nextButtonLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
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
        {Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(name);yield return null;}
    }
}
#endif
