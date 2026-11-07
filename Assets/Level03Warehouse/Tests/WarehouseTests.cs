#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Warehouse.Tests
{
    public sealed class WarehouseTests
    {
        private const string Path="Assets/Level03Warehouse/Level_03_Warehouse.unity";
        private WarehouseLevel level;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;EventBus.Clear();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;Bind();
        }
        private void Bind()
        {
            level=Object.FindFirstObjectByType<WarehouseLevel>();
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("WarehouseCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [Test] public void PreviewUsesSharedActorsPuzzlesHUDAndThePlannedCounts()
        {
            Assert.That(Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<Target>(FindObjectsSortMode.None).Length,Is.EqualTo(16));
            Assert.That(Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Length,Is.EqualTo(13));
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var socket=Object.FindFirstObjectByType<WarehouseHealthSocket>();Assert.That(socket,Is.Not.Null);
            Assert.That(socket.IsGameplayBound,Is.False,"Health must wait for the team's shared healing API.");
            Assert.That(socket.SuggestedRestoreFraction,Is.EqualTo(.3f));
            Assert.That(Camera.main.GetComponent<CameraFollow>(),Is.Not.Null);
            Assert.That(GameManager.Instance.LevelTimer,Is.InRange(418f,420f));
            Assert.That(level.Possession.PossessDuration,Is.EqualTo(12f));
        }
        [Test] public void RoomsAndPassagesProvideSpaceToCircleCoverAndPlan()
        {
            var room=GameObject.Find("Guard_room_Floor").GetComponent<Collider>().bounds;
            Assert.That(room.size.x,Is.EqualTo(22.1f).Within(.01f));
            Assert.That(room.size.z,Is.EqualTo(14.3f).Within(.01f));
            var sorting=GameObject.Find("Sorting_hall_Floor").GetComponent<Collider>().bounds;
            Assert.That(sorting.size.x,Is.EqualTo(16.9f).Within(.01f));
            Assert.That(sorting.size.z,Is.EqualTo(13f).Within(.01f));
            foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                var bounds=door.GetComponent<Collider>().bounds;
                Assert.That(Mathf.Max(bounds.size.x,bounds.size.z),Is.GreaterThanOrEqualTo(6.49f));
            }
            // Crates stay compact; enlarging a room must create usable floor space.
            Assert.That(GameObject.Find("Cover_0").GetComponent<Collider>().bounds.size.x,Is.EqualTo(2.5f).Within(.01f));
            // A large manoeuvring capsule can traverse the fully solved left route.
            Open("inspection");Open("exit-passage");Assert.That(Reach(70,88,70,9,1.2f),Is.True);
        }
        [Test] public void MiddleRoomsHaveWideUnobstructedTurningAndPlanningAreas()
        {
            var recovery=GameObject.Find("Recovery_passage_Floor").GetComponent<Collider>().bounds;
            Assert.That(recovery.size.x,Is.EqualTo(23.4f).Within(.01f));
            Assert.That(recovery.size.z,Is.EqualTo(7.8f).Within(.01f));
            foreach(var point in new[]{new Vector2(60,48),new Vector2(70,49),new Vector2(80,48),
                new Vector2(70,63),new Vector2(70,29),new Vector2(70,8)})
            {
                var at=Map(point.x,point.y);at.y=2f;
                Assert.That(Physics.Raycast(at,Vector3.down,3f,1<<0,QueryTriggerInteraction.Ignore),Is.True);
                var obstacles=Physics.OverlapCapsule(at+Vector3.up*.3f,at-Vector3.up*.3f,1.4f,
                    (1<<0)|(1<<7),QueryTriggerInteraction.Ignore)
                    .Where(c=>c.GetComponentInParent<SpiritController>()==null).ToArray();
                Assert.That(obstacles,Is.Empty,"Middle turning space obstructed at "+point);
            }
        }
        [UnityTest] public IEnumerator DoorsRequireAllDistinctTargetsAndKeepProgress()
        {
            foreach(var view in Object.FindObjectsByType<WarehouseDoorView>(FindObjectsSortMode.None))
            {
                var door=view.GetComponent<Door>();int events=0;System.Action<Target> hit=t=>events++;
                EventBus.TargetHit+=hit;Assert.That(door.IsOpen,Is.False);
                view.Targets[2].OnHit();view.Targets[2].OnHit();Assert.That(events,Is.EqualTo(1));Assert.That(door.IsOpen,Is.False);
                foreach(var target in view.Targets.Where(t=>t!=view.Targets[2]))
                {
                    Assert.That(door.IsOpen,Is.False);target.OnHit();
                    Assert.That(door.IsOpen,Is.EqualTo(view.Targets.All(t=>t.IsHit)));
                }
                Assert.That(door.IsOpen,Is.True);
                Assert.That(door.GetComponent<Collider>().isTrigger,Is.True);EventBus.TargetHit-=hit;
            }
            yield return new WaitForSeconds(.5f);
            foreach(var view in Object.FindObjectsByType<WarehouseDoorView>(FindObjectsSortMode.None))Assert.That(view.Opening,Is.EqualTo(1f));
        }
        [Test] public void ClosedRoutesCannotBeBypassedAndTheLeftWingWorksIndependently()
        {
            Assert.That(Reach(70,88,70,9),Is.False,"Locked wing bypass.");
            Open("inspection");Assert.That(Reach(70,88,70,9),Is.False,"Second left door bypass.");
            Open("exit-passage");Assert.That(level.LeftReady,Is.True);Assert.That(level.RightReady,Is.False);
            Assert.That(Reach(70,88,70,9),Is.True,"Heavy-width physical route must fit through the left wing.");
            Assert.That(Wall("Left_recovery_wall").gameObject.activeSelf,Is.True);
            Assert.That(Wall("Right_recovery_wall").gameObject.activeSelf,Is.True);
        }
        [Test] public void RightWingWorksIndependentlyAndRequiresTheLoadingSmash()
        {
            Open("cargo-entry");Open("cargo");Assert.That(Reach(70,88,70,9),Is.False);
            Wall("Loading_wall").TakeDamage(2);Physics.SyncTransforms();
            Assert.That(level.RightReady,Is.True);Assert.That(level.LeftReady,Is.False);
            Assert.That(Reach(70,88,70,9),Is.True,"Right wing route blocked after its own puzzles.");
        }
        [Test] public void RecoveryHasTwoSealedEntrancesAndEitherSideCanReachHealthThenRetreat()
        {
            Assert.That(Reach(45,48,70,49),Is.False);Assert.That(Reach(109,47,70,49),Is.False);
            Wall("Left_recovery_wall").TakeDamage(2);Physics.SyncTransforms();
            Assert.That(Reach(45,48,70,49),Is.True);Assert.That(Reach(70,49,45,48),Is.True);
            Assert.That(Reach(109,47,70,49),Is.False);
            Wall("Right_recovery_wall").TakeDamage(2);Physics.SyncTransforms();
            Assert.That(Reach(109,47,70,49),Is.True);Assert.That(Reach(70,49,109,47),Is.True);
        }
        [UnityTest] public IEnumerator SharedGuardShotHitsTargetAndEnemyShotCannotSolveIt()
        {
            var target=GameObject.Find("cargo-entry_Target_1").GetComponent<Target>();
            var hostile=new GameObject("Test enemy shot").AddComponent<Projectile>();hostile.gameObject.AddComponent<EnemyShot>().Init(1,6);
            Invoke(hostile,"HandleHit",target.GetComponent<Collider>());Object.Destroy(hostile.gameObject);Assert.That(target.IsHit,Is.False);
            var guard=GameObject.Find("Sorting_hall_Guard").GetComponent<GuardEnemy>();
            Move(guard,Map(59,65),Vector3.forward);guard.OnPossessed();
            var cam=Camera.main;var saved=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up,0).Raycast(ray,out float distance),Is.True);
            var delta=target.transform.position-ray.GetPoint(distance);delta.y=0;cam.transform.position+=delta;
            guard.GetComponent<ShootAbility>().TryExecute();cam.transform.position=saved;
            yield return new WaitForSeconds(.5f);Assert.That(target.IsHit,Is.True,"Actual shared Guard projectile must reach target.");
        }
        [UnityTest] public IEnumerator EveryTargetHasAClearInRoomFiringPositionForTheSharedGuardAbility()
        {
            var guard=GameObject.Find("Sorting_hall_Guard").GetComponent<GuardEnemy>();
            var ability=guard.GetComponent<ShootAbility>();
            foreach(var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
            {
                bool found=false;
                foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
                {
                    var at=target.transform.position-direction*2.5f;at.y=1.15f;
                    if(!Physics.Raycast(at,Vector3.down,2f,1<<0,QueryTriggerInteraction.Ignore))continue;
                    if(Physics.OverlapCapsule(at+Vector3.up*.4f,at-Vector3.up*.4f,.55f,(1<<0)|(1<<7),QueryTriggerInteraction.Ignore)
                        .Any(c=>c.GetComponentInParent<SpiritController>()==null))continue;
                    var origin=at+direction*.8f+Vector3.up*.5f;
                    if(!Physics.Raycast(origin,direction,out var hit,3f,~(1<<6),QueryTriggerInteraction.Ignore)||hit.collider.GetComponent<Target>()!=target)continue;
                    Move(guard,at,direction);found=true;break;
                }
                Assert.That(found,Is.True,"No in-room firing position for "+target.name);
                guard.OnPossessed();Set(typeof(Ability),ability,"cooldownTimer",0f);
                var cam=Camera.main;var saved=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);
                Assert.That(new Plane(Vector3.up,0).Raycast(ray,out float distance),Is.True);
                var delta=target.transform.position-ray.GetPoint(distance);delta.y=0;cam.transform.position+=delta;
                ability.TryExecute();cam.transform.position=saved;
                float deadline=Time.time+1;while(!target.IsHit&&Time.time<deadline)yield return null;
                Assert.That(target.IsHit,Is.True,"Shared projectile could not reach "+target.name);
            }
        }
        private static void Set(System.Type type,object obj,string field,object value)=>type.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        [Test] public void SharedSmashOpensFullBarrierWhileGuardDamageCannot()
        {
            var wall=Wall("Left_recovery_wall");int hp=wall.CurrentHP;wall.TakeDamage(1);Assert.That(wall.CurrentHP,Is.EqualTo(hp));
            var heavy=GameObject.Find("Service_lobby_Heavy").GetComponent<HeavyEnemy>();
            Move(heavy,wall.transform.position-Vector3.right*1.5f,Vector3.right);heavy.OnPossessed();
            heavy.GetComponent<SmashAbility>().TryExecute();Assert.That(wall.CurrentHP,Is.LessThanOrEqualTo(0));
            Assert.That(wall.gameObject.activeSelf,Is.False);
        }
        [UnityTest] public IEnumerator SharedChargePossessionAndExpiryPreserveTargetProgress()
        {
            var target=GameObject.Find("inspection_Target_1").GetComponent<Target>();target.OnHit();
            var guard=GameObject.Find("Sorting_hall_Guard").GetComponent<GuardEnemy>();
            var charge=level.Spirit.GetComponent<PossessionCharge>();Assert.That(charge.TryCollect(),Is.True);
            level.Spirit.RestoreAt(guard.transform.position-Vector3.forward*1.8f);guard.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
            Invoke(level.Possession,"Possess",guard);Assert.That(level.Possession.PossessedEnemy,Is.SameAs(guard));
            Set(level.Possession,"possessTimer",.02f);yield return new WaitForSeconds(.1f);
            Assert.That(level.Possession.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(guard.IsHostile,Is.True);
            Assert.That(level.Spirit.transform.position.y,Is.InRange(1f,1.5f));Assert.That(target.IsHit,Is.True);
            Assert.That(GameObject.Find("inspection_Door").GetComponent<Door>().IsOpen,Is.False);
        }
        [UnityTest] public IEnumerator ExitRejectsNPCsAndUnsolvedSpiritThenSharedWinAndReplayReset()
        {
            level.Spirit.RestoreAt(level.transform.position);Physics.SyncTransforms();
            Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);
            Open("inspection");Open("exit-passage");Open("extraction");
            var guard=Object.FindFirstObjectByType<GuardEnemy>();Move(guard,level.transform.position,Vector3.forward);
            Assert.That(level.TryFinish(guard.GetComponent<Collider>()),Is.False);
            int wins=0;EventBus.LevelCompleted+=()=>wins++;Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);
            Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);Assert.That(wins,Is.EqualTo(1));
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));
            level.RestartPreview();yield return null;yield return null;Bind();
            Assert.That(level.CanExtract,Is.False);Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.InProgress));
            Assert.That(Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length,Is.EqualTo(8));
        }
        [Test] public void FormerExitIsOnlyATransferLobbyAndFourTargetDoorStaysClosedAtThree()
        {
            var view=GameObject.Find("extraction_Door").GetComponent<WarehouseDoorView>();
            Assert.That(view.Targets.Length,Is.EqualTo(4));Assert.That(view.transform.Find("Target lamp 4"),Is.Not.Null);
            Open("inspection");Open("exit-passage");Assert.That(level.CanReachTransfer,Is.True);Assert.That(level.CanExtract,Is.False);
            level.Spirit.RestoreAt(Map(70,9));Physics.SyncTransforms();
            Assert.That(level.GetComponent<Collider>().bounds.Intersects(level.Spirit.GetComponent<Collider>().bounds),Is.False);
            Assert.That(Object.FindObjectsByType<WarehouseLevel>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            for(int i=0;i<3;i++)view.Targets[i].OnHit();view.Targets[0].OnHit();
            Assert.That(view.GetComponent<Door>().IsOpen,Is.False);view.Targets[3].OnHit();Assert.That(level.ControlReady,Is.True);
            Assert.That(Reach(70,9,70,-62),Is.True);Assert.That(level.MachineReady,Is.False);
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.InProgress));
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void EitherInitialWingCanUseEitherFinalBranchWithoutSolvingTheOther(bool rightInitial,bool machineFinal)
        {
            if(rightInitial){Open("cargo-entry");Open("cargo");Wall("Loading_wall").TakeDamage(2);}
            else {Open("inspection");Open("exit-passage");}
            Assert.That(Reach(70,88,70,-62),Is.False);Assert.That(level.CanExtract,Is.False);
            if(machineFinal){Wall("Machine_divider").TakeDamage(2);Wall("Machine_exit_wall").TakeDamage(2);}
            else Open("extraction");
            Physics.SyncTransforms();Assert.That(level.CanExtract,Is.True);Assert.That(Reach(70,88,70,-62),Is.True);
            Assert.That(level.ControlReady,Is.EqualTo(!machineFinal));Assert.That(level.MachineReady,Is.EqualTo(machineFinal));
        }
        [Test] public void MachineNeedsBothWallsAndCannotBypassTheDividerThroughTheExitOpening()
        {
            Wall("Machine_exit_wall").TakeDamage(2);Physics.SyncTransforms();
            Assert.That(level.MachineReady,Is.False);Assert.That(Reach(106,-36,70,-62),Is.False);
            Wall("Machine_divider").TakeDamage(2);Physics.SyncTransforms();Assert.That(Reach(106,-36,70,-62),Is.True);
            Assert.That(level.CanExtract,Is.False,"A final solution cannot bypass the initial wing requirement.");
        }
        [UnityTest] public IEnumerator OccupiedGuardCanWinAtNewExitButAnUnoccupiedNPCStillCannot()
        {
            Open("inspection");Open("exit-passage");Open("extraction");
            var host=GameObject.Find("Control_Guard").GetComponent<GuardEnemy>();
            Assert.That(level.Spirit.GetComponent<PossessionCharge>().TryCollect(),Is.True);
            level.Spirit.RestoreAt(host.transform.position-Vector3.forward*1.8f);host.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
            Invoke(level.Possession,"Possess",host);Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            Move(host,level.transform.position,Vector3.forward);Assert.That(level.TryFinish(host.GetComponent<Collider>()),Is.True);
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));yield return null;
        }
        [UnityTest] public IEnumerator FourTargetControlDoorUsesASecondRealChargeWithoutResettingEarlierHits()
        {
            var host=GameObject.Find("Control_Guard").GetComponent<GuardEnemy>();
            var source=GameObject.Find("Charge_Source_11").GetComponent<PossessionChargePickup>();Set(source,"cooldown",.05f);
            Collect(source);Approach(host);Invoke(level.Possession,"Possess",host);
            Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            var view=GameObject.Find("extraction_Door").GetComponent<WarehouseDoorView>();
            yield return Fire(host,view.Targets[0],Map(24,-49),Vector3.left);
            yield return Fire(host,view.Targets[1],Map(44,-50),Vector3.right);
            yield return Fire(host,view.Targets[2],Map(24,-35),Vector3.left);
            Assert.That(host.GetComponent<ShootAbility>().UsesLeft,Is.Zero);
            Assert.That(view.GetComponent<Door>().IsOpen,Is.False);Assert.That(level.Possession.TryEject(false),Is.True);
            yield return new WaitForSeconds(.6f);Collect(source);Approach(host);Invoke(level.Possession,"Possess",host);
            Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            yield return Fire(host,view.Targets[3],Map(44,-34),Vector3.right);
            Assert.That(view.GetComponent<Door>().IsOpen,Is.True);
            Assert.That(level.Spirit.GetComponent<PossessionCharge>().SuccessfulPossessions,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator MachineUsesTheLocalHeavyAndFreshChargeForEachSharedSmash()
        {
            var host=GameObject.Find("Machine_Heavy").GetComponent<HeavyEnemy>();
            var source=GameObject.Find("Charge_Source_13").GetComponent<PossessionChargePickup>();Set(source,"cooldown",.05f);
            Collect(source);Approach(host);Invoke(level.Possession,"Possess",host);Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            var divider=Wall("Machine_divider");var exit=Wall("Machine_exit_wall");
            Move(host,Map(106,-39.5f),Vector3.forward);host.GetComponent<SmashAbility>().TryExecute();
            Assert.That(divider.CurrentHP,Is.LessThanOrEqualTo(0));Assert.That(exit.CurrentHP,Is.GreaterThan(0));
            Assert.That(host.GetComponent<SmashAbility>().UsesLeft,Is.Zero);Assert.That(level.Possession.TryEject(false),Is.True);
            yield return new WaitForSeconds(.6f);Collect(source);Approach(host);Invoke(level.Possession,"Possess",host);
            Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            Move(host,Map(92.5f,-47),Vector3.left);host.GetComponent<SmashAbility>().TryExecute();
            Assert.That(exit.CurrentHP,Is.LessThanOrEqualTo(0));Assert.That(level.MachineReady,Is.True);
            Assert.That(level.Spirit.GetComponent<PossessionCharge>().SuccessfulPossessions,Is.EqualTo(2));
        }
        private void Collect(PossessionChargePickup source)
        {
            level.Spirit.RestoreAt(source.transform.position+Vector3.up*1.05f);Physics.SyncTransforms();
            Assert.That(source.TryCollect(level.Spirit.GetComponent<PossessionCharge>()),Is.True,"Placed source unavailable: "+source.name);
        }
        private void Approach(EnemyBase host)
        {
            level.Spirit.RestoreAt(host.transform.position-host.transform.forward*1.8f);Physics.SyncTransforms();
            Assert.That(host.CanBePossessedFrom(level.Spirit.transform),Is.True);
        }
        private static IEnumerator Fire(GuardEnemy host,Target target,Vector3 at,Vector3 forward)
        {
            Move(host,at,forward);yield return new WaitForSeconds(.55f);
            var cam=Camera.main;var saved=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up,0).Raycast(ray,out float distance),Is.True);
            var delta=target.transform.position-ray.GetPoint(distance);delta.y=0;cam.transform.position+=delta;
            host.GetComponent<ShootAbility>().TryExecute();cam.transform.position=saved;
            float deadline=Time.time+1;while(!target.IsHit&&Time.time<deadline)yield return null;
            Assert.That(target.IsHit,Is.True,"Shared Guard shot missed "+target.name);
        }
        [Test] public void ExpandedRoomsHaveSolidOutsideEdgesIncludingNegativePlanCoordinates()
        {
            foreach(var edge in new[]{new Vector4(18,-15,1,0),new Vector4(50,-15,1,0),
                new Vector4(18,-36,1,0),new Vector4(122,-36,1,0),new Vector4(90,-36,1,0),
                new Vector4(58,-62,1,0),new Vector4(82,-62,1,0),new Vector4(70,-70,0,1)})
            {
                var at=Map(edge.x,edge.y);var offset=new Vector3(edge.z,0,-edge.w)*.7f;
                Assert.That(Physics.Linecast(at-offset,at+offset,out var hit,1<<0,QueryTriggerInteraction.Ignore),Is.True,
                    "Missing structural outer wall at "+edge);
                Assert.That(hit.collider.name,Is.EqualTo("BoundaryWall"));
            }
        }
        private static void Open(string key){foreach(var t in GameObject.Find(key+"_Door").GetComponent<WarehouseDoorView>().Targets)t.OnHit();Physics.SyncTransforms();}
        private static BreakableWall Wall(string name)=>GameObject.Find(name).GetComponent<BreakableWall>();
        private static Vector3 Map(float x,float y)=>new((x-70)*.65f,1.5f,(90-y)*.65f);
        // Rasterise actual colliders with a Heavy-sized capsule, not just the paper floor plan.
        private static bool Reach(int sx,int sy,int tx,int ty,float radius=.68f)
        {
            var visited=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();var start=new Vector2Int(sx,sy);queue.Enqueue(start);visited.Add(start);
            var solid=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>!c.isTrigger&&c.enabled&&c.GetComponentInParent<SpiritController>()==null&&(c.gameObject.layer==0||c.gameObject.layer==7)).ToArray();
            var blocked=new Dictionary<Vector2Int,bool>();
            bool Clear(Vector2Int p)
            {
                if(blocked.TryGetValue(p,out bool value))return value;
                var at=Map(p.x,p.y);at.y=Mathf.Max(at.y,radius+.5f);bool floor=Physics.Raycast(at,Vector3.down,3f,1<<0,QueryTriggerInteraction.Ignore);
                bool free=floor&&!Physics.OverlapCapsule(at+Vector3.up*.3f,at-Vector3.up*.3f,radius,(1<<0)|(1<<7),QueryTriggerInteraction.Ignore).Any(c=>solid.Contains(c));
                blocked[p]=free;return free;
            }
            var offsets=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0)
            {
                var p=queue.Dequeue();if((p-new Vector2Int(tx,ty)).sqrMagnitude<=1)return true;
                foreach(var delta in offsets){var next=p+delta;if(next.x<0||next.x>140||next.y< -75||next.y>100||visited.Contains(next)||!Clear(next))continue;visited.Add(next);queue.Enqueue(next);}
            }
            return false;
        }
        private static void Move(EnemyBase body,Vector3 at,Vector3 forward)
        {var cc=body.GetComponent<CharacterController>();cc.enabled=false;at.y=body.transform.position.y;body.transform.SetPositionAndRotation(at,Quaternion.LookRotation(forward));cc.enabled=true;Physics.SyncTransforms();}
        private static void Invoke(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    }
}

#endif
