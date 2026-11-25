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
            foreach(string name in new[]{"PlayButton","AvatarButton","HowToPlayButton"})AssertClickable(menu,name);
            menu.OnPlayButton();yield return null;Canvas.ForceUpdateCanvases();
            AssertClickable(menu,"BackButton");
            Assert.That(menu.GetComponentsInChildren<RawImage>().All(i=>i.texture!=null),Is.True);
            menu.OnBackButton();yield return null;Canvas.ForceUpdateCanvases();
            AssertClickable(menu,"PlayButton");
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
