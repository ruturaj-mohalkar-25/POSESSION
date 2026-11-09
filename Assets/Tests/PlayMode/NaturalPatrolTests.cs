using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class NaturalPatrolTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PatrolFeelCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [UnityTest] public IEnumerator AllStagesShareCalmPatrolPaceAndTheSameDetectionRules()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    Assert.That(enemy.PatrolSpeed,Is.EqualTo(enemy is HeavyEnemy?2f:2.5f));
                    Assert.That(enemy.MoveSpeed,Is.EqualTo(enemy is HeavyEnemy?4f:5f));
                    Assert.That(enemy.DetectionRange,Is.EqualTo(enemy is HeavyEnemy?7f:9f));
                    Assert.That(enemy.VisionAngle,Is.EqualTo(75f));
                }
            }
        }
        [UnityTest] public IEnumerator GuardTurnsSmoothlyAndLooksAroundWhileStopped()
        {
            yield return Load(CampaignLevels.ThirdScene);
            var guard=GameObject.Find("Sorting_hall_Guard").GetComponent<GuardEnemy>();
            guard.transform.rotation=Quaternion.LookRotation(Vector3.back);Quaternion start=guard.transform.rotation;
            guard.enabled=true;yield return new WaitForSeconds(.12f);
            float turned=Quaternion.Angle(start,guard.transform.rotation);
            Assert.That(turned,Is.InRange(10f,60f),"Turning must be visible rather than an instantaneous right-angle snap.");
            float deadline=Time.time+5f;while(!guard.IsIdle&&Time.time<deadline)yield return null;
            Assert.That(guard.IsIdle,Is.True);Vector3 at=guard.transform.position;var facing=guard.transform.rotation;
            yield return new WaitForSeconds(.3f);
            Assert.That(Vector3.Distance(at,guard.transform.position),Is.LessThan(.05f),"A look-around pause should stay in place.");
            Assert.That(Quaternion.Angle(facing,guard.transform.rotation),Is.GreaterThan(1f));
        }
        [UnityTest] public IEnumerator WarehouseLoopsClearCoverWallsAndLockedPuzzlesWithFullBodies()
        {
            yield return Load(CampaignLevels.ThirdScene);Physics.SyncTransforms();
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                var route=Read<Transform[]>(enemy,"waypoints");Assert.That(route.Length,Is.EqualTo(4));
                Assert.That(Vector3.Cross(route[1].position-route[0].position,route[2].position-route[1].position).magnitude,Is.GreaterThan(1f),"Use a loop, not four collinear left/right points.");
                var cc=enemy.GetComponent<CharacterController>();float radius=cc.radius*enemy.transform.lossyScale.x*.98f;
                float shaft=cc.height*enemy.transform.lossyScale.y*.5f-radius;
                foreach(var pair in Enumerable.Range(0,route.Length).Select(i=>(a:route[i].position,b:route[(i+1)%route.Length].position)))
                {
                    var center=pair.a+enemy.transform.TransformVector(cc.center);
                    Assert.That(Physics.CheckCapsule(center+Vector3.up*shaft,center-Vector3.up*shaft,radius,(1<<0)|(1<<7),QueryTriggerInteraction.Ignore),Is.False,enemy.name+" waypoint overlap");
                    var delta=pair.b-pair.a;
                    Assert.That(Physics.CapsuleCast(center+Vector3.up*shaft,center-Vector3.up*shaft,radius,delta.normalized,out var hit,delta.magnitude,(1<<0)|(1<<7),QueryTriggerInteraction.Ignore),Is.False,enemy.name+" patrol segment blocked by "+(hit.collider!=null?hit.collider.name:"none"));
                }
            }
        }
        [UnityTest] public IEnumerator EveryWarehouseEnemyCompletesAWholeLoopWithoutStalling()
        {
            yield return Load(CampaignLevels.ThirdScene);
            var enemies=Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            var routes=enemies.Select(e=>Read<Transform[]>(e,"waypoints")).ToArray();
            var last=Enumerable.Repeat(-1,enemies.Length).ToArray();var arrivals=new int[enemies.Length];
            foreach(var enemy in enemies)enemy.enabled=true;
            float deadline=Time.time+25f;
            while(arrivals.Any(n=>n<5)&&Time.time<deadline)
            {
                for(int i=0;i<enemies.Length;i++)for(int j=0;j<routes[i].Length;j++)
                {
                    var delta=enemies[i].transform.position-routes[i][j].position;delta.y=0;
                    if(delta.magnitude<=.35f&&last[i]!=j){last[i]=j;arrivals[i]++;}
                }
                yield return null;
            }
            for(int i=0;i<enemies.Length;i++)Assert.That(arrivals[i],Is.GreaterThanOrEqualTo(5),enemies[i].name+" stalled before completing its loop.");
        }
        private static IEnumerator Load(string scene)
        {
            Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(scene);
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<HuntedEnemyPressure>(FindObjectsSortMode.None))e.enabled=false;
            Object.FindFirstObjectByType<SpiritController>().SetVisible(false);
            yield return null;
        }
        private static T Read<T>(object obj,string name)
        {for(var type=obj.GetType();type!=null;type=type.BaseType){var f=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null)return(T)f.GetValue(obj);}throw new System.Exception(name);}
    }
}
