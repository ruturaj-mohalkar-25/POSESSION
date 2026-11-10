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
    public class BetaHudTests
    {
        private Scene level;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator EverySavedLevelUsesTypedAmmoAndTheBetaCamera()
        {
            foreach (string name in CampaignLevels.SceneNames.Concat(new[] { "Level_01", "Tutorial" }))
            {
                if (level.IsValid() && level.isLoaded) yield return SceneManager.UnloadSceneAsync(level);
                yield return Load(name);
                var ammo = Find<AmmoDisplay>();
                Assert.That(Read<Sprite>(ammo, "bulletSprite").name, Is.EqualTo("BulletPip"), name);
                Assert.That(Read<Sprite>(ammo, "hammerSprite").name, Is.EqualTo("HammerPip"), name);
                Assert.That(Read<float>(ammo, "pipSpacing"), Is.EqualTo(2f), name);
                var cameraOffset = CampaignLevels.NumberFor(name) > 0
                    ? new Vector3(0f, 25f, -18f) : new Vector3(0f, 15f, -11f);
                Assert.That(Read<Vector3>(Find<CameraFollow>(), "offset"), Is.EqualTo(cameraOffset), name);
                ammo.UseBulletSprite(); ammo.Show(2, 3);
                AssertPips(ammo, "BulletPip", 48f, 3, 2);
                ammo.UseHammerSprite(); ammo.Show(0, 1);
                AssertPips(ammo, "HammerPip", 60f, 1, 0);
                ammo.Hide();
            }
        }

        [UnityTest]
        public IEnumerator AmmoStaysInsideEveryScreenEdgeInOverlayAndCameraCanvases()
        {
            yield return Load("Level_01");
            var ammo = Find<AmmoDisplay>();
            var canvas = ammo.GetComponentInParent<Canvas>();
            var camera = Find<Camera>();
            foreach (RenderMode mode in new[] { RenderMode.ScreenSpaceOverlay, RenderMode.ScreenSpaceCamera })
            {
                canvas.renderMode = mode;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (bool heavy in new[] { false, true })
                {
                    if (heavy) { ammo.UseHammerSprite(); ammo.Show(1, 1); }
                    else { ammo.UseBulletSprite(); ammo.Show(3, 3); }
                    foreach (Vector2 cursor in new[] { Vector2.zero, new Vector2(Screen.width, 0f),
                        new Vector2(0f, Screen.height), new Vector2(Screen.width, Screen.height) })
                    {
                        typeof(AmmoDisplay).GetMethod("PositionPips", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(ammo, new object[] { cursor });
                        Canvas.ForceUpdateCanvases();
                        Camera uiCamera = mode == RenderMode.ScreenSpaceOverlay ? null : camera;
                        foreach (var pip in Read<RectTransform>(ammo, "pipRow").GetComponentsInChildren<Image>())
                        {
                            var corners = new Vector3[4];
                            pip.rectTransform.GetWorldCorners(corners);
                            foreach (var corner in corners)
                            {
                                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                                Assert.That(screen.x, Is.InRange(-0.5f, Screen.width + 0.5f), mode + " horizontal clipping");
                                Assert.That(screen.y, Is.InRange(-0.5f, Screen.height + 0.5f), mode + " vertical clipping");
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator Load(string name)
        {
            EventBus.Clear(); Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            level = SceneManager.GetSceneByName(name);
            foreach (var enemy in level.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyBase>()))
                enemy.MoveSpeed = 0f;
            yield return null;
        }

        private static void AssertPips(AmmoDisplay ammo, string sprite, float size, int count, int left)
        {
            var pips = Read<RectTransform>(ammo, "pipRow").GetComponentsInChildren<Image>();
            Assert.That(pips, Has.Length.EqualTo(count));
            for (int i = 0; i < pips.Length; i++)
            {
                Assert.That(pips[i].sprite.name, Is.EqualTo(sprite));
                Assert.That(pips[i].rectTransform.rect.size, Is.EqualTo(new Vector2(size, size)));
                Assert.That(pips[i].color.a, Is.EqualTo(i < left ? 1f : 0.2f).Within(0.001f));
            }
        }

        private T Find<T>() where T : Component => level.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).First();
        private static T Read<T>(object target, string field) => (T)target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
