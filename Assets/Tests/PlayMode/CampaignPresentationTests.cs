using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Possession.Tests
{
    public sealed class CampaignPresentationTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PresentationCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest] public IEnumerator EveryCampaignObjectiveIsDiscoverableImmediatelyWithoutChangingItsCollider()
        {
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                foreach (var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
                {
                    var visibility = target.GetComponent<ObjectiveVisibility>();
                    Assert.That(visibility, Is.Not.Null, target.name);
                    Assert.That(visibility.Silhouette.enabled, Is.True, target.name);
                    Assert.That(visibility.Source, Is.SameAs(target.GetComponent<Renderer>()));
                    Assert.That(visibility.Silhouette.GetComponentsInChildren<Collider>(), Is.Empty);
                    target.OnHit(); visibility.RefreshVisibility();
                    Assert.That(visibility.Silhouette.enabled, Is.False);
                }
                foreach (var pickup in Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                {
                    var visibility = pickup.GetComponent<ObjectiveVisibility>();
                    Assert.That(visibility, Is.Not.Null, pickup.name);
                    Assert.That(visibility.Source, Is.SameAs(pickup.Visual));
                    Assert.That(visibility.Silhouette.enabled, Is.EqualTo(pickup.IsReady && pickup.Visual.enabled));
                    Assert.That(visibility.Silhouette.GetComponentsInChildren<Collider>(), Is.Empty);
                }
            }
        }

        [UnityTest] public IEnumerator CollectedShardLosesItsSilhouetteUntilItReforms()
        {
            yield return Load(CampaignLevels.ThirdScene);
            var pickup = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).First();
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            spirit.RestoreAt(pickup.transform.position + Vector3.up * 1.4f);
            Assert.That(pickup.TryCollect(spirit.GetComponent<PossessionCharge>()), Is.True);
            var visibility = pickup.GetComponent<ObjectiveVisibility>(); visibility.RefreshVisibility();
            Assert.That(visibility.Silhouette.enabled, Is.False);
            // Move away before reforming; staying inside the trigger would collect it again.
            spirit.RestoreAt(pickup.transform.position + new Vector3(4f, 1.4f, 0f));
            Physics.SyncTransforms();
            Set(pickup, "readyAt", Time.time - 1f);
            yield return null;
            Assert.That(pickup.IsReady, Is.True);
            Assert.That(visibility.Silhouette.enabled, Is.True);
            pickup.gameObject.SetActive(false);
            Assert.That(visibility.Silhouette.enabled, Is.False);
        }

        [UnityTest] public IEnumerator HighlightDoesNotAllowShootingThroughAWall()
        {
            yield return Load(CampaignLevels.FirstScene);
            var at = new Vector3(1000f, 2f, 1000f);
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = at + Vector3.forward * 4f;
            var target = targetObject.AddComponent<Target>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Visibility test solid wall"; wall.transform.position = at + Vector3.forward * 2f;
            wall.transform.localScale = new Vector3(5, 5, .5f);
            var shoot = Object.FindFirstObjectByType<GuardEnemy>().GetComponent<ShootAbility>();
            var projectile = Object.Instantiate(Read<GameObject>(shoot, "projectilePrefab"), at, Quaternion.identity);
            Physics.SyncTransforms();
            Assert.That(target.GetComponent<ObjectiveVisibility>().Silhouette.enabled, Is.True);
            yield return new WaitForSeconds(.4f);
            Assert.That(projectile == null, Is.True, "The solid wall must consume the shot.");
            Assert.That(target.IsHit, Is.False, "Seeing a silhouette must not solve a target through a wall.");
            Object.Destroy(wall); Physics.SyncTransforms(); yield return null;
            Object.Instantiate(Read<GameObject>(shoot, "projectilePrefab"), at, Quaternion.identity);
            yield return new WaitForSeconds(.4f);
            Assert.That(target.IsHit, Is.True, "The same shot must work once the obstruction is removed.");
        }

        [UnityTest] public IEnumerator AllStagesShareOneHUDAndChargePanelAcrossSceneTransitions()
        {
            string signature = null;
            foreach (string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var hud = Object.FindFirstObjectByType<HUDManager>();
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                Assert.That(canvases.Length, Is.EqualTo(1), "No separate Hunted or charge canvas.");
                var charge = Object.FindFirstObjectByType<ChargeHUD>();
                Assert.That(charge, Is.Not.Null); Assert.That(charge.Label, Is.Not.Null);
                Assert.That(charge.Label.GetComponentInParent<HUDManager>(), Is.SameAs(hud));
                ChargeHUDInstaller.Install(SceneManager.GetActiveScene());
                Assert.That(Object.FindObjectsByType<ChargeHUD>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(hud.transform.GetComponentsInChildren<RectTransform>(true).Count(t => t.name == "ChargePanel"), Is.EqualTo(1));
                Assert.That(charge.Label.text, Does.StartWith("CHARGES 1/3"));
                Assert.That(hud.transform.Find("ObjectiveBar").gameObject.activeInHierarchy, Is.True);
                var current = Signature(hud);
                if (signature == null) signature = current;
                else Assert.That(current, Is.EqualTo(signature), scene + " HUD differs from displayed Level 1.");
                EventBus.LevelCompleted?.Invoke(); yield return null;
                Assert.That(charge.Label.transform.parent.gameObject.activeSelf, Is.False, "Charge strip must not cover results.");
            }
        }

        [UnityTest] public IEnumerator HuntedUsesTheSharedObjectiveBarAndKeepsCheckpointRetry()
        {
            yield return Load(CampaignLevels.SecondScene);
            var director = Object.FindFirstObjectByType<Level02Director>();
            var hud = Object.FindFirstObjectByType<HUDManager>();
            var objective = Read<Text>(hud, "objectiveText");
            Assert.That(objective.text, Does.Contain("CHOOSE A ROUTE"));
            var door = Read<Door>(director, "northDoor");
            foreach (var t in Read<Target[]>(door, "requiredTargets")) t.OnHit();
            yield return null;
            Assert.That(objective.text, Does.Contain("BREAK A ROUTE WALL"));
            director.MarkCheckpoint(); yield return null;
            Assert.That(objective.text, Does.Contain("C: RETRY"));
            var wall = Read<BreakableWall>(director, "northWall");
            wall.TakeDamage(100); yield return null;
            Assert.That(objective.text, Does.Contain("ROUTE CLEAR"));
            director.RetryCourtyard(); yield return null; yield return null;
            director = Object.FindFirstObjectByType<Level02Director>();
            Assert.That(director.CheckpointReached, Is.True);
            Assert.That(Object.FindObjectsByType<ChargeHUD>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        private static string Signature(HUDManager hud)
        {
            var textFields = new[] { "healthText", "levelTimerText", "objectiveText", "messageText" };
            var signature = "";
            foreach (string field in textFields)
            {
                var t = Read<Text>(hud, field); var r = t.rectTransform;
                signature += field + r.anchorMin + r.anchorMax + r.pivot + r.anchoredPosition + r.sizeDelta + t.fontSize + t.font.name + t.color;
            }
            return signature;
        }
        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f; EventBus.Clear(); yield return SceneManager.LoadSceneAsync(scene);
            foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) e.enabled = false;
            foreach (var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) e.enabled = false;
            yield return null; yield return null;
        }
        private static T Read<T>(object obj, string field) => (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    }
}
