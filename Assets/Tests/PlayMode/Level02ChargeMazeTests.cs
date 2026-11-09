using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class Level02ChargeMazeTests
    {
        private Level02Director level;
        private PossessionController pc;
        private PossessionCharge charge;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1f;EventBus.Clear();yield return SceneManager.LoadSceneAsync("Level_02");yield return null;
            Bind();Freeze();
        }
        private void Bind(){level=Object.FindFirstObjectByType<Level02Director>();pc=level.Possession;charge=level.Charge;}
        private static void Freeze()
        {
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<HuntedEnemyPressure>(FindObjectsSortMode.None))e.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1f;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("ChargeMazeCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [Test] public void SavedSceneHasTheDesignedRoomsHostsShardsAndIndependentRoutes()
        {
            Assert.That(Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Length,Is.EqualTo(8));
            foreach(string room in new[]{"R1","R2","R3","R4","R5","R6","R7","D1","D2","D3"})Assert.That(GameObject.Find(room),Is.Not.Null);
            Assert.That(charge.Count,Is.Zero);Assert.That(pc.CanStartPossession,Is.False);Assert.That(level.CanExit,Is.False);
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [Test] public void EmptySpiritCannotPossessAndInvalidAttemptsNeverSpendACollectedCharge()
        {
            var host=Host("Guard_Courtyard");Approach(host);Invoke(pc,"Possess",host);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));
            Collect("C1");Approach(host);Set(pc,"ejectCooldownTimer",1f);Invoke(pc,"Possess",host);
            Assert.That(charge.HasCharge,Is.True);Assert.That(charge.SuccessfulPossessions,Is.Zero);
            Set(pc,"ejectCooldownTimer",0f);Invoke(pc,"Possess",Host("Guard_North"));
            Assert.That(charge.HasCharge,Is.True);Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));
            Approach(host);Invoke(pc,"Possess",host);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Possessed_Guard));Assert.That(charge.Count,Is.Zero);
            Assert.That(charge.SuccessfulPossessions,Is.EqualTo(1));
        }
        [Test] public void WallBlockedPossessionDoesNotSpendTheCharge()
        {
            Collect("C1");level.Spirit.RestoreAt(Map(14.3f,11));
            var host=Host("Guard_Courtyard");Move(host,Map(16,11),Vector3.forward);Physics.SyncTransforms();
            Invoke(pc,"Possess",host);Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.HasCharge,Is.True);
        }
        [Test] public void GuardVisionBlocksChargedPossessionWithoutSpendingTheShard()
        {
            Collect("C1");var host=Host("Guard_Courtyard");Approach(host);
            Vector3 towardSpirit=level.Spirit.transform.position-host.transform.position;towardSpirit.y=0f;
            host.transform.rotation=Quaternion.LookRotation(towardSpirit);Physics.SyncTransforms();
            Assert.That(host.CanSee(level.Spirit.transform),Is.True);
            var nearest=typeof(PossessionController).GetMethod("FindNearestEnemy",BindingFlags.NonPublic|BindingFlags.Instance);
            Assert.That(nearest.Invoke(pc,null),Is.Not.SameAs(host));
            Invoke(pc,"Possess",host);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.HasCharge,Is.True);
            Assert.That(charge.SuccessfulPossessions,Is.Zero);

            host.transform.rotation=Quaternion.LookRotation(-towardSpirit);Physics.SyncTransforms();
            Assert.That(nearest.Invoke(pc,null),Is.SameAs(host));
            Invoke(pc,"Possess",host);
            Assert.That(pc.PossessedEnemy,Is.SameAs(host));Assert.That(charge.HasCharge,Is.False);
            Assert.That(charge.SuccessfulPossessions,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FullCapacityAndOccupiedBodiesCannotWastePickups()
        {
            Collect("C1");Collect("C2");Collect("C3");Assert.That(charge.Count,Is.EqualTo(3));
            var optional=Pickup("C4");level.Spirit.RestoreAt(optional.transform.position+Vector3.up*1.05f);
            Assert.That(optional.TryCollect(charge),Is.False);Assert.That(optional.IsReady,Is.True);
            var host=Host("Guard_Courtyard");Approach(host);Invoke(pc,"Possess",host);
            level.Spirit.transform.position=optional.transform.position+Vector3.up*1.05f;
            Assert.That(optional.TryCollect(charge),Is.False);Assert.That(optional.IsReady,Is.True);
            Assert.That(pc.TryEject(false),Is.True);Assert.That(charge.Count,Is.EqualTo(2));
            yield return null;
        }
        [UnityTest] public IEnumerator ReformingSourcesPermitRetriesAndTheOptionalShardIsOneUse()
        {
            var c=Pickup("C1");Set(c,"cooldown",0.05f);Collect("C1");charge.Clear();
            Assert.That(c.IsReady,Is.False);level.Spirit.RestoreAt(Map(10,14));
            yield return new WaitForSeconds(0.08f);Assert.That(c.IsReady,Is.True);Collect("C1");charge.Clear();
            Collect("C4");charge.Clear();yield return new WaitForSeconds(0.08f);
            Assert.That(Pickup("C4").IsReady,Is.False);
        }
        [UnityTest] public IEnumerator NaturalExpiryAndHostDefeatBothReturnAnEmptyVisibleSpirit()
        {
            Collect("C1");var host=Host("Guard_Courtyard");Approach(host);Set(pc,"possessDuration",0.05f);Invoke(pc,"Possess",host);
            yield return new WaitForSeconds(.1f);Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.Count,Is.Zero);
            Assert.That(host.IsHostile,Is.True);
            Collect("C2");yield return WaitForPossessionReady();var heavy=Host("Heavy_North");Approach(heavy);Set(pc,"possessDuration",10f);Invoke(pc,"Possess",heavy);
            Assert.That(pc.PossessedEnemy,Is.SameAs(heavy));heavy.TakeDamage(999);
            yield return new WaitForSeconds(.1f);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.Count,Is.Zero);Assert.That(level.Spirit.enabled,Is.True);
            Assert.That(level.Spirit.transform.position.y,Is.InRange(1f,1.5f));
        }
        [UnityTest] public IEnumerator OrdinaryGuardsCanHurtTheUnchargedSpirit()
        {
            var guard=Host("Guard_Courtyard");Approach(guard);
            level.Spirit.RestoreAt(guard.transform.position+Vector3.right*1.65f);
            guard.transform.rotation=Quaternion.LookRotation(level.Spirit.transform.position-guard.transform.position);
            Physics.SyncTransforms();
            guard.enabled=true;guard.GetComponent<HuntedEnemyPressure>().enabled=true;
            int before=level.Spirit.CurrentHP;yield return new WaitForSeconds(.6f);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(before),"Contact damage must give time to retreat.");
            yield return new WaitForSeconds(.85f);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(before-1));
            Assert.That(level.TryContactDamage(),Is.False,"Nearby enemies must share the contact grace period.");
            yield return new WaitForSeconds(1.4f);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(before-1),"Repeated damage must be slower than the original 1.4 seconds.");
        }
        [Test] public void HostileProjectilesCannotSolveTheTargets()
        {
            var target=GameObject.Find("TN").GetComponent<Target>();
            var shot=new GameObject("Hostile test shot").AddComponent<Projectile>();shot.gameObject.AddComponent<EnemyShot>().Init(1,6f);
            Invoke(shot,"HandleHit",target.GetComponent<Collider>());Assert.That(target.IsHit,Is.False);Object.Destroy(shot.gameObject);
        }
        [UnityTest] public IEnumerator NorthSolutionUsesGuardThenRechargeThenRealHeavySmash()
        {
            Collect("C1");var guard=Host("Guard_Courtyard");Approach(guard);Invoke(pc,"Possess",guard);
            yield return Walk(guard,Map(12,8),Map(12,5),Map(12.5f,4.5f));
            yield return Shoot(guard,GameObject.Find("TN").GetComponent<Target>());
            yield return Walk(guard,Map(15,3),Map(20.3f,3));
            Assert.That(pc.TryEject(false),Is.True);Assert.That(charge.HasCharge,Is.False);
            Collect("C2");yield return WaitForPossessionReady();var heavy=Host("Heavy_North");Approach(heavy);Invoke(pc,"Possess",heavy);
            yield return Walk(heavy,Map(26,5.5f),Map(26,6));Smash(heavy,GameObject.Find("BN").GetComponent<BreakableWall>(),Vector3.back);
            Assert.That(level.NorthReady,Is.True);Assert.That(level.SouthReady,Is.False);
            Assert.That(charge.SuccessfulPossessions,Is.EqualTo(2));
            Move(heavy,Object.FindFirstObjectByType<Level02Exit>().transform.position,Vector3.forward);
            Assert.That(Object.FindFirstObjectByType<Level02Exit>().TryFinish(heavy.GetComponent<Collider>()),Is.True);
        }
        [UnityTest] public IEnumerator SouthSolutionUsesHeavyThenRechargeThenGuardAndAcceptsSpiritAtExit()
        {
            Collect("C1");var heavy=Host("Heavy_Courtyard");Approach(heavy);Invoke(pc,"Possess",heavy);
            yield return Walk(heavy,Map(12,16));Smash(heavy,GameObject.Find("BS").GetComponent<BreakableWall>(),Vector3.back);
            yield return Walk(heavy,Map(12,19.5f));Assert.That(pc.TryEject(false),Is.True);
            Collect("C3");yield return WaitForPossessionReady();var guard=Host("Guard_South");Approach(guard);Invoke(pc,"Possess",guard);
            yield return Walk(guard,Map(13.3f,21));yield return Shoot(guard,GameObject.Find("TS").GetComponent<Target>());
            yield return Walk(guard,Map(16.5f,21),Map(20.3f,21));
            Assert.That(level.SouthReady,Is.True);Assert.That(level.NorthReady,Is.False);Assert.That(charge.SuccessfulPossessions,Is.EqualTo(2));
            Assert.That(pc.TryEject(false),Is.True);var exit=Object.FindFirstObjectByType<Level02Exit>();level.Spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();
            Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);
        }
        [UnityTest] public IEnumerator CourtyardRetryResetsPuzzleHostsSourcesAndStartsEmptyBeforeC1()
        {
            level.MarkCheckpoint();Collect("C1");GameObject.Find("TN").GetComponent<Target>().OnHit();GameObject.Find("BN").GetComponent<BreakableWall>().TakeDamage(2);
            Assert.That(level.NorthReady,Is.True);Host("Guard_Courtyard").TakeDamage(999);
            level.RetryCourtyard();yield return null;yield return null;Bind();Freeze();yield return new WaitForSeconds(.1f);
            Assert.That(level.CheckpointReached,Is.True);Assert.That(level.Charge.Count,Is.Zero);Assert.That(level.CanExit,Is.False);
            Assert.That(Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length,Is.EqualTo(8));
            foreach(var c in Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))Assert.That(c.IsReady,Is.True);
            Assert.That(Vector3.Distance(level.Spirit.transform.position,Map(10,14)),Is.LessThan(.3f));
            level.FullRestart();yield return null;yield return null;Bind();Freeze();Assert.That(level.CheckpointReached,Is.False);Assert.That(charge.Count,Is.Zero);
        }
        [Test] public void ExitRejectsAnUnsolvedRouteAndUnoccupiedEnemies()
        {
            var exit=Object.FindFirstObjectByType<Level02Exit>();Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);
            GameObject.Find("TN").GetComponent<Target>().OnHit();GameObject.Find("BN").GetComponent<BreakableWall>().TakeDamage(2);
            Assert.That(exit.TryFinish(Host("Heavy_Exit").GetComponent<Collider>()),Is.False);
        }
        private IEnumerator WaitForPossessionReady()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!pc.CanStartPossession && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(pc.CanStartPossession, Is.True,
                "The real ejection cooldown must expire before the next charged possession.");
        }

        private void Collect(string name)
        {
            var p=Pickup(name);level.Spirit.RestoreAt(p.transform.position+Vector3.up*1.05f);Physics.SyncTransforms();
            Assert.That(p.TryCollect(charge),Is.True,name+" could not be collected");
        }
        private void Approach(EnemyBase host)
        {
            var options=new[]{-host.transform.forward,Vector3.right,Vector3.back,Vector3.left,Vector3.forward};
            foreach(var dir in options)
            {
                var p=host.transform.position+dir*1.8f;p.y=1.15f;
                if(Physics.OverlapCapsule(p+Vector3.up*.45f,p-Vector3.up*.45f,.5f,1<<0,QueryTriggerInteraction.Ignore)
                    .Any(c => !c.transform.IsChildOf(level.Spirit.transform)))continue;
                level.Spirit.RestoreAt(p);Physics.SyncTransforms();
                if(!host.CanBePossessedFrom(level.Spirit.transform))continue;
                return;
            }
            Assert.Fail("No clear host approach: "+host.name);
        }
        private IEnumerator Walk(EnemyBase body,params Vector3[] points)
        {
            foreach(var target in points)
            {
                float deadline=Time.time+15f;
                while(true)
                {
                    var delta=target-body.transform.position;delta.y=0;if(delta.magnitude<.14f)break;
                    Assert.That(pc.PossessedEnemy,Is.SameAs(body),"Possession expired before reaching "+target+" from "+body.transform.position);
                    Assert.That(Time.time,Is.LessThan(deadline),"Route obstructed for "+body.name+" near "+body.transform.position);
                    body.transform.rotation=Quaternion.LookRotation(delta);
                    body.GetComponent<CharacterController>().Move(delta.normalized*Mathf.Min(delta.magnitude,body.MoveSpeed*Time.deltaTime));yield return null;
                }
            }
        }
        private static IEnumerator Shoot(EnemyBase body,Target target)
        {
            var cam=Camera.main;var saved=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up,0).Raycast(ray,out float distance),Is.True);
            var delta=target.transform.position-ray.GetPoint(distance);delta.y=0;cam.transform.position+=delta;
            body.GetComponent<ShootAbility>().TryExecute();cam.transform.position=saved;
            float until=Time.time+1.5f;while(!target.IsHit && Time.time<until)yield return null;
            Assert.That(target.IsHit,Is.True,"The actual shared projectile did not reach "+target.name);
        }
        private static void Smash(EnemyBase body,BreakableWall wall,Vector3 forward)
        {body.transform.rotation=Quaternion.LookRotation(forward);Physics.SyncTransforms();body.GetComponent<SmashAbility>().TryExecute();Assert.That(wall.CurrentHP,Is.LessThanOrEqualTo(0));}
        private static void Move(EnemyBase body,Vector3 at,Vector3 forward)
        {var cc=body.GetComponent<CharacterController>();cc.enabled=false;at.y=body.transform.position.y;body.transform.SetPositionAndRotation(at,Quaternion.LookRotation(forward));cc.enabled=true;Physics.SyncTransforms();}
        private static Vector3 Map(float x,float y)=>new((x-17f)*1.6f,1.15f,(12f-y)*1.6f);
        private static EnemyBase Host(string name)=>GameObject.Find(name).GetComponent<EnemyBase>();
        private static PossessionChargePickup Pickup(string name)=>GameObject.Find(name).GetComponent<PossessionChargePickup>();
        private static void Invoke(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    }
}
