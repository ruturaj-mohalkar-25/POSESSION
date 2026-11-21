using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class UscRoomStyleTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("UscDecorCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator CampaignArtHasReadableOriginalLetteringAndArtificialLeavesWithoutNewColliders()
        {
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var art = Object.FindObjectsByType<UscWallArt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                Assert.That(art.Length,Is.GreaterThan(1),scene);
                foreach (var poster in art)
                {
                    Assert.That(poster.GetComponentsInChildren<Collider>(true),Is.Empty);
                    var labels = poster.GetComponentsInChildren<TextMesh>(true);
                    Assert.That(labels.Length,Is.GreaterThanOrEqualTo(2));
                    Assert.That(labels.All(t => t.GetComponent<MeshRenderer>().sharedMaterial != null),Is.True);
                    var headline = labels.OrderByDescending(t => t.GetComponent<Renderer>().bounds.size.y).First();
                    Bounds text = headline.GetComponent<Renderer>().bounds;
                    Assert.That(text.size.x,Is.LessThanOrEqualTo(3.25f));
                    Assert.That(text.size.y,Is.InRange(.65f,1.4f),scene+" headline needs a visible world-space size");
                    Assert.That(poster.transform.parent.GetComponent<Collider>(),Is.Not.Null,"Art mounts on solid walls.");
                }
                var vases = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Decor_CornerVase").ToArray();
                Assert.That(vases.Length,Is.GreaterThan(0),scene);
                Assert.That(vases.All(v=>v.GetComponentsInChildren<Transform>().Any(t=>t.name=="Artificial leaf")),Is.True);
                Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Any(t=>t.name.ToLowerInvariant().Contains("sofa")),Is.False);
            }
        }

        [UnityTest]
        public IEnumerator CampusArtHidesAndRestoresWithItsWallWithoutLosingLettering()
        {
            yield return Load(CampaignLevels.FirstScene);
            var art = Object.FindFirstObjectByType<UscWallArt>();
            var wall = art.transform.parent.GetComponent<Renderer>();
            Material original = wall.sharedMaterial;
            var text = art.GetComponentsInChildren<TextMesh>();
            var textMaterials = text.Select(t=>t.GetComponent<Renderer>().sharedMaterial).ToArray();
            var fading = new Material(Resources.Load<Shader>("Visibility/P5WallFade"));
            fading.SetFloat("_P5OcclusionAlpha",.18f);
            wall.sharedMaterial = fading;
            yield return null; yield return null;
            Assert.That(art.gameObject.activeSelf,Is.False);
            wall.sharedMaterial = original;
            yield return null; yield return null;
            Assert.That(art.gameObject.activeSelf,Is.True);
            Assert.That(text.Select(t=>t.GetComponent<Renderer>().sharedMaterial),Is.EqualTo(textMaterials));
            Object.Destroy(fading);
        }

        [UnityTest]
        public IEnumerator AllFourSafeCornersAreDressedEvenBeyondTheSeventhRoom()
        {
            yield return SyntheticScene();
            for (int i=0;i<9;i++) Room(new Vector3(i*22f,0,0));
            Physics.SyncTransforms();
            var before = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Select(c=>c.GetInstanceID()).ToArray();
            GoldRoomDecor.Install(SceneManager.GetActiveScene());
            yield return null; yield return null;
            Assert.That(Corners().Length,Is.EqualTo(36),"Every clear corner in all nine rooms receives a plant or vase.");
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Select(c=>c.GetInstanceID()),Is.EquivalentTo(before));
            GoldRoomDecor.Install(SceneManager.GetActiveScene());
            Assert.That(Corners().Length,Is.EqualTo(36),"Reinstalling must not add duplicate props.");
        }

        [UnityTest]
        public IEnumerator DoorAndWholePatrolSegmentClearanceKeepCornerDecorOutOfTheRoute()
        {
            yield return SyntheticScene();
            Room(Vector3.zero);
            var door = new GameObject("Protected door").AddComponent<Door>();
            door.transform.position = new Vector3(6.75f,1f,6.75f);
            var from = new GameObject("Guard_WP0").transform;
            var to = new GameObject("Guard_WP1").transform;
            from.position = new Vector3(-10f,1f,5f); to.position = new Vector3(-4f,1f,9f);
            Physics.SyncTransforms();
            GoldRoomDecor.Install(SceneManager.GetActiveScene());
            yield return null; yield return null;
            var corners = Corners();
            Assert.That(corners.Length,Is.EqualTo(2),"Skip the door corner and the corner crossed by the patrol segment.");
            Assert.That(corners.All(t=>t.position.z<0f),Is.True);
            Assert.That(corners.All(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(6.75f,6.75f))>=2.5f),Is.True);
        }

        private static Transform[] Corners() => Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t=>t.name=="Decor_CornerPlant" || t.name=="Decor_CornerVase").ToArray();

        private static IEnumerator Load(string name)
        {
            Time.timeScale=1f; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(name);
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))enemy.enabled=false;
            foreach(var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))attack.enabled=false;
            yield return null; yield return null;
        }

        private static IEnumerator SyntheticScene()
        {
            Time.timeScale=1f; EventBus.Clear();
            var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("UscCornerFixture"));
            yield return SceneManager.UnloadSceneAsync(old);
            var spirit=new GameObject("Fixture Spirit").AddComponent<SpiritController>();
            spirit.transform.position=Vector3.up;
            spirit.enabled=false;
            spirit.GetComponent<Rigidbody>().isKinematic=true;
        }

        private static void Room(Vector3 at)
        {
            Cube("Fixture_Floor",at-Vector3.up*.1f,new Vector3(16f,.2f,16f));
            Cube("North wall",at+new Vector3(0,2,8),new Vector3(16f,4f,.2f));
            Cube("South wall",at+new Vector3(0,2,-8),new Vector3(16f,4f,.2f));
            Cube("West wall",at+new Vector3(-8,2,0),new Vector3(.2f,4f,16f));
            Cube("East wall",at+new Vector3(8,2,0),new Vector3(.2f,4f,16f));
        }

        private static void Cube(string name,Vector3 at,Vector3 scale)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name=name; cube.transform.position=at; cube.transform.localScale=scale;
        }
    }
}
