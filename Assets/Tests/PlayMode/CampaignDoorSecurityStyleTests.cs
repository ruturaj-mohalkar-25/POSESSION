using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class CampaignDoorSecurityStyleTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1f;
            var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("SecurityDoorCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }
        private static IEnumerator Load(string scene)
        {
            Time.timeScale=1f;EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(scene);yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))enemy.enabled=false;
            foreach(var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))attack.enabled=false;
            Object.FindFirstObjectByType<SpiritController>().SetVisible(false);
            Physics.SyncTransforms();
        }

        [UnityTest] public IEnumerator EveryDoorHasReadableTwoSidedPanelsWithoutAdditionalBlockers()
        {
            int total=0;
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                foreach(var view in Object.FindObjectsByType<SlidingDoorView>(FindObjectsSortMode.None))
                {
                    total++;
                    foreach(var leaf in new[]{view.LeftLeaf,view.RightLeaf})
                    {
                        var details=leaf.Find("Security leaf details");Assert.That(details,Is.Not.Null,view.name);
                        Assert.That(details.Find("Front"),Is.Not.Null);Assert.That(details.Find("Back"),Is.Not.Null);
                        Assert.That(details.GetComponentsInChildren<Collider>(true),Is.Empty,"Decorative parts must not change passage collision.");
                        var face=details.GetComponentsInChildren<Renderer>().First(r=>r.name=="Raised pale panel").sharedMaterial.color;
                        Assert.That(Mathf.Min(face.r,face.g,face.b),Is.GreaterThan(.7f),"Panels must read as pale metal rather than the old black slab.");
                    }
                    var frame=view.LeftLeaf.parent.Find("Security frame details");Assert.That(frame,Is.Not.Null);
                    Assert.That(frame.GetComponentsInChildren<Collider>(true),Is.Empty);
                    Assert.That(view.GetComponent<Collider>().isTrigger,Is.False,"The existing locked door must still block the route.");
                }
            }
            Assert.That(total,Is.EqualTo(7));
        }

        [UnityTest] public IEnumerator SolvedDoorCarriesEveryDecorativePieceClearOfTheOriginalOpening()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var views=Object.FindObjectsByType<SlidingDoorView>(FindObjectsSortMode.None);
                foreach(var view in views)foreach(var target in view.Targets)target.OnHit();
                yield return new WaitForSeconds(.5f);
                foreach(var view in views)
                {
                    Assert.That(view.Opening,Is.EqualTo(1f));Assert.That(view.GetComponent<Collider>().isTrigger,Is.True);
                    var box=view.GetComponent<BoxCollider>();
                    foreach(var leaf in new[]{view.LeftLeaf,view.RightLeaf})
                    {
                        bool left=leaf==view.LeftLeaf;
                        foreach(var renderer in leaf.Find("Security leaf details").GetComponentsInChildren<Renderer>())
                        {
                            var bounds=renderer.localBounds;
                            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                            {
                                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                                float along=box.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner)).x-box.center.x;
                                if(left)Assert.That(along,Is.LessThanOrEqualTo(-box.size.x*.5f+.002f),view.name+" left detail remains in the open doorway: "+renderer.name);
                                else Assert.That(along,Is.GreaterThanOrEqualTo(box.size.x*.5f-.002f),view.name+" right detail remains in the open doorway: "+renderer.name);
                            }
                        }
                    }
                }
            }
        }
    }
}
