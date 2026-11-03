using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class P4CameraTutorialTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var loaded = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("P4CameraTutorialCleanup"));
            yield return SceneManager.UnloadSceneAsync(loaded);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator LevelOneUsesSharedCameraAndHasReachableChargePlan()
        {
            yield return SceneManager.LoadSceneAsync("Level_01");
            yield return null;

            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.fieldOfView, Is.EqualTo(CameraFollow.SharedFieldOfView).Within(0.01f));
            Assert.That(Quaternion.Angle(camera.transform.rotation,
                Quaternion.Euler(CameraFollow.SharedEuler)), Is.LessThan(0.1f));

            var pickups = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None);
            Assert.That(pickups.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(pickups.Any(p => p.transform.position.z < 24f), Is.True,
                "The required shard must be reachable before the locked door.");
            Assert.That(pickups.Any(p => p.transform.position.z > 24f && p.transform.position.z < 33f), Is.True,
                "The spare shard must be available before the Heavy's breakable-wall gate.");
            Assert.That(Object.FindFirstObjectByType<SpiritController>().GetComponent<PossessionCharge>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TutorialExplainsConesChargesAndCollectibles()
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
            while (guide.IsCoaching) guide.ContinueCoaching();
            guide.Progress.Move(TutorialMovement.All);
            StringAssert.Contains("cone", guide.Instruction.ToLowerInvariant());
            StringAssert.Contains("behind", guide.Instruction.ToLowerInvariant());
        }
    }
}
