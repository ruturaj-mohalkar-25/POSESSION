using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class CampaignSlidingDoorTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("SlidingDoorCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.enabled = false;
            foreach (var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) attack.enabled = false;
            Object.FindFirstObjectByType<SpiritController>().SetVisible(false);
            Physics.SyncTransforms();
        }

        [UnityTest] public IEnumerator EveryCampaignDoorKeepsItsTargetsAndClosedPhysicalBlocker()
        {
            int[] counts = { 1, 2, 4 };
            for (int scene = 0; scene < CampaignLevels.SceneNames.Length; scene++)
            {
                yield return Load(CampaignLevels.SceneNames[scene]);
                var doors = Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
                Assert.That(doors.Length, Is.EqualTo(counts[scene]));
                foreach (var door in doors)
                {
                    var view = door.GetComponent<SlidingDoorView>(); Assert.That(view, Is.Not.Null, door.name);
                    Assert.That(door.GetComponents<SlidingDoorView>().Length, Is.EqualTo(1));
                    var required = (Target[])typeof(Door).GetField("requiredTargets", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(door);
                    Assert.That(view.Targets, Is.EqualTo(required), "Animation lamps must refer to this door's actual targets.");
                    Assert.That(door.IsOpen, Is.False); Assert.That(view.Opening, Is.Zero);
                    var box = door.GetComponent<BoxCollider>(); Assert.That(box.enabled && !box.isTrigger, Is.True);
                    Assert.That(box.Raycast(new Ray(box.bounds.center - door.transform.forward * 4f, door.transform.forward), out _, 8f), Is.True);
                    Assert.That(view.LeftLeaf.gameObject.activeInHierarchy && view.RightLeaf.gameObject.activeInHierarchy, Is.True);
                    Assert.That(view.LeftLeaf.GetComponent<Renderer>().enabled && view.RightLeaf.GetComponent<Renderer>().enabled, Is.True);
                    Assert.That(view.LeftLeaf.GetComponent<Collider>(), Is.Null, "Presentation must not add a second blocker.");
                    Assert.That(view.RightLeaf.GetComponent<Collider>(), Is.Null);
                    Assert.That(Vector3.Distance(view.LeftLeaf.position, view.RightLeaf.position),
                        Is.EqualTo(box.size.x * Mathf.Abs(door.transform.lossyScale.x) * .5f).Within(.01f));
                    if (scene < 2)
                    {
                        Assert.That(door.GetComponent<Renderer>().enabled, Is.False, "The old solid visual must not remain in the doorway.");
                        var model = door.transform.Find("P5_Model"); if (model != null) Assert.That(model.gameObject.activeSelf, Is.False);
                    }
                }
            }
        }

        [UnityTest] public IEnumerator OnlyOwnTargetsUnlockThenBothPanelsSlideFullyClearAndLampsTurnGreen()
        {
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var views = Object.FindObjectsByType<SlidingDoorView>(FindObjectsSortMode.None);
                foreach (var view in views)
                {
                    var door = view.GetComponent<Door>();
                    var foreign = new GameObject("Unrelated target").AddComponent<Target>(); foreign.OnHit();
                    Assert.That(door.IsOpen, Is.False); Object.Destroy(foreign.gameObject);
                    int opened = 0; System.Action<Door> onOpen = d => { if (d == door) opened++; };
                    EventBus.DoorOpened += onOpen;
                    for (int i = 0; i < view.Targets.Length; i++)
                    {
                        view.Targets[i].OnHit(); view.Targets[i].OnHit();
                        Assert.That(door.IsOpen, Is.EqualTo(i == view.Targets.Length - 1));
                    }
                    Assert.That(opened, Is.EqualTo(1)); EventBus.DoorOpened -= onOpen;
                    yield return new WaitForSeconds(.12f);
                    Assert.That(view.Opening, Is.GreaterThan(0f).And.LessThan(1f), "Both panels should visibly animate, not disappear.");
                    if (scene != CampaignLevels.ThirdScene)
                    {
                        var panel = view.LeftLeaf;
                        Assert.That(Mathf.Abs(panel.localPosition.x) + panel.localScale.x * .5f, Is.EqualTo(.5f).Within(.001f),
                            "Thin-wall panels must stay inside their frame while retracting, not extend into void or adjacent rooms.");
                    }
                    yield return new WaitForSeconds(.4f);
                    Assert.That(view.Opening, Is.EqualTo(1f));
                    var box = door.GetComponent<BoxCollider>(); Assert.That(box.isTrigger, Is.True);
                    float gap = Vector3.Distance(view.LeftLeaf.position, view.RightLeaf.position)
                        - (view.LeftLeaf.lossyScale.x + view.RightLeaf.lossyScale.x) * .5f;
                    Assert.That(gap, Is.EqualTo(box.size.x * Mathf.Abs(door.transform.lossyScale.x)).Within(.02f), "The full original opening must be visually clear.");
                    var lamps = (Renderer[])typeof(SlidingDoorView).GetField("lamps", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                    foreach (var lamp in lamps) Assert.That(lamp.material.color.g, Is.EqualTo(1f).Within(.01f));
                }
            }
        }

        [UnityTest] public IEnumerator SlidingAnimationPausesAndRetryRestoresClosedDoors()
        {
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var view = Object.FindFirstObjectByType<SlidingDoorView>();
                foreach (var target in view.Targets) target.OnHit();
                yield return new WaitForSeconds(.1f);
                Time.timeScale = 0f; float frozen = view.Opening;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(view.Opening, Is.EqualTo(frozen));
                Time.timeScale = 1f;
                yield return new WaitForSeconds(.45f); Assert.That(view.Opening, Is.EqualTo(1f));
                yield return Load(scene);
                foreach (var reset in Object.FindObjectsByType<SlidingDoorView>(FindObjectsSortMode.None))
                {
                    Assert.That(reset.Opening, Is.Zero); Assert.That(reset.GetComponent<Door>().IsOpen, Is.False);
                    Assert.That(reset.GetComponent<Collider>().isTrigger, Is.False);
                    Assert.That(reset.Targets.All(t => !t.IsHit), Is.True);
                }
            }
        }
    }
}
