using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Tests
{
    public sealed class ReferenceMenuTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1f;
            var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("ReferenceMenuCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator MainActionsAndBackRemainVisibleAndReceiveRealPointerRaycasts()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;yield return null;
            var menu=Object.FindFirstObjectByType<MainMenuController>();
            foreach(string name in new[]{"PlayButton","AvatarButton","HowToPlayButton","CharactersButton"})AssertClickable(menu,name);
            menu.OnPlayButton();yield return null;Canvas.ForceUpdateCanvases();
            AssertClickable(menu,"BackButton");
            Assert.That(menu.GetComponentsInChildren<RawImage>().All(i=>i.texture!=null),Is.True);
            menu.OnBackButton();yield return null;Canvas.ForceUpdateCanvases();
            AssertClickable(menu,"PlayButton");
        }

        [UnityTest]
        public IEnumerator CharacterGuideShowsAllRolesAndReturnsToUsableHome()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null; yield return null;
            var menu=Object.FindFirstObjectByType<MainMenuController>();
            var entry=menu.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CharactersButton");
            entry.onClick.Invoke();
            yield return null;
            Assert.That(entry.gameObject.activeInHierarchy,Is.False);
            AssertClickable(menu,"CharacterGuideBackButton");
            foreach(string role in new[]{"GUARD","HEAVY","ENGINEER"})
            {
                var card=menu.GetComponentsInChildren<Transform>().Single(t=>t.name==role+" guide");
                Assert.That(card.GetComponentInChildren<RawImage>().texture,Is.Not.Null,role+" portrait");
                Assert.That(card.GetComponentsInChildren<Text>().Any(t=>t.name=="Instructions" && t.text.Length>0),Is.True);
            }
            menu.GetComponentsInChildren<Button>().Single(b=>b.name=="CharacterGuideBackButton").onClick.Invoke();
            yield return null;
            foreach(string name in new[]{"PlayButton","AvatarButton","HowToPlayButton","CharactersButton"})AssertClickable(menu,name);
            Assert.That(Object.FindFirstObjectByType<CharacterGuideView>(),Is.Not.Null);
        }

        private static void AssertClickable(MainMenuController menu,string name)
        {
            Canvas.ForceUpdateCanvases();
            var button=menu.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
            Assert.That(button.gameObject.activeInHierarchy && button.interactable,Is.True,name);
            Assert.That(button.GetComponentInChildren<Text>().gameObject.activeInHierarchy,Is.True,name+" label");
            var rect=(RectTransform)button.transform;
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits,Is.Not.Empty,name);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),name+" must not be covered by artwork or panels");
        }
    }
}
