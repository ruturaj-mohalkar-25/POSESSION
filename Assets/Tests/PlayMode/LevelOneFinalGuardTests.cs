using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class LevelOneFinalGuardTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("FinalGuardCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }

        [UnityTest] public IEnumerator FinalGuardRepeatedlyReachesBothSavedWaypoints()
        {
            yield return Load(CampaignLevels.FirstScene);
            Object.FindFirstObjectByType<SpiritController>().SetVisible(false);
            var guard=GameObject.Find("Guard_03").GetComponent<GuardEnemy>();guard.enabled=true;
            var route=Read<Transform[]>(guard,"waypoints");int last=-1,arrivals=0;
            float deadline=Time.time+20f;
            while(Time.time<deadline&&arrivals<4)
            {
                for(int i=0;i<route.Length;i++)
                {
                    var delta=guard.transform.position-route[i].position;delta.y=0;
                    if(delta.magnitude<=.35f&&last!=i){last=i;arrivals++;}
                }
                yield return null;
            }
            Assert.That(arrivals,Is.GreaterThanOrEqualTo(4),"Guard_03 must keep patrolling, rather than stop at its home-area edge.");
        }

        [UnityTest] public IEnumerator FinalGuardDetectsChasesAndShootsBelowTheForkBoundary()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();
            var guard=GameObject.Find("Guard_03").GetComponent<GuardEnemy>();
            var cc=guard.GetComponent<CharacterController>();cc.enabled=false;
            guard.transform.SetPositionAndRotation(new Vector3(16f,guard.transform.position.y,32f),Quaternion.LookRotation(Vector3.back));cc.enabled=true;
            spirit.RestoreAt(new Vector3(16f,1.4f,27f));Physics.SyncTransforms();
            Assert.That(guard.IsInsideHome(spirit.transform.position),Is.True,"The right passage must belong to this Guard's patrol area.");
            Assert.That(guard.CanSee(spirit.transform),Is.True,"The locked exit door is behind the Spirit, not between it and the Guard.");
            guard.enabled=true;guard.GetComponent<EnemyContactDamage>().enabled=true;
            yield return new WaitForSeconds(.12f);Assert.That(guard.IsChasing,Is.True);
            int hp=spirit.CurrentHP;float deadline=Time.time+2.2f;
            while(spirit.CurrentHP==hp&&Time.time<deadline)yield return null;
            Assert.That(spirit.CurrentHP,Is.LessThan(hp),"The final Guard must use its real projectile attack in the right passage.");
        }

        [UnityTest] public IEnumerator EverySavedPatrolEndpointIsReachableWithinItsHomeArea()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    float inset=enemy.GetComponent<CharacterController>().radius*enemy.transform.lossyScale.x+.05f;
                    float tolerance=Read<float>(enemy,"waypointArrivalDistance");
                    var b=enemy.HomeBounds;
                    foreach(var point in Read<Transform[]>(enemy,"waypoints"))
                    {
                        float edgeDistance=Mathf.Min(point.position.x-b.min.x,b.max.x-point.position.x,
                            point.position.z-b.min.z,b.max.z-point.position.z);
                        Assert.That(edgeDistance+tolerance,Is.GreaterThanOrEqualTo(inset-.01f),
                            scene+"/"+enemy.name+" cannot reach "+point.name+" before its room boundary stops its body.");
                    }
                }
            }
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(scene);
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<HuntedEnemyPressure>(FindObjectsSortMode.None))e.enabled=false;
            yield return null;
        }
        private static T Read<T>(object obj,string name)
        {for(var type=obj.GetType();type!=null;type=type.BaseType){var f=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null)return(T)f.GetValue(obj);}throw new System.Exception(name);}
    }
}
