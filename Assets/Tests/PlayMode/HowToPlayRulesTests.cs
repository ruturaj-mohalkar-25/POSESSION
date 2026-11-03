using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class HowToPlayRulesTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            var active = SceneManager.GetActiveScene();
            if (active.name == TutorialScene.Name)
            {
                var empty = SceneManager.CreateScene("HowToPlayCleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(active);
            }
        }

        [UnityTest]
        public IEnumerator TutorialTeachesNewRulesWithoutBlockingMidGamePopups()
        {
            yield return SceneManager.LoadSceneAsync(TutorialScene.Name);
            yield return null;

            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.Charges, Is.Not.Null);
            Assert.That(guide.Charges.Count, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Length,
                Is.GreaterThanOrEqualTo(2));

            guide.BeginPractice();
            Assert.That(guide.IsCoaching, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(guide.Spirit.enabled, Is.True);
            Assert.That(guide.Possession.enabled, Is.True);
            yield return null; // Let the overlay refresh after leaving the welcome state.

            guide.Progress.Move(TutorialMovement.All);
            StringAssert.Contains("cone", guide.Instruction.ToLowerInvariant());
            StringAssert.Contains("behind", guide.Instruction.ToLowerInvariant());

            var modal = guide.GetComponent<TutorialOverlay>().GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "PracticeModal");
            Assert.That(modal.gameObject.activeInHierarchy, Is.False,
                "The welcome card should close without another blocking tutorial popup.");
        }
    }
}
