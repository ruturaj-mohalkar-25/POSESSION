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
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name.StartsWith("GoldPart2Test")) Object.DestroyImmediate(go);
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

        private static T Enemy<T>(string name) where T : EnemyBase
        {
            var go = new GameObject(name, typeof(CharacterController));
            return go.AddComponent<T>();
        }
    }
}
