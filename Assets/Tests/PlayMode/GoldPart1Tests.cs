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
            var shadows = Object.FindFirstObjectByType<GoldContactShadows>();
            AssertGroundedShadow(shadows.PlayerShadow, guard.GetComponent<Renderer>());
            yield return new WaitForSeconds(.15f);
            Assert.That(identity.PulseAmount, Is.GreaterThan(.1f), "Possession should briefly pulse the collar.");
            float pausedPulse = identity.PulseAmount;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(identity.PulseAmount, Is.EqualTo(pausedPulse).Within(.001f), "Pause must freeze the brief effect.");
            Time.timeScale = 1;
            yield return new WaitForSeconds(.65f);
            Assert.That(identity.PulseAmount, Is.Zero, "The collar must settle instead of flashing continuously.");
            var visuals = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == "Gold_PlayerIdentity");
            Assert.That(visuals.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(1), "Only the collar remains; no health-like overhead symbol.");
            Assert.That(pc.TryEject(false), Is.True); yield return null;
            Assert.That(identity.TrackedBody, Is.EqualTo(spirit.transform)); Assert.That(identity.IsShowing, Is.True);
            Assert.That(identity.PulseAmount, Is.GreaterThan(0), "Ejection should pulse the same collar.");
            // Test coroutines resume before LateUpdate; sample the current post-ejection physics pose.
            shadows.Refresh();
            AssertGroundedShadow(shadows.PlayerShadow, spirit.GetComponent<Renderer>());
            Assert.That(guard.GetComponent<Renderer>().sharedMaterial, Is.EqualTo(original));
        }

        [UnityTest] public IEnumerator RoomBoardsMountOnSolidWallsAndContactShadowsRemainDecorativeInEveryScene()
        {
            foreach (string name in CampaignLevels.SceneNames)
            {
                yield return Load(name);
                var boards = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                    .Where(t => t.transform.parent != null && t.transform.parent.name == "Gold_RoomSign").ToArray();
                Assert.That(boards.Length, Is.InRange(1, 8), name + " needs a few readable room boards.");
                foreach (var text in boards)
                {
                    var board = text.transform.parent;
                    Assert.That(board.parent.GetComponent<Collider>(), Is.Not.Null, "Mount on a real solid wall.");
                    Assert.That(board.GetComponentsInChildren<Collider>(true), Is.Empty);
                    Assert.That(text.text, Is.Not.Empty); Assert.That(text.GetComponent<Renderer>().sharedMaterial, Is.Not.Null);
                    Assert.That(text.GetComponent<Renderer>().bounds.size.x, Is.LessThanOrEqualTo(3.9f));
                    var bounds = board.parent.GetComponent<Collider>().bounds;
                    Assert.That(Mathf.Abs(board.position.z - bounds.min.z), Is.LessThan(.2f), "Keep the nameplate attached to the wall face.");
                }
                var mounted = boards[0].transform.parent;
                var wall = mounted.parent.GetComponent<Renderer>();
                var original = wall.sharedMaterial;
                var fading = new Material(Resources.Load<Shader>("Visibility/P5WallFade"));
                fading.SetFloat("_P5OcclusionAlpha", .18f); wall.sharedMaterial = fading;
                yield return null; yield return null;
                Assert.That(mounted.gameObject.activeSelf, Is.False, "Hide the board while its wall fades in front of the player.");
                wall.sharedMaterial = original;
                yield return null; yield return null;
                Assert.That(mounted.gameObject.activeSelf, Is.True, "Restore the board with the solid wall.");
                Object.Destroy(fading);
                var shadows = Object.FindFirstObjectByType<GoldContactShadows>();
                AssertGroundedShadow(shadows.PlayerShadow, Object.FindFirstObjectByType<PlayerIdentityView>().TrackedBody.GetComponent<Renderer>());
                var stamps = shadows.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.Contains("ContactShadow")).ToArray();
                Assert.That(stamps.Length, Is.GreaterThanOrEqualTo(1 + Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length), "Every character needs a ground shadow, including autonomous enemies.");
                Assert.That(stamps.All(r => r.GetComponent<Collider>() == null && r.sharedMaterial.shader.isSupported), Is.True);
            }
        }

        [UnityTest] public IEnumerator RoomDecorRemainsVisualAndInstallsOnlyOnceAcrossCampaign()
        {
            foreach(string name in CampaignLevels.SceneNames)
            {
                yield return Load(name);
                var decor=Object.FindFirstObjectByType<GoldRoomDecor>();
                Assert.That(decor,Is.Not.Null,name);
                var props=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Decor_")).ToArray();
                Assert.That(props.Length,Is.GreaterThan(0),name);
                foreach(string kind in new[]{"Decor_Painting","Decor_WallTV","Decor_CornerPlant","Decor_CornerVase","Decor_FloorMat"})
                    Assert.That(props.Any(t=>t.name==kind),Is.True,name+" missing "+kind);
                Assert.That(props.SelectMany(t=>t.GetComponentsInChildren<Collider>(true)),Is.Empty);
                Assert.That(props.Any(t=>t.name.ToLowerInvariant().Contains("sofa")),Is.False);
                GoldRoomDecor.Install(SceneManager.GetActiveScene());
                Assert.That(Object.FindObjectsByType<GoldRoomDecor>(FindObjectsSortMode.None),Has.Length.EqualTo(1));
                Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name.StartsWith("Decor_")),Is.EqualTo(props.Length));
            }
        }

        private static void AssertGroundedShadow(Renderer shadow, Renderer body)
        {
            Assert.That(shadow, Is.Not.Null); Assert.That(shadow.enabled, Is.True);
            Assert.That(shadow.transform.position.x, Is.EqualTo(body.bounds.center.x).Within(.02f));
            Assert.That(shadow.transform.position.z, Is.EqualTo(body.bounds.center.z).Within(.02f));
            var floors = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(GoldRoomPresentation.IsFloor);
            Assert.That(floors.Any(f => Mathf.Abs(shadow.transform.position.y - f.bounds.max.y - .025f) < .002f), Is.True,
                "The shadow must sit above a real floor rather than follow body height.");
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
