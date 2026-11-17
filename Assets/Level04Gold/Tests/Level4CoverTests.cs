using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4CoverTests
    {
        private SpiritController spirit;
        private Transform lower;
        private Transform upper;

        [UnitySetUp]
        public IEnumerator LoadSavedLevel()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_04_Gold");
            foreach (var reporter in All<FirebaseReporter>()) Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in All<P5TimingTracker>()) tracker.PersistReports = false;
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var damage in All<EnemyContactDamage>()) damage.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            // Occlusion is an intentional see-through effect. Isolate the cover's base geometry/material here.
            foreach (var fader in All<WallOcclusionFader>()) { fader.RestoreAll(); fader.enabled = false; }
            spirit = All<SpiritController>().Single();
            var geometry = All<Transform>().Single(t => t.name == "Level04_Geometry");
            lower = geometry.Find("Lower floor");
            upper = geometry.Find("Upper floor");
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Level4CoverCleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [Test]
        public void EveryCoverCapClearsTheBodyTopWithoutChangingItsPhysicalBlocker()
        {
            Assert.That(All<Level4WorldPresentation>().Single().IsApplied, Is.True);
            foreach (var floor in new[] { lower, upper })
            {
                var bodies = Covers(floor);
                Assert.That(bodies.Length, Is.EqualTo(floor == lower ? 10 : 4));
                foreach (var body in bodies)
                {
                    var rim = floor.Find("Cover_Rim_" + body.name.Substring("Cover_".Length))
                        .GetComponent<MeshRenderer>();
                    float depthSeparation = rim.bounds.max.y - body.bounds.max.y;
                    Assert.That(depthSeparation, Is.InRange(.02f, .04f),
                        floor.name + "/" + body.name + " must not share its top depth with its metal cap.");
                    Assert.That(rim.bounds.min.x, Is.LessThan(body.bounds.min.x));
                    Assert.That(rim.bounds.max.x, Is.GreaterThan(body.bounds.max.x));
                    Assert.That(rim.bounds.min.z, Is.LessThan(body.bounds.min.z));
                    Assert.That(rim.bounds.max.z, Is.GreaterThan(body.bounds.max.z));
                    Assert.That(body.bounds.size.y, Is.EqualTo(1.7f).Within(.001f));
                    Assert.That(body.bounds.min.y, Is.EqualTo(floor == lower ? 0f : 5f).Within(.001f));
                    var blocker = body.GetComponent<BoxCollider>();
                    Assert.That(blocker, Is.Not.Null);
                    Assert.That(blocker.enabled && !blocker.isTrigger, Is.True);
                    Assert.That(Vector3.Distance(blocker.bounds.center, body.bounds.center), Is.LessThan(.001f));
                    Assert.That(Vector3.Distance(blocker.bounds.size, body.bounds.size), Is.LessThan(.001f));
                    Assert.That(rim.GetComponent<Collider>(), Is.Null,
                        "The small visual cap offset must not add a new movement obstacle.");
                    AssertOpaque(body);
                    AssertOpaque(rim);
                }
            }
        }

        [UnityTest]
        public IEnumerator CoverPairsStayStableWhenTheUpperFloorCutawayChanges()
        {
            var visibility = All<Level4FloorVisibility>().Single();
            var covers = new[] { lower, upper }.SelectMany(f => f.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.name.StartsWith("Cover_", StringComparison.Ordinal)).ToArray();
            Assert.That(covers.Length, Is.EqualTo(28));
            var poses = covers.Select(r => r.transform.localToWorldMatrix).ToArray();
            var materials = covers.Select(r => r.sharedMaterial).ToArray();
            foreach (bool upstairs in new[] { false, true, false })
            {
                spirit.RestoreAt(Level4Layout.Map(upstairs
                    ? new Vector3(74f, 6.4f, 25f) : new Vector3(9f, 1.4f, 7f)));
                yield return null;
                yield return null;
                visibility.RefreshVisibility();
                Assert.That(visibility.UpperVisible, Is.EqualTo(upstairs));
                for (int i = 0; i < covers.Length; i++)
                {
                    var cover = covers[i];
                    Assert.That(cover.enabled, Is.True, cover.name);
                    Assert.That(cover.forceRenderingOff, Is.EqualTo(cover.transform.IsChildOf(upper) && !upstairs),
                        "Both the body and cap must follow the same floor cutaway state.");
                    Assert.That(cover.transform.localToWorldMatrix, Is.EqualTo(poses[i]));
                    Assert.That(cover.sharedMaterial, Is.SameAs(materials[i]));
                    AssertOpaque(cover);
                }
            }
        }

        private static MeshRenderer[] Covers(Transform floor) => floor.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.name.StartsWith("Cover_", StringComparison.Ordinal)
                && !r.name.StartsWith("Cover_Rim_", StringComparison.Ordinal)).ToArray();

        private static void AssertOpaque(MeshRenderer renderer)
        {
            var material = renderer.sharedMaterial;
            Assert.That(material, Is.Not.Null, renderer.name);
            Assert.That(material.color.a, Is.EqualTo(1f));
            Assert.That(material.renderQueue, Is.LessThanOrEqualTo(2500));
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1f));
        }

        private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
