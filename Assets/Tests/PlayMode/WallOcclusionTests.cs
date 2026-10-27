using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class WallOcclusionTests
    {
        private readonly List<GameObject> objects = new();
        private readonly List<Object> assets = new();
        private PossessionController possession;
        private Camera camera;
        private WallOcclusionFader fader;
        private Material material;
        private Scene level;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.4f, 0.6f, 0.8f);
            assets.Add(material);
            var player = Create("Spirit", new Vector3(0f, 1f, 0f));
            player.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            var collider = player.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.4f;
            player.AddComponent<SpiritController>();
            possession = player.AddComponent<PossessionController>();
            possession.enabled = false;
            var view = Create("Visibility test camera", new Vector3(0f, 4f, -8f));
            camera = view.AddComponent<Camera>();
            camera.fieldOfView = 90f;
            camera.transform.LookAt(player.transform.position);
            camera.tag = "MainCamera";
            fader = view.AddComponent<WallOcclusionFader>();
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (fader != null) fader.RestoreAll();
            foreach (GameObject item in objects) if (item != null) Object.Destroy(item);
            objects.Clear();
            if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
            foreach (Object asset in assets) if (asset != null) Object.Destroy(asset);
            assets.Clear();
            yield return null;
            EventBus.Clear();
        }

        [Test]
        public void BlockingWallFadesWithoutChangingPhysicsOrSharedMaterial()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Vector3 position = wall.transform.position;
            Vector3 scale = wall.transform.localScale;
            int layer = wall.layer;
            Refresh();
            Material faded = wall.GetComponent<Renderer>().sharedMaterial;
            Assert.That(faded, Is.Not.SameAs(material));
            Assert.That(faded.shader.name, Is.EqualTo("Possession/Visibility/P5WallFade"));
            Assert.That(faded.GetFloat("_P5OcclusionAlpha"), Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(material.shader.name, Is.EqualTo("Standard"));
            Assert.That(material.color.a, Is.EqualTo(1f));
            Assert.That(wall.GetComponent<Collider>().enabled, Is.True);
            Assert.That(wall.GetComponent<Collider>().isTrigger, Is.False);
            Assert.That(wall.transform.position, Is.EqualTo(position));
            Assert.That(wall.transform.localScale, Is.EqualTo(scale));
            Assert.That(wall.layer, Is.EqualTo(layer));
        }

        [Test]
        public void MovingAwayRestoresExactMaterialBinding()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Refresh();
            wall.transform.position = new Vector3(20f, 2f, -4f);
            Refresh();
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(fader.FadedRendererCount, Is.Zero);
        }

        [Test]
        public void AllBlockingWallsFadeWhileUnrelatedWallStaysOpaque()
        {
            var first = Wall("Wall_First", new Vector3(0f, 2f, -3f));
            var second = Wall("Wall_Second", new Vector3(0f, 2f, -5f));
            var unrelated = Wall("Wall_Offscreen", new Vector3(20f, 2f, -4f));
            Refresh();
            Assert.That(fader.FadedRendererCount, Is.EqualTo(2));
            Assert.That(first.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
            Assert.That(second.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
            Assert.That(unrelated.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void WallsBehindThePlayerAndTriggerVolumesRemainOpaque()
        {
            var behind = Wall("Wall_Behind", new Vector3(0f, 2f, 3f));
            var trigger = Wall("Wall_Trigger", new Vector3(0f, 2f, -4f));
            trigger.GetComponent<Collider>().isTrigger = true;
            Refresh();
            Assert.That(behind.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(trigger.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void GeometryUnderWallsGroupIsRecognizedWithoutRenamingIt()
        {
            var group = Create("Walls", Vector3.zero);
            var wall = Wall("Entry_South", new Vector3(0f, 2f, -4f));
            wall.transform.SetParent(group.transform, true);
            Refresh();
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
        }

        [Test]
        public void MarkerSupportsCustomGeometryAndCanExcludeWalls()
        {
            var custom = Wall("Architecture_001", new Vector3(0f, 2f, -4f));
            Refresh();
            Assert.That(custom.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            var marker = custom.AddComponent<VisibilityOccluder>();
            Refresh();
            Assert.That(custom.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
            Set(marker, "fadeWhenBlocking", false);
            Refresh();
            Assert.That(custom.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void DoorsTargetsAndBreakableWallsRetainPuzzleFeedbackMaterials()
        {
            var door = Wall("Wall_Door", new Vector3(0f, 2f, -3f));
            door.AddComponent<Door>();
            var target = Wall("Wall_Target", new Vector3(0f, 2f, -4f));
            target.AddComponent<Target>();
            var breakable = Wall("BreakableWall", new Vector3(0f, 2f, -5f));
            breakable.AddComponent<BreakableWall>();
            Refresh();
            Assert.That(fader.FadedRendererCount, Is.Zero);
            Assert.That(door.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(target.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(breakable.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
        }

        [Test]
        public void MultiMaterialAndPropertyBlockStateArePreserved()
        {
            var wall = Wall("Wall_Multiple", new Vector3(0f, 2f, -4f));
            var second = new Material(material);
            assets.Add(second);
            Renderer renderer = wall.GetComponent<Renderer>();
            renderer.sharedMaterials = new[] { material, second };
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", Color.yellow);
            renderer.SetPropertyBlock(block);
            Refresh();
            Assert.That(renderer.sharedMaterials, Has.Length.EqualTo(2));
            Assert.That(renderer.sharedMaterials[0].GetColor("_Color"), Is.EqualTo(material.color));
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_Color"), Is.EqualTo(Color.yellow));
            block.SetColor("_Color", Color.green);
            renderer.SetPropertyBlock(block);
            fader.RestoreAll();
            Assert.That(renderer.sharedMaterials[0], Is.SameAs(material));
            Assert.That(renderer.sharedMaterials[1], Is.SameAs(second));
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_Color"), Is.EqualTo(Color.green));
        }

        [Test]
        public void DisablingComponentImmediatelyRestoresWalls()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Refresh();
            fader.enabled = false;
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(fader.FadedRendererCount, Is.Zero);
        }

        [Test]
        public void CameraMotionIsNotControlledByVisibilityComponent()
        {
            Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            camera.transform.SetPositionAndRotation(new Vector3(1f, 5f, -8f), Quaternion.Euler(25f, 5f, 0f));
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Refresh();
            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
        }

        [Test]
        public void CameraInsideWallIsAlsoDetected()
        {
            var wall = Wall("Wall_CameraInside", camera.transform.position);
            wall.transform.localScale = new Vector3(3f, 3f, 3f);
            Refresh();
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
        }

        [Test]
        public void PossessionAndEjectionSwitchTheTrackedBody()
        {
            var wall = Wall("Wall_Spirit", new Vector3(0f, 2f, -4f));
            Refresh();
            Assert.That(fader.TrackedTarget, Is.EqualTo(possession.transform));
            var host = Create("Guard", new Vector3(8f, 1f, 0f)).AddComponent<GuardEnemy>();
            host.enabled = false;
            Invoke(possession, "Possess", host);
            Refresh();
            Assert.That(fader.TrackedTarget, Is.EqualTo(host.transform));
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
            // Clear only the possession reference to isolate rendering from grounding logic.
            Set(possession, "possessedEnemy", null);
            Refresh();
            Assert.That(fader.TrackedTarget, Is.EqualTo(possession.transform));
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.Not.SameAs(material));
        }

        [Test]
        public void ExternalMaterialReplacementSurvivesRestoration()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Refresh();
            var replacement = new Material(material) { color = Color.green };
            assets.Add(replacement);
            wall.GetComponent<Renderer>().sharedMaterial = replacement;
            fader.RestoreAll();
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.SameAs(replacement));
        }

        [Test]
        public void DestroyingAnOccluderAndLosingThePlayerAreSafe()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Refresh();
            Object.DestroyImmediate(wall);
            Assert.DoesNotThrow(Refresh);
            Object.DestroyImmediate(possession.gameObject);
            Assert.DoesNotThrow(Refresh);
            Assert.That(fader.FadedRendererCount, Is.Zero);
        }

        [Test]
        public void InstallationDoesNotDuplicateTheComponent()
        {
            Assert.That(WallOcclusionInstaller.Install(), Is.SameAs(fader));
            Assert.That(WallOcclusionInstaller.Install(), Is.SameAs(fader));
            Assert.That(camera.GetComponents<WallOcclusionFader>(), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FadingAndRestorationAreGradualAndWorkWhilePaused()
        {
            var wall = Wall("Wall_Blocker", new Vector3(0f, 2f, -4f));
            Physics.SyncTransforms();
            fader.RefreshVisibility(0.01f);
            float partial = wall.GetComponent<Renderer>().sharedMaterial.GetFloat("_P5OcclusionAlpha");
            Assert.That(partial, Is.GreaterThan(0.18f).And.LessThan(1f));
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial.GetFloat("_P5OcclusionAlpha"),
                Is.EqualTo(0.18f).Within(0.01f));
            wall.transform.position = new Vector3(20f, 2f, -4f);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(wall.GetComponent<Renderer>().sharedMaterial, Is.SameAs(material));
        }

        [UnityTest]
        public IEnumerator InstallerWorksAcrossAllThreeSavedLevels()
        {
            foreach (GameObject item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
            fader = null;
            foreach (string name in new[] { "Level_01", "Level_02", "Level_03" })
            {
                yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                level = SceneManager.GetSceneByName(name);
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                var visibility = camera.GetComponent<WallOcclusionFader>();
                Assert.That(visibility, Is.Not.Null, name + " did not receive runtime wall visibility.");
                yield return null;
                Assert.That(visibility.TrackedTarget, Is.Not.Null);
                Assert.That(camera.GetComponents<WallOcclusionFader>(), Has.Length.EqualTo(1));
                yield return SceneManager.UnloadSceneAsync(level);
                EventBus.Clear();
                level = default;
            }
        }

        private GameObject Create(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        private GameObject Wall(string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = new Vector3(6f, 4f, 0.5f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            objects.Add(go);
            return go;
        }

        private void Refresh()
        {
            Physics.SyncTransforms();
            fader.RefreshVisibility(1f);
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Invoke(object target, string method, params object[] args) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
