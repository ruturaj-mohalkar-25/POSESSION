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
            Assert.That(Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).Length,Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<Target>(FindObjectsSortMode.None).Length,Is.EqualTo(12));
            Assert.That(Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length,Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).Length,Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Length,Is.EqualTo(9));
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var socket=Object.FindFirstObjectByType<WarehouseHealthSocket>();Assert.That(socket,Is.Not.Null);
            Assert.That(socket.IsGameplayBound,Is.False,"Health must wait for the team's shared healing API.");
            Assert.That(socket.SuggestedRestoreFraction,Is.EqualTo(.3f));
            Assert.That(Camera.main.GetComponent<CameraFollow>(),Is.Not.Null);
            Assert.That(GameManager.Instance.LevelTimer,Is.InRange(418f,420f));
            Assert.That(level.Possession.PossessDuration,Is.EqualTo(12f));
            Assert.That(level.Spirit.MaxHP,Is.EqualTo(5),"Use integration's shared Spirit health.");
            var charges=level.Spirit.GetComponent<PossessionCharge>();
            Assert.That(charges.Count,Is.EqualTo(1));Assert.That(charges.MaxCharges,Is.EqualTo(3));
        }
        [Test] public void RoomsAndPassagesProvideSpaceToCircleCoverAndPlan()
        {
            var room=GameObject.Find("Guard_room_Floor").GetComponent<Collider>().bounds;
            Assert.That(room.size.x,Is.EqualTo(17f).Within(.01f));
            Assert.That(room.size.z,Is.EqualTo(11f).Within(.01f));
            var sorting=GameObject.Find("Sorting_hall_Floor").GetComponent<Collider>().bounds;
            Assert.That(sorting.size.x,Is.EqualTo(13f).Within(.01f));
            Assert.That(sorting.size.z,Is.EqualTo(8f).Within(.01f));
            foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                var bounds=door.GetComponent<Collider>().bounds;
                Assert.That(Mathf.Max(bounds.size.x,bounds.size.z),Is.GreaterThanOrEqualTo(3.49f));
            }
            // Crates stay compact; enlarging a room must create usable floor space.
            Assert.That(GameObject.Find("Cover_0").GetComponent<Collider>().bounds.size.x,Is.EqualTo(2.5f).Within(.01f));
            // A large manoeuvring capsule can traverse the fully solved left route.
            Open("inspection");Open("exit-passage");Assert.That(Reach(70,88,70,9,.85f),Is.True);
        }
        [Test] public void OriginalExitAndCompactFootprintContainNoExtensionRoomsOrPuzzles()
        {
            Assert.That(level.transform.position.x,Is.EqualTo(0f).Within(.01f));
            Assert.That(level.transform.position.z,Is.EqualTo(40.5f).Within(.01f));
            Assert.That(GameObject.Find("Exit_Floor"),Is.Not.Null);
            foreach(string removed in new[]{"Transfer_lobby_Floor","Records_Floor","Control_Floor","Workshop_Floor","Machine_Floor",
                "extraction_Door","Machine_divider","Machine_exit_wall","Control_Guard","Machine_Heavy"})
                Assert.That(GameObject.Find(removed),Is.Null,removed+" belongs to the removed extension.");
            Assert.That(Object.FindObjectsByType<WarehouseLevel>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            foreach(var floor in GameObject.Find("Floors").GetComponentsInChildren<Collider>())
                Assert.That(floor.bounds.max.z,Is.LessThanOrEqualTo(44.01f),"No extra floor may extend past the original north boundary.");
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
            Open("inspection");Open("exit-passage");
            var guard=Object.FindFirstObjectByType<GuardEnemy>();Move(guard,level.transform.position,Vector3.forward);
            Assert.That(level.TryFinish(guard.GetComponent<Collider>()),Is.False);
            int wins=0;EventBus.LevelCompleted+=()=>wins++;Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);
            Assert.That(level.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);Assert.That(wins,Is.EqualTo(1));
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));
            level.RestartPreview();yield return null;yield return null;Bind();
            Assert.That(level.CanExtract,Is.False);Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.InProgress));
            Assert.That(Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length,Is.EqualTo(6));
        }
        [TestCase(false)] [TestCase(true)]
        public void EitherOriginalWingUnlocksTheOriginalFinishWithoutAnyFinalBranch(bool right)
        {
            Assert.That(level.CanExtract,Is.False);
            if(right){Open("cargo-entry");Open("cargo");Wall("Loading_wall").TakeDamage(2);}
            else{Open("inspection");Open("exit-passage");}
            Physics.SyncTransforms();Assert.That(level.CanExtract,Is.True);
            Assert.That(level.LeftReady,Is.EqualTo(!right));Assert.That(level.RightReady,Is.EqualTo(right));
            Assert.That(Reach(70,88,70,9,.85f),Is.True,"The larger Heavy must fit through the original route.");
        }
        [UnityTest] public IEnumerator OccupiedGuardCanWinAtOriginalExitButAnUnoccupiedNPCStillCannot()
        {
            Open("inspection");Open("exit-passage");
            var host=GameObject.Find("Sorting_hall_Guard").GetComponent<GuardEnemy>();
            Assert.That(level.Spirit.GetComponent<PossessionCharge>().TryCollect(),Is.True);
            level.Spirit.RestoreAt(host.transform.position-Vector3.forward*1.8f);host.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
            Invoke(level.Possession,"Possess",host);Assert.That(level.Possession.PossessedEnemy,Is.SameAs(host));
            Move(host,level.transform.position,Vector3.forward);Assert.That(level.TryFinish(host.GetComponent<Collider>()),Is.True);
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));yield return null;
        }
        private static void Open(string key){foreach(var t in GameObject.Find(key+"_Door").GetComponent<WarehouseDoorView>().Targets)t.OnHit();Physics.SyncTransforms();}
        private static BreakableWall Wall(string name)=>GameObject.Find(name).GetComponent<BreakableWall>();
        private static Vector3 Map(float x,float y)=>new((x-70)*.5f,1.5f,(90-y)*.5f);
        // Rasterise actual colliders with a Heavy-sized capsule, not just the paper floor plan.
        private static bool Reach(int sx,int sy,int tx,int ty,float radius=.85f)
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
