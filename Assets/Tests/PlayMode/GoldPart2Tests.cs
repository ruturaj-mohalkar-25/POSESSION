using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Tests
{
    public class GoldPart2Tests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            var roots = new System.Collections.Generic.List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("GoldPart2Test")) roots.Add(go);
            foreach (var root in roots)
                if (root != null) Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator GuardLookAddsCapWithoutAddingColliders()
        {
            var guard = Enemy<GuardEnemy>("GoldPart2Test Guard");
            int before = guard.GetComponentsInChildren<Collider>(true).Length;
            var look = guard.gameObject.AddComponent<GoldEnemyPresentation>();
            yield return null;

            Assert.That(look.IsBuilt, Is.True);
            Assert.That(look.VisualPieceCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(guard.transform.Find("Guard Cap"), Is.Not.Null);
            Assert.That(guard.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator HeavyLookHasAReadableSilhouetteWithoutAddingColliders()
        {
            var heavy = Enemy<HeavyEnemy>("GoldPart2Test Heavy");
            int before = heavy.GetComponentsInChildren<Collider>(true).Length;
            var look = heavy.gameObject.AddComponent<GoldEnemyPresentation>();
            yield return null;

            Assert.That(look.VisualPieceCount, Is.GreaterThanOrEqualTo(5));
            Assert.That(heavy.transform.Find("Heavy Left Shoulder"), Is.Not.Null);
            Assert.That(heavy.transform.Find("Heavy Right Shoulder"), Is.Not.Null);
            Assert.That(heavy.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator PauseOverlayUsesResponsiveAnchorsAndRestoresTime()
        {
            var root = new GameObject("GoldPart2Test Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var presentation = root.AddComponent<GoldEndScreenPresentation>();
            presentation.Apply();
            yield return null;

            Assert.That(presentation.PausePanel, Is.Not.Null);
            var rect = presentation.PausePanel.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));

            presentation.Pause();
            Assert.That(presentation.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            presentation.Resume();
            Assert.That(presentation.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator EndScreenUsesSharedGoldThemeAndPreservesButtonOrder()
        {
            var root = new GameObject("GoldPart2Test End Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            var title = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            title.transform.SetParent(panel.transform, false);
            title.text = "GAME OVER";

            CreateButton(panel.transform, "RestartButton", "RESTART");
            CreateButton(panel.transform, "MenuButton", "MAIN MENU");

            var presentation = root.AddComponent<GoldEndScreenPresentation>();
            presentation.Apply();
            yield return null;

            Assert.That(panel.GetComponent<Image>().color, Is.EqualTo(GoldTheme.Background));
            Assert.That(title.color, Is.EqualTo(GoldTheme.Teal));
            Assert.That(panel.transform.Find("Gold End Card"), Is.Not.Null);

            var buttons = panel.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(2));
            Assert.That(buttons[0].name, Is.EqualTo("RestartButton"));
            Assert.That(buttons[1].name, Is.EqualTo("MenuButton"));
            Assert.That(buttons[0].colors.normalColor, Is.EqualTo(GoldTheme.Teal));
            Assert.That(buttons[1].colors.normalColor, Is.EqualTo(GoldTheme.Secondary));
        }

        private static T Enemy<T>(string name) where T : EnemyBase
        {
            var go = new GameObject(name, typeof(CharacterController));
            return go.AddComponent<T>();
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.targetGraphic = button.GetComponent<Image>();

            var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(button.transform, false);
            text.text = label;
            return button;
        }
    }
}
