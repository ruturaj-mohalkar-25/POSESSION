using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class P5FloorTextTests
    {
        [UnityTest]
        public IEnumerator CleanupHidesOnlyLegacyFloorTextAndPreservesObjectLabelsAndPhysics()
        {
            Scene scene = SceneManager.CreateScene("P5 floor cleanup");
            var instructions = new GameObject("StartInstructions", typeof(TextMesh), typeof(BoxCollider));
            var sign = new GameObject("Sign_Door", typeof(TextMesh));
            var label = new GameObject("Label_Door", typeof(TextMesh));
            var protectedLabel = new GameObject("Sign_Object", typeof(TextMesh), typeof(WorldLabel));
            var geometry = new GameObject("Sign_SolidGeometry", typeof(BoxCollider));
            foreach (var go in new[] { instructions, sign, label, protectedLabel, geometry })
                SceneManager.MoveGameObjectToScene(go, scene);
            // The legacy text objects are decorative, but unrelated objects must never be removed.
            Assert.That(LegacyFloorTextCleanup.Apply(scene), Is.EqualTo(2));
            Assert.That(LegacyFloorTextCleanup.Apply(scene), Is.Zero);
            Assert.That(instructions.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(sign.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(instructions.GetComponent<Collider>().enabled, Is.True);
            Assert.That(instructions.activeInHierarchy, Is.True);
            Assert.That(label.activeSelf, Is.True);
            Assert.That(protectedLabel.activeSelf, Is.True);
            Assert.That(geometry.activeSelf, Is.True);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ExistingLevelScenesLoadWithoutActiveFloorInstructions()
        {
            foreach (string name in new[] { "Level_01", "Level_02", "Level_03" })
            {
                yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                Scene scene = SceneManager.GetSceneByName(name);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
                    if ((text.name == "StartInstructions" || text.name.StartsWith("Sign_"))
                        && text.GetComponent<WorldLabel>() == null)
                        Assert.That(text.gameObject.activeInHierarchy && text.GetComponent<MeshRenderer>().enabled,
                            Is.False, name + ": " + text.name);
                var manager = Object.FindFirstObjectByType<GameManager>();
                Assert.That(manager, Is.Not.Null);
                Assert.That(manager.GetComponent<P5TimingTracker>(), Is.Not.Null);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
