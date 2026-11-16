using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class GoldPart1Tests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("GoldPart1Cleanup"));
            yield return SceneManager.UnloadSceneAsync(old); EventBus.Clear();
        }

        private static IEnumerator Load(string name)
        {
            Time.timeScale = 1f; EventBus.Clear(); yield return SceneManager.LoadSceneAsync(name);
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.enabled = false;
            foreach (var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) attack.enabled = false;
            yield return null; yield return null;
        }

        [UnityTest] public IEnumerator FloorCoverageIsCompleteWithoutCoplanarRenderOverlapInEveryCampaignScene()
        {
            foreach (string name in CampaignLevels.SceneNames)
            {
                yield return Load(name);
                var surface = Object.FindFirstObjectByType<GoldFloorSurface>();
                Assert.That(surface, Is.Not.Null);
                var patches = surface.VisiblePatches;
                Assert.That(patches.Count, Is.GreaterThan(0));
                for (int i = 0; i < patches.Count; i++) for (int j = i + 1; j < patches.Count; j++)
                {
                    float width = Mathf.Min(patches[i].xMax, patches[j].xMax) - Mathf.Max(patches[i].xMin, patches[j].xMin);
                    float depth = Mathf.Min(patches[i].yMax, patches[j].yMax) - Mathf.Max(patches[i].yMin, patches[j].yMin);
                    Assert.That(width <= .0001f || depth <= .0001f, Is.True, name + " renders the same floor area twice.");
                }
                var originals = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(GoldRoomPresentation.IsFloor).ToArray();
                foreach (var floor in originals)
                {
                    Assert.That(floor.enabled, Is.False, "Original coplanar rendering must not compete with the partitioned surface.");
                    Assert.That(floor.GetComponent<Collider>().enabled, Is.True);
                    var bounds = floor.bounds;
                    foreach (float x in new[] { .1f, .5f, .9f }) foreach (float z in new[] { .1f, .5f, .9f })
                    {
                        var sample = new Vector2(Mathf.Lerp(bounds.min.x, bounds.max.x, x), Mathf.Lerp(bounds.min.z, bounds.max.z, z));
                        Assert.That(patches.Any(p => p.Contains(sample)), Is.True, name + " has a visible floor hole.");
                    }
                }
                Assert.That(surface.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
        }

        [UnityTest] public IEnumerator CollarFollowsRealPossessionAndEjectionWithoutAnOverheadSymbol()
        {
            yield return Load(CampaignLevels.FirstScene);
            var pc = Object.FindFirstObjectByType<PossessionController>();
            var spirit = pc.GetComponent<SpiritController>();
            var identity = pc.GetComponent<PlayerIdentityView>();
            var guard = Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();
            var original = guard.GetComponent<Renderer>().sharedMaterial;
            bool possessed = false;
            foreach (var offset in new[] { -guard.transform.forward, Vector3.left, Vector3.back, Vector3.right })
            {
                spirit.RestoreAt(guard.transform.position + offset * 2.5f); Physics.SyncTransforms();
                if (pc.TryPossess(guard)) { possessed = true; break; }
            }
            Assert.That(possessed, Is.True); yield return null;
            Assert.That(identity.TrackedBody, Is.EqualTo(guard.transform)); Assert.That(identity.IsShowing, Is.True);
            var visuals = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == "Gold_PlayerIdentity");
            Assert.That(visuals.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(1), "Only the collar remains; no health-like overhead symbol.");
            Assert.That(pc.TryEject(false), Is.True); yield return null;
            Assert.That(identity.TrackedBody, Is.EqualTo(spirit.transform)); Assert.That(identity.IsShowing, Is.True);
            Assert.That(guard.GetComponent<Renderer>().sharedMaterial, Is.EqualTo(original));
        }

        [UnityTest] public IEnumerator DoorPanelsRemainSolidUntilTheActualTargetsUnlockAndClearTheOpening()
        {
            foreach (string name in CampaignLevels.SceneNames)
            {
                yield return Load(name);
                var skins = Object.FindObjectsByType<GoldDoorPresentation>(FindObjectsSortMode.None);
                Assert.That(skins.Length, Is.GreaterThan(0));
                foreach (var skin in skins)
                {
                    var blocker = skin.GetComponent<Collider>(); var before = blocker.bounds;
                    var view = skin.GetComponent<SlidingDoorView>();
                    Assert.That(blocker.enabled && !blocker.isTrigger, Is.True);
                    Assert.That(skin.Panels.All(p => p.enabled && p.GetComponent<Collider>() == null && p.sharedMaterial != null), Is.True);
                    Assert.That(skin.Panels.Count(p => p.name == "Door handle"), Is.EqualTo(4), "Each leaf has a handle on both sides.");
                    var details = view.LeftLeaf.Find("Security leaf details");
                    if (details != null) Assert.That(details.GetComponentsInChildren<Renderer>().All(r => !r.enabled), Is.True);
                    foreach (var target in view.Targets) target.OnHit();
                    yield return new WaitForSeconds(.5f);
                    Assert.That(skin.GetComponent<Door>().IsOpen && blocker.isTrigger, Is.True);
                    Assert.That(blocker.bounds, Is.EqualTo(before));
                    Assert.That(skin.Panels.All(p => !p.enabled), Is.True, "No panel/handle can hang in the open route.");
                }
            }
        }

        [UnityTest] public IEnumerator InstallingAgainAddsNoCollidersAndDoesNotDuplicateVisuals()
        {
            yield return Load(CampaignLevels.ThirdScene);
            var before = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => c.GetInstanceID()).ToArray();
            GoldPresentationInstaller.Install(SceneManager.GetActiveScene());
            GoldPresentationInstaller.Install(SceneManager.GetActiveScene());
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => c.GetInstanceID()), Is.EquivalentTo(before));
            Assert.That(Object.FindObjectsByType<GoldFloorSurface>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GoldDoorPresentation>(FindObjectsSortMode.None), Has.Length.EqualTo(4));
        }
    }
}
