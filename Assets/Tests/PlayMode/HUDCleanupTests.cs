using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Tests
{
    public class HUDCleanupTests
    {
        private Scene levelScene;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            if (levelScene.IsValid() && levelScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(levelScene);
        }

        [UnityTest]
        public IEnumerator Level01UsesCleanCompactHud()
        {
            yield return CheckLevel("Level_01");
        }

        [UnityTest]
        public IEnumerator Level02UsesCleanCompactHud()
        {
            yield return CheckLevel("Level_02");
        }

        [UnityTest]
        public IEnumerator Level03UsesCleanCompactHud()
        {
            yield return CheckLevel("Level_03");
        }

        [UnityTest]
        public IEnumerator HudUsesShortCenteredSemanticStatusMessages()
        {
            yield return Load("Level_01");
            var hud = FindInLevel<HUDManager>();
            Assert.That(hud, Is.Not.Null);
            var message = Read<Text>(hud, "messageText");

            EventBus.DoorOpened?.Invoke(null);
            yield return null;
            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(message.text, Is.EqualTo("DOOR OPEN"));
            Assert.That(message.color.g, Is.GreaterThan(message.color.r));

            EventBus.Ejected?.Invoke(null, true);
            yield return null;
            Assert.That(message.text, Is.EqualTo("FORCED OUT"));
            Assert.That(message.color.r, Is.GreaterThan(message.color.g));
        }

        private IEnumerator CheckLevel(string sceneName)
        {
            yield return Load(sceneName);
            var hud = FindInLevel<HUDManager>();
            Assert.That(hud, Is.Not.Null);

            Transform controlsPanel = hud.transform.Find("ControlsPanel");
            Assert.That(controlsPanel == null || !controlsPanel.gameObject.activeSelf, Is.True,
                sceneName + " still shows the removed controls panel.");
            foreach (Text text in hud.GetComponentsInChildren<Text>(true))
                Assert.That(text.text.StartsWith("Form:") && text.gameObject.activeSelf, Is.False,
                    sceneName + " still contains the removed form label.");

            var healthText = Read<Text>(hud, "healthText");
            var healthFill = Read<Image>(hud, "healthFill");
            var timerText = Read<Text>(hud, "levelTimerText");
            AssertTopLeftCompact(healthText.rectTransform, 320f);
            AssertTopLeftCompact(healthFill.rectTransform.parent as RectTransform, 320f);
            AssertTopLeftCompact(timerText.rectTransform, 320f);

            var objective = Read<Text>(hud, "objectiveText");
            var objectiveBar = objective.transform.parent.GetComponent<Image>();
            Assert.That(objective.text.Length, Is.LessThan(50));
            Assert.That(objective.text, Does.Not.Contain("unlock the door").IgnoreCase);
            Assert.That(objective.text, Does.Not.Contain("BREAKABLE").IgnoreCase);
            Assert.That(objectiveBar.color.r, Is.GreaterThan(0.8f));
            Assert.That(objectiveBar.color.g, Is.GreaterThan(0.55f));
            Assert.That(objectiveBar.color.b, Is.LessThan(0.4f));

            var message = Read<Text>(hud, "messageText");
            Assert.That(message.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(message.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));

            AssertWhite(Read<Color>(hud, "possessBarColor"));
            AssertGreen(Read<Color>(hud, "healthMid"));
            AssertGreen(Read<Color>(hud, "successColor"));
            AssertRed(Read<Color>(hud, "alertColor"));
            var possessPrompt = Read<GameObject>(hud, "possessPrompt");
            AssertGreen(possessPrompt.GetComponent<Text>().color);
            Color objectiveColor = Read<Color>(hud, "objectiveColor");
            Assert.That(objectiveColor.r, Is.GreaterThan(0.8f));
            Assert.That(objectiveColor.g, Is.GreaterThan(0.55f));
            Assert.That(objectiveColor.b, Is.LessThan(0.4f));
        }

        private IEnumerator Load(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            levelScene = SceneManager.GetSceneByName(sceneName);
            yield return null;
        }

        private T FindInLevel<T>() where T : Component
        {
            foreach (GameObject root in levelScene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static void AssertTopLeftCompact(RectTransform rect, float maxWidth)
        {
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.sizeDelta.x, Is.LessThanOrEqualTo(maxWidth));
        }

        private static void AssertWhite(Color color)
        {
            Assert.That(color.r, Is.GreaterThan(0.9f));
            Assert.That(color.g, Is.GreaterThan(0.9f));
            Assert.That(color.b, Is.GreaterThan(0.9f));
        }

        private static void AssertGreen(Color color)
        {
            Assert.That(color.g, Is.GreaterThan(color.r));
            Assert.That(color.g, Is.GreaterThan(color.b));
        }

        private static void AssertRed(Color color)
        {
            Assert.That(color.r, Is.GreaterThan(color.g));
            Assert.That(color.r, Is.GreaterThan(color.b));
        }

        private static T Read<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized field " + fieldName);
            return (T)field.GetValue(target);
        }
    }
}
