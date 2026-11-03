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
    public sealed class WebPreviewTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("WebPreviewCleanup_" + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator LevelSelectTwoOpensOldLevelThreeAndThreeOpensTheChargeMaze()
        {
            yield return Load("MainMenu");
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            CollectionAssert.AreEqual(new[] { "Level_01", "Level_03", "Level_02" }, Read<string[]>(menu, "levelScenes"));
            menu.OnPlayButton();
            Read<Button[]>(menu, "levelButtons")[1].onClick.Invoke();
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_03"));
            Assert.That(Object.FindFirstObjectByType<Level02Director>(), Is.Null);
            Assert.That(Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            GameManager.Instance.QuitToMenu();
            yield return null; yield return null;
            menu = Object.FindFirstObjectByType<MainMenuController>();
            menu.OnPlayButton();
            Read<Button[]>(menu, "levelButtons")[2].onClick.Invoke();
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_02"));
            Assert.That(Object.FindFirstObjectByType<Level02Director>(), Is.Not.Null);
            var coins = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None);
            Assert.That(coins.Length, Is.EqualTo(4));
            foreach (var coin in coins) Assert.That(coin.GetComponentsInChildren<Transform>().Any(t => t.name == "Soul Coin"), Is.True);
            var charge = Object.FindFirstObjectByType<SpiritController>().GetComponent<PossessionCharge>();
            Assert.That(charge.Count, Is.Zero);
            Assert.That(charge.MaxCharges, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NextLevelFollowsMenuOrderAndTheMazeHasTheFinalFinishScreen()
        {
            string[] expected = { "Level_01", "Level_03", "Level_02" };
            yield return Load(expected[0]);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(expected[i]));
                EventBus.LevelCompleted?.Invoke();
                var hud = Object.FindFirstObjectByType<HUDManager>();
                Assert.That(Read<Text>(hud, "nextButtonLabel").text, Is.EqualTo(i == 2 ? "FINISH" : "NEXT LEVEL"));
                if (i < 2)
                {
                    GameManager.Instance.LoadNextLevel();
                    yield return null; yield return null;
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                }
                else
                {
                    GameManager.Instance.LoadNextLevel();
                    Assert.That(Read<GameObject>(hud, "gameCompletePanel").activeSelf, Is.True);
                    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_02"));
                }
            }
        }

        [UnityTest]
        public IEnumerator LevelOneAndTutorialKeepTheIntegrationCameraAndFreePossession()
        {
            foreach (string name in new[] { "Level_01", TutorialScene.Name })
            {
                yield return Load(name);
                var spirit = Object.FindFirstObjectByType<SpiritController>();
                Assert.That(spirit.GetComponent<PossessionCharge>(), Is.Null);
                Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None), Is.Empty);
                Assert.That(Camera.main.fieldOfView, Is.EqualTo(40f));
                Assert.That(Read<Vector3>(Camera.main.GetComponent<CameraFollow>(), "offset"), Is.EqualTo(new Vector3(0f,25f,-13f)));
            }
        }

        [UnityTest]
        public IEnumerator MazeContactPressureRespectsTheSameTwoSecondEjectGrace()
        {
            yield return Load("Level_02");
            var level = Object.FindFirstObjectByType<Level02Director>();
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                enemy.enabled = false;
                enemy.GetComponent<EnemyContactDamage>().enabled = false;
                enemy.GetComponent<HuntedEnemyPressure>().enabled = false;
            }
            var guard = GameObject.Find("Guard_Courtyard").GetComponent<GuardEnemy>();
            var attack = guard.GetComponent<EnemyContactDamage>();
            Set(attack,"spotReactionTime",20f);
            attack.enabled = true;
            var pressure = guard.GetComponent<HuntedEnemyPressure>();
            Set(pressure,"windup",.05f);
            level.Charge.TryCollect();
            level.Spirit.RestoreAt(guard.transform.position - guard.transform.forward * 1.7f);
            Physics.SyncTransforms();
            typeof(PossessionController).GetMethod("Possess",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(level.Possession,new object[] { guard });
            Assert.That(level.Possession.PossessedEnemy,Is.SameAs(guard));
            Assert.That(level.Possession.TryEject(false),Is.True);
            Assert.That(attack.IsInEjectGrace,Is.True);
            level.Spirit.RestoreAt(guard.transform.position + Vector3.right * 1.65f);
            guard.transform.rotation=Quaternion.LookRotation(level.Spirit.transform.position-guard.transform.position);
            typeof(EnemyBase).GetField("chaseTarget",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(guard,level.Spirit);
            pressure.enabled = true;
            Physics.SyncTransforms();
            int hp=level.Spirit.CurrentHP;
            yield return new WaitForSeconds(1.3f);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));
            yield return new WaitForSeconds(1f);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp-1));
        }

        private static IEnumerator Load(string name)
        {
            Time.timeScale=1f;EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(name);yield return null;
        }
        private static T Read<T>(object target,string field) => (T)target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        private static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    }
}
