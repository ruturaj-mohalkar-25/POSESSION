using System.Collections;
using System.Collections.Generic;
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
                    Vector3 text = Vector3.Scale(headline.GetComponent<Renderer>().localBounds.size,headline.transform.localScale);
                    Assert.That(text.x,Is.LessThanOrEqualTo(3.25f));
                    Assert.That(text.y,Is.InRange(.65f,1.4f),scene+" headline needs a visible world-space size");
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
        public IEnumerator CampaignContainsEveryRequestedPosterHeadlineAcrossActualRoomPlacements()
        {
            var headlines = new HashSet<string>();
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var art = Object.FindObjectsByType<UscWallArt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                foreach (var poster in art.Where(p=>p.name=="Decor_Painting"))
                    foreach (var text in poster.GetComponentsInChildren<TextMesh>(true).Where(t=>t.name=="Campus headline"))
                        headlines.Add(text.text.Replace('\n',' ').Trim());
                // A coverage win must not silently replace the existing TV, plants, vases or mat.
                var props = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                foreach (string kind in new[]{"Decor_Painting","Decor_WallTV","Decor_CornerPlant","Decor_CornerVase","Decor_HidingCrate"})
                    Assert.That(props.Any(t=>t.name==kind),Is.True,scene+" missing "+kind);
            }
            foreach (string expected in new[]{"USC","FIGHT ON","TROJANS","SC","LA","BEAT UCLA"})
                Assert.That(headlines.Contains(expected),Is.True,"Missing primary poster headline: "+expected
                    +"; actual: "+string.Join(", ",headlines));
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
                .Where(c=>!c.GetComponentsInParent<Transform>().Any(t=>t.name=="Decor_HidingCrate"))
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

        [UnityTest]
        public IEnumerator EveryInteriorWallFaceGetsCorrectlyOrientedArtWithReadableLocalLettering()
        {
            yield return SyntheticScene();
            Room(Vector3.zero);
            Physics.SyncTransforms();
            GoldRoomDecor.Install(SceneManager.GetActiveScene());
            yield return null; yield return null;
            var art=Object.FindObjectsByType<UscWallArt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var outward in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                var face=art.Where(p=>Vector3.Dot(p.transform.forward,outward)>.99f).ToArray();
                Assert.That(face.Length,Is.GreaterThan(0),"No wall art facing "+-outward);
                foreach(var poster in face)
                {
                    var wall=poster.transform.parent.GetComponent<Renderer>();
                    Assert.That(wall,Is.Not.Null);
                    Assert.That(Vector3.Dot(poster.transform.position-wall.bounds.center,outward),Is.LessThan(0f),"Art must face into room.");
                    foreach(var piece in poster.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Assert.That(Quaternion.Angle(piece.transform.rotation,poster.transform.rotation),Is.LessThan(.1f),piece.name);
                        if(piece.name!="Campus headline")continue;
                        var size=Vector3.Scale(piece.localBounds.size,piece.transform.localScale);
                        Assert.That(size.x,Is.LessThanOrEqualTo(3.25f));
                        Assert.That(size.y,Is.InRange(.65f,1.4f));
                    }
                }
                for(int i=0;i<face.Length;i++)for(int j=i+1;j<face.Length;j++)
                    Assert.That(Vector3.Distance(face[i].transform.position,face[j].transform.position),Is.GreaterThanOrEqualTo(4.4f));
            }
            Assert.That(art.Any(p=>p.name=="Decor_WallTV"),Is.True);
            Assert.That(art.Any(p=>p.name=="Decor_Painting"),Is.True);
        }

        [UnityTest]
        public IEnumerator WallArtRespectsDoorAndExistingNameplateClearance()
        {
            yield return SyntheticScene();
            Room(Vector3.zero);
            var door=new GameObject("North door").AddComponent<Door>();
            door.transform.position=new Vector3(0,1,8);
            var wall=GameObject.Find("West wall").transform;
            var sign=new GameObject("Gold_RoomSign").transform;
            sign.SetParent(wall,true);sign.position=new Vector3(-7.8f,3.2f,0);
            Physics.SyncTransforms();
            GoldRoomDecor.Install(SceneManager.GetActiveScene());
            yield return null;yield return null;
            foreach(var poster in Object.FindObjectsByType<UscWallArt>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var p=poster.transform.position;
                Assert.That(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(0,8)),Is.GreaterThanOrEqualTo(3f));
                if(poster.transform.parent==wall)
                    Assert.That(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(-7.8f,0)),Is.GreaterThanOrEqualTo(4.1f));
            }
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
