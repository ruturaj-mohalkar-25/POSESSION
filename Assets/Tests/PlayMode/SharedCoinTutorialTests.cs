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
    public sealed class SharedCoinTutorialTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1f;
            var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("SharedCoinCleanup_"+System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator EveryPlayableSceneHasTheSameChargePolicyHudAndUsableCoins()
        {
            foreach(string name in new[]{TutorialScene.Name,"Level_01","Level_03","Level_02"})
            {
                yield return Load(name);
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                var charges=spirit.GetComponent<PossessionCharge>();
                Assert.That(charges,Is.Not.Null,name);
                Assert.That(charges.Count,Is.EqualTo(1),name);
                Assert.That(charges.MaxCharges,Is.Zero,name);
                var coins=Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None);
                Assert.That(coins.Length,Is.GreaterThanOrEqualTo(2),name);
                foreach(var coin in coins)
                {
                    Assert.That(coin.GetComponentsInChildren<Transform>().Any(t=>t.name=="Soul Coin"),Is.True,name);
                    Assert.That(Physics.Raycast(coin.transform.position+Vector3.up*2f,Vector3.down,out var floor,4f,~0,QueryTriggerInteraction.Ignore),Is.True,name+" coin has no floor");
                    Assert.That(floor.normal.y,Is.GreaterThan(.5f));
                }
                var hud=Object.FindFirstObjectByType<HUDManager>();
                Assert.That(hud.GetComponentsInChildren<Text>(true).Any(t=>t.name=="ChargeCountText"&&t.text=="SOUL COINS 1"),Is.True,name);
            }
        }

        [UnityTest]
        public IEnumerator TutorialCompletesWithRealCoinsAbilitiesAndNoIntermediateModal()
        {
            yield return Load(TutorialScene.Name);
            var guide=Object.FindFirstObjectByType<TutorialGuide>();
            var pc=guide.Possession;
            foreach(var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))attack.enabled=false;
            SetBase(guide.Guard,"moveSpeed",0f);SetBase(guide.Heavy,"moveSpeed",0f);
            guide.BeginPractice();guide.Progress.Move(TutorialMovement.All);
            yield return null;
            AssertRunning(guide);
            var coins=Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.z).ToArray();
            Collect(guide,coins[0]);Assert.That(guide.Charges.Count,Is.EqualTo(2));
            Possess(guide,guide.Guard);Assert.That(guide.Charges.Count,Is.EqualTo(1));AssertRunning(guide);
            var target=Object.FindFirstObjectByType<Target>();
            var cam=Camera.main;var old=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up,0f).Raycast(ray,out float distance),Is.True);
            var delta=target.transform.position-ray.GetPoint(distance);delta.y=0;cam.transform.position+=delta;
            guide.Guard.GetComponent<ShootAbility>().TryExecute();cam.transform.position=old;
            yield return new WaitForSeconds(.5f);
            Assert.That(target.IsHit,Is.True,"The tutorial Guard's actual shot must unlock the door.");
            Assert.That(pc.TryEject(false),Is.True);Assert.That(guide.Charges.Count,Is.EqualTo(1));AssertRunning(guide);
            yield return new WaitForSeconds(.6f);
            Set(pc,"possessDuration",.1f);Set(pc,"hostileTime",.3f);
            Possess(guide,guide.Guard);
            yield return new WaitForSeconds(.2f);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(guide.Progress.ForcedOut,Is.True);
            yield return new WaitForSeconds(.6f);
            Assert.That(guide.Progress.SurvivedHostility,Is.True);Assert.That(guide.Charges.Count,Is.Zero);
            StringAssert.Contains("coin",guide.Instruction.ToLowerInvariant());AssertRunning(guide);
            Collect(guide,coins[1]);Set(pc,"possessDuration",12f);
            Possess(guide,guide.Heavy);AssertRunning(guide);
            var wall=Object.FindFirstObjectByType<BreakableWall>();var cc=guide.Heavy.GetComponent<CharacterController>();
            cc.enabled=false;var location=wall.transform.position-Vector3.forward*1.6f;location.y=1.45f;
            guide.Heavy.transform.SetPositionAndRotation(location,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
            guide.Heavy.GetComponent<SmashAbility>().TryExecute();
            Assert.That(wall.CurrentHP,Is.LessThanOrEqualTo(0));Assert.That(guide.Progress.BrokeWall,Is.True);
            Assert.That(pc.TryEject(false),Is.True);AssertRunning(guide);
            var exit=Object.FindFirstObjectByType<LevelExit>();guide.Spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();
            typeof(LevelExit).GetMethod("OnTriggerStay",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(exit,new object[]{guide.Spirit.GetComponent<Collider>()});
            yield return null;
            Assert.That(guide.Progress.Completed,Is.True);Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));
            var modal=guide.GetComponent<TutorialOverlay>().GetComponentsInChildren<Transform>(true).Single(t=>t.name=="PracticeModal");
            Assert.That(modal.gameObject.activeInHierarchy,Is.True);
        }

        [UnityTest]
        public IEnumerator PracticeCoinReformsAfterBeingSpentSoMissedShotsCannotExhaustTheLesson()
        {
            yield return Load(TutorialScene.Name);
            var guide=Object.FindFirstObjectByType<TutorialGuide>();guide.BeginPractice();guide.Progress.Move(TutorialMovement.All);
            var coin=Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.z).First();
            Assert.That(coin.Reforms,Is.True);Set(coin,"cooldown",.1f);guide.Charges.Clear();Collect(guide,coin);
            guide.Charges.Clear();guide.Spirit.RestoreAt(new Vector3(0f,1.15f,4f));
            Assert.That(coin.IsReady,Is.False);yield return new WaitForSeconds(.2f);
            Assert.That(coin.IsReady,Is.True);Collect(guide,coin);Assert.That(guide.Charges.Count,Is.EqualTo(1));
        }

        private static void AssertRunning(TutorialGuide guide)
        {
            Assert.That(Time.timeScale,Is.EqualTo(1f));Assert.That(guide.IsCoaching,Is.False);
            var modal=guide.GetComponent<TutorialOverlay>().GetComponentsInChildren<Transform>(true).Single(t=>t.name=="PracticeModal");
            Assert.That(modal.gameObject.activeInHierarchy,Is.False);
        }
        private static void Collect(TutorialGuide guide,PossessionChargePickup coin)
        {
            guide.Spirit.RestoreAt(coin.transform.position+Vector3.up*1.05f);Physics.SyncTransforms();
            Assert.That(coin.TryCollect(guide.Charges),Is.True,coin.name);
        }
        private static void Possess(TutorialGuide guide,EnemyBase enemy)
        {
            guide.Spirit.RestoreAt(enemy.transform.position-enemy.transform.forward*2.5f);Physics.SyncTransforms();
            typeof(PossessionController).GetMethod("Possess",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(guide.Possession,new object[]{enemy});
            Assert.That(guide.Possession.PossessedEnemy,Is.SameAs(enemy));
        }
        private static IEnumerator Load(string name)
        {
            Time.timeScale=1f;EventBus.Clear();yield return SceneManager.LoadSceneAsync(name);yield return null;
        }
        private static void Set(object item,string field,object value)=>item.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,value);
        private static void SetBase(object item,string field,object value)=>typeof(EnemyBase).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,value);
    }
}
