using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class Level03CompoundTests
    {
        private Level03Director level;
        private PossessionController pc;
        private PossessionCharge charge;
        const float Upper=Level03Director.FloorHeight;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=3f;EventBus.Clear();yield return SceneManager.LoadSceneAsync("Level_03");yield return null;yield return null;
            Bind();Freeze();yield return new WaitForSeconds(.1f);
        }
        private void Bind(){level=Object.FindFirstObjectByType<Level03Director>();pc=level.Possession;charge=level.Charge;}
        private void Freeze()
        {
            foreach(var enemy in level.Hosts){enemy.enabled=false;enemy.GetComponent<EnemyContactDamage>().enabled=false;}
            level.West.enabled=level.East.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Level03_Test_Cleanup");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [Test] public void LayoutContainsSpaciousFloorsNineSharedHostsAndTenSources()
        {
            Assert.That(level.Hosts.Length,Is.EqualTo(9));Assert.That(level.Hosts.OfType<GuardEnemy>().Count(),Is.EqualTo(5));Assert.That(level.Hosts.OfType<HeavyEnemy>().Count(),Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Length,Is.EqualTo(10));
            Assert.That(Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).Count(p=>p.Reforms),Is.EqualTo(8));
            Assert.That(charge.Count,Is.Zero);Assert.That(pc.PossessDuration,Is.EqualTo(12));
            foreach(string zone in new[]{"G1","G2","G3","G4","G5","G6","U1","U2","U3","U4","U5","U6","D1","D2","D3","D4"})Assert.That(GameObject.Find(zone),Is.Not.Null,zone);
            foreach(var enemy in level.Hosts)
            {Assert.That(enemy.GetComponent<CharacterController>(),Is.Not.Null);Assert.That(enemy.CurrentHP,Is.EqualTo(enemy is HeavyEnemy?5:3));Assert.That(enemy.GetComponents<EnemyContactDamage>().Length,Is.EqualTo(1));Assert.That(enemy.GetComponents<Level03AlarmLink>().Length,Is.LessThanOrEqualTo(1));Assert.That(enemy.GetComponent<VisionConeView>(),Is.Not.Null);Assert.That(enemy.VisionAngle,Is.EqualTo(45f));}
        }
        [UnityTest] public IEnumerator FloorsBlockPossessionAndPickupsAndOverheadRenderingIsOnlyVisual()
        {
            Vector3 lower=Map(8,7);level.Spirit.RestoreAt(lower);var host=Host("Upper_Guard_0");Move(host,Map(8,7,Upper));
            Assert.That(Physics.Linecast(lower,host.transform.position,out var hit,~0,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(hit.collider.name,Is.EqualTo("Floor"));
            Assert.That(charge.TryCollect(),Is.True);Set(pc,"possessRange",10f);Invoke(pc,"Possess",host);Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.HasCharge,Is.True);
            charge.Clear();var source=GameObject.Find("K4").GetComponent<PossessionChargePickup>();level.Spirit.RestoreAt(source.transform.position+Vector3.down*(Upper-1.05f));Assert.That(source.TryCollect(charge),Is.False);
            yield return null;yield return null;
            var upperFloor=GameObject.Find("UpperFloor").GetComponentsInChildren<Renderer>().First(r=>r.name=="Floor");Assert.That(upperFloor.forceRenderingOff,Is.True);Assert.That(upperFloor.GetComponent<Collider>().enabled,Is.True);
            level.Spirit.RestoreAt(Map(8,7,Upper));yield return null;yield return null;Assert.That(upperFloor.forceRenderingOff,Is.False);
        }
        [UnityTest] public IEnumerator GuardAndHeavyUseEveryPhysicalStairInBothDirections()
        {
            OpenAll();
            foreach(string bodyName in new[]{"Ground_Guard_3","Ground_Heavy_7"})
            {
                var host=Host(bodyName);yield return Possess("K1",host);pc.enabled=false;host.enabled=true;
                foreach(var stair in new[]{new Vector2(18.5f,9),new Vector2(31.5f,25),new Vector2(31.5f,9)})
                {
                    Move(host,Map(stair.x,stair.y+.4f,0,host is HeavyEnemy?1.45f:1.15f));
                    yield return Walk(host,Map(stair.x,stair.y-6.2f));
                    Assert.That(host.transform.position.y,Is.GreaterThan(Upper+.85f),host.name+" did not reach the upper landing");
                    yield return Walk(host,Map(stair.x,stair.y+.4f));
                    Assert.That(host.transform.position.y,Is.LessThan(2f),host.name+" did not descend");
                }
                host.enabled=false;Assert.That(pc.TryEject(false),Is.True);Move(host,GameObject.Find("SpawnPoint_"+host.name).transform.position);pc.enabled=true;yield return new WaitForSeconds(.6f);
            }
        }
        [UnityTest] public IEnumerator SpiritUsesEveryPhysicalStairInBothDirections()
        {
            OpenAll();
            foreach(var stair in new[]{new Vector2(18.5f,9),new Vector2(31.5f,25),new Vector2(31.5f,9)})
            {
                level.Spirit.RestoreAt(Map(stair.x,stair.y+.4f));yield return SpiritWalk(Map(stair.x,stair.y-6.2f));
                Assert.That(level.Spirit.transform.position.y,Is.GreaterThan(Upper+.8f));
                yield return SpiritWalk(Map(stair.x,stair.y+.4f));Assert.That(level.Spirit.transform.position.y,Is.LessThan(2f));
            }
        }
        [UnityTest] public IEnumerator MainStrategyUsesGuardWorkshopHeavyAndRecordsGuard()
        {
            yield return Possess("K1",Host("Ground_Guard_3"));yield return FireAt("T1",Map(5,7));Assert.That(Gate("Gate_A").IsOpen,Is.True);Eject();
            yield return Possess("K7",Host("Upper_Heavy_5"));Smash("PW",Map(7,23.6f,Upper),Vector3.back);level.West.enabled=true;yield return null;Assert.That(level.West.Disabled,Is.True);Assert.That(Gate("West_Bridge").IsOpen,Is.True);Eject();
            yield return Possess("K5",Host("Upper_Guard_6"));yield return FireAt("T3",Map(18,20,Upper));Assert.That(Gate("Extraction_South").IsOpen,Is.True);Finish();
            Assert.That(Target("R").IsHit,Is.False);Assert.That(Wall("B1").CurrentHP,Is.GreaterThan(0));Assert.That(Wall("PE").CurrentHP,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator ServiceStrategyUsesOnlyB1AndExtractionTarget()
        {
            yield return Possess("K3",Host("Ground_Heavy_7"));Smash("B1",Map(31.5f,25.7f),Vector3.forward);Eject();
            yield return Possess("K5",Host("Upper_Guard_6"));yield return FireAt("T3",Map(18,20,Upper));Assert.That(Gate("Extraction_West").IsOpen,Is.True);Finish();
            Assert.That(Target("T1").IsHit,Is.False);Assert.That(Wall("PW").CurrentHP,Is.GreaterThan(0));Assert.That(Wall("PE").CurrentHP,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator ShutdownStrategyKeepsK6AccessibleBeforePanelGate()
        {
            yield return Possess("K3",Host("Ground_Heavy_7"));Smash("B1",Map(31.5f,25.7f),Vector3.forward);Eject();
            yield return Possess("K5",Host("Upper_Guard_6"));yield return FireAt("SE",Map(20,19.5f,Upper));Assert.That(Gate("PE_Service").IsOpen,Is.True);Eject();
            yield return Possess("K6",Host("Upper_Heavy_7"));Smash("PE",Map(28,23.6f,Upper),Vector3.back);level.East.enabled=true;yield return null;
            Assert.That(level.East.Disabled,Is.True);Assert.That(Gate("Extraction_South").IsOpen,Is.True);Finish();Assert.That(Target("T3").IsHit,Is.False);
        }
        [UnityTest] public IEnumerator MaintenanceStrategyReleasesBothIndependentLowerBarriers()
        {
            yield return Possess("K1",Host("Ground_Guard_3"));yield return FireAt("T1",Map(5,7));Eject();
            yield return Possess("K4",Host("Upper_Guard_0"));yield return FireAt("R",Map(3,5,Upper));Eject();Assert.That(Gate("Gate_C").IsOpen,Is.True);
            yield return Possess("K8",Host("Ground_Heavy_5"));Smash("B3",Map(31.5f,10.3f),Vector3.forward);Finish();
            Assert.That(Target("T3").IsHit,Is.False);Assert.That(Gate("Extraction_West").IsOpen,Is.False);Assert.That(Gate("Extraction_South").IsOpen,Is.False);
        }
        [UnityTest] public IEnumerator HeavyCanWalkToExtractionFromTheUpperStairLanding()
        {
            OpenAll();var heavy=Host("Upper_Heavy_7");yield return Possess("K6",heavy);pc.enabled=false;heavy.enabled=true;
            Move(heavy,Map(31.5f,2.5f,Upper));yield return Walk(heavy,Map(34.7f,2.5f,Upper));
            yield return Walk(heavy,Map(34.7f,5.5f,Upper));yield return Walk(heavy,Object.FindFirstObjectByType<Level03Exit>().transform.position);
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));
        }
        [UnityTest] public IEnumerator LinkedWitnessWarnsThenLatchesAndJunctionDoesNotClearExistingAlarm()
        {
            var watcher=Host("Upper_Guard_1");var suspect=Host("Upper_Heavy_5");yield return Possess("K7",suspect);
            Move(watcher,Map(17,22,Upper));Move(suspect,Map(18.3f,22,Upper));watcher.transform.forward=Vector3.right;Physics.SyncTransforms();
            watcher.transform.forward=Vector3.left;Assert.That(level.West.CanObserve(watcher,suspect.transform),Is.False,"Alarm must respect the displayed cone.");watcher.transform.forward=Vector3.right;Physics.SyncTransforms();
            level.West.enabled=true;Assert.That(level.West.CanObserve(watcher,suspect.transform),Is.True,"Sight blocked: "+SightBlocker(watcher,suspect));yield return new WaitForSeconds(1f);Assert.That(level.West.Latched,Is.False);Assert.That(level.West.Suspicion,Is.GreaterThan(0));
            yield return new WaitForSeconds(2.25f);Assert.That(level.West.Latched,Is.True);Target("JW").OnHit();yield return null;Assert.That(level.West.Connected,Is.False);Assert.That(level.West.Latched,Is.True);
            Wall("PW").TakeDamage(2);yield return null;yield return null;Assert.That(level.West.Latched,Is.False);Assert.That(level.West.Disabled,Is.True);Assert.That(Gate("Gate_A").IsOpen,Is.True);
        }
        [UnityTest] public IEnumerator BothAlarmsPreserveRecoverySourcesAndPermanentRelayAndServiceLatches()
        {
            Target("R").OnHit();Target("SE").OnHit();Wall("B1").TakeDamage(2);level.West.RestoreAlarm(true);level.East.RestoreAlarm(true);yield return null;
            Assert.That(Gate("Gate_A").IsOpen,Is.False);Assert.That(Gate("Extraction_South").IsOpen,Is.False);Assert.That(Gate("Gate_C").IsOpen,Is.True);Assert.That(Gate("PE_Service").IsOpen,Is.True);
            Collect("K6");Assert.That(charge.HasCharge,Is.True);Assert.That(Wall("B1").CurrentHP,Is.LessThanOrEqualTo(0));
        }
        [UnityTest] public IEnumerator CheckpointRestoresProgressCircuitAndSourcesWithoutFreeChargeOrMissingHosts()
        {
            Target("T1").OnHit();Target("R").OnHit();Target("SE").OnHit();Wall("B1").TakeDamage(2);level.West.RestoreAlarm(true);Collect("K9");
            var point=Object.FindObjectsByType<Level03Checkpoint>(FindObjectsSortMode.None).First(p=>p.label=="Main stair shelter");level.Spirit.RestoreAt(point.transform.position+Vector3.up*1.15f);
            Assert.That(level.SaveCheckpoint(point),Is.True);Host("Ground_Heavy_7").TakeDamage(999);level.RetryCheckpoint();yield return null;yield return null;yield return new WaitForSeconds(.2f);Bind();Freeze();
            Assert.That(charge.Count,Is.Zero);Assert.That(Target("R").IsHit,Is.True);Assert.That(Target("SE").IsHit,Is.True);Assert.That(Wall("B1").CurrentHP,Is.LessThanOrEqualTo(0));Assert.That(level.West.Latched,Is.True);Assert.That(Gate("Gate_C").IsOpen,Is.True);Assert.That(GameObject.Find("K9").GetComponent<PossessionChargePickup>().IsReady,Is.False);Assert.That(GameObject.Find("K2").GetComponent<PossessionChargePickup>().IsReady,Is.True);Assert.That(level.Hosts.All(h=>h.CurrentHP>0&&h.gameObject.activeInHierarchy),Is.True);
            level.FullRestart();yield return null;yield return null;yield return new WaitForSeconds(.2f);Bind();Freeze();Assert.That(level.HasCheckpoint,Is.False);Assert.That(Target("T1").IsHit,Is.False);Assert.That(level.West.Latched,Is.False);Assert.That(charge.Count,Is.Zero);
        }
        [Test] public void ExitRejectsNpcAndRemotePlayer()
        {
            var exit=Object.FindFirstObjectByType<Level03Exit>();Assert.That(exit.TryFinish(Host("Upper_Guard_1").GetComponent<Collider>()),Is.False);Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);
        }
        [Test] public void StairUndersidesAreSealedAgainstSideAndRearEntry()
        {
            foreach(string name in new[]{"A","B","C"})
            {
                var stair=GameObject.Find("Stair_"+name).transform;
                var bounds=stair.GetComponent<MeshCollider>().sharedMesh.bounds;
                Vector3 origin=stair.position+new Vector3(-bounds.extents.x-1f,1.05f,0);
                Assert.That(Physics.SphereCast(origin,.4f,Vector3.right,out var side,2f,1,QueryTriggerInteraction.Ignore),Is.True,name+" allows entry from below its side");
                Assert.That(side.collider.name,Is.EqualTo("Stair Wall"));
                origin=stair.position+new Vector3(0,1.05f,bounds.extents.z+1.2f);
                Assert.That(Physics.SphereCast(origin,.4f,Vector3.back,out var rear,2f,1,QueryTriggerInteraction.Ignore),Is.True,name+" allows entry from below its rear");
                Assert.That(rear.collider.name,Is.EqualTo("Stair backing"));
            }
        }
        [UnityTest] public IEnumerator TargetsOnlyAnnounceDoorsWhoseSecurityConditionsAreSatisfied()
        {
            var opened=new System.Collections.Generic.List<string>();System.Action<Door> listener=d=>opened.Add(d.name);EventBus.DoorOpened+=listener;
            Target("JW").OnHit();yield return null;yield return null;Assert.That(opened,Is.Empty,"A junction must not announce unrelated gates as open.");
            Target("T1").OnHit();yield return null;yield return null;Assert.That(opened,Is.EquivalentTo(new[]{"Gate_A"}));
            EventBus.DoorOpened-=listener;
        }
        [UnityTest] public IEnumerator StairAndUpperEjectionAndHostDefeatRestoreAnEmptyGroundedSpirit()
        {
            OpenAll();var guard=Host("Ground_Guard_3");yield return Possess("K1",guard);
            var cc=guard.GetComponent<CharacterController>();cc.enabled=false;guard.transform.position=Map(18.5f,6,3.6f);cc.enabled=true;Physics.SyncTransforms();
            Assert.That(pc.TryEject(true),Is.True);Assert.That(charge.Count,Is.Zero);Assert.That(level.Spirit.transform.position.y,Is.InRange(3.5f,6f));
            Assert.That(Physics.Raycast(level.Spirit.transform.position,Vector3.down,2f,1,QueryTriggerInteraction.Ignore),Is.True);
            yield return Possess("K4",Host("Upper_Guard_0"));Set(pc,"possessTimer",.01f);yield return new WaitForSeconds(.2f);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.Count,Is.Zero);Assert.That(level.Spirit.transform.position.y,Is.GreaterThan(Upper+.8f));
            var heavy=Host("Upper_Heavy_7");yield return Possess("K6",heavy);heavy.TakeDamage(999);yield return new WaitForSeconds(.2f);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.Count,Is.Zero);Assert.That(level.Spirit.transform.position.y,Is.GreaterThan(Upper+.8f));Assert.That(level.Spirit.GetComponent<Collider>().enabled,Is.True);
        }
        [UnityTest] public IEnumerator PausedMapFreezesTimeHealthAndInputThenRestoresThePlayer()
        {
            var map=level.GetComponent<Level03RouteMap>();Assert.That(map,Is.Not.Null);Assert.That(charge.TryCollect(),Is.True);
            float clock=GameManager.Instance.LevelTimer;int hp=level.Spirit.CurrentHP;float scale=Time.timeScale;
            Assert.That(map.Open(),Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(pc.enabled,Is.False);Assert.That(level.Spirit.enabled,Is.False);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(GameManager.Instance.LevelTimer,Is.EqualTo(clock).Within(.01f));Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));Assert.That(charge.Count,Is.EqualTo(1));
            map.Close();Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(pc.enabled,Is.True);Assert.That(level.Spirit.enabled,Is.True);Assert.That(charge.Count,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator GuardsNeedVisibleNoticeTimeAndChaseSlowerThanThePlayerBody()
        {
            var guard=Host("Ground_Guard_3");Move(guard,Map(18,22));level.Spirit.RestoreAt(Map(18,23.3f));
            guard.transform.rotation=Quaternion.LookRotation(Vector3.back);Physics.SyncTransforms();
            typeof(EnemyBase).GetField("waypointWaitTimer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(guard,8f);guard.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSeconds(1f);Assert.That(guard.IsChasing,Is.False,"A Guard must not chase immediately.");
            var screen=GameObject.CreatePrimitive(PrimitiveType.Cube);screen.name="Temporary sight blocker";screen.transform.position=(guard.transform.position+level.Spirit.transform.position)*.5f;screen.transform.localScale=new Vector3(2,3,.3f);Physics.SyncTransforms();
            yield return new WaitForSeconds(1.4f);Assert.That(guard.IsChasing,Is.False,"Walls must reset the notice period.");Object.Destroy(screen);yield return null;
            yield return new WaitForSeconds(1f);Assert.That(guard.IsChasing,Is.False);
            yield return new WaitForSeconds(1.2f);Assert.That(guard.IsChasing,Is.True);
            Vector3 start=guard.transform.position;yield return new WaitForSeconds(.4f);var delta=guard.transform.position-start;delta.y=0;
            Assert.That(delta.magnitude,Is.InRange(.15f,.8f),"Pursuit should be slower than the controllable Guard.");
            Assert.That(charge.TryCollect(),Is.True);Approach(guard);Invoke(pc,"Possess",guard);Assert.That(pc.PossessedEnemy,Is.SameAs(guard));Assert.That(guard.MoveSpeed,Is.EqualTo(3.25f));
        }
        [Test] public void FrontFacingPossessionKeepsTheChargeAndBehindTheConeSpendsIt()
        {
            var guard=Host("Ground_Guard_3");Move(guard,Map(18,22));level.Spirit.RestoreAt(guard.transform.position+Vector3.right*2f);
            guard.transform.forward=Vector3.right;Physics.SyncTransforms();Assert.That(charge.TryCollect(),Is.True);
            Assert.That(guard.CanSee(level.Spirit.transform),Is.True);Invoke(pc,"Possess",guard);
            Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(charge.HasCharge,Is.True);
            guard.transform.forward=Vector3.left;Physics.SyncTransforms();Invoke(pc,"Possess",guard);
            Assert.That(pc.PossessedEnemy,Is.SameAs(guard));Assert.That(charge.HasCharge,Is.False);
        }
        [UnityTest] public IEnumerator GuardsShootOnSightOnBothFloorsAfterTheWarning()
        {
            var spiritBody=level.Spirit.GetComponent<Rigidbody>();spiritBody.useGravity=false;spiritBody.constraints=RigidbodyConstraints.FreezeAll;
            foreach(var encounter in new[]{("Ground_Guard_3",0f),("Upper_Guard_6",Upper)})
            {
                var guard=Host(encounter.Item1);Move(guard,Map(17,22,encounter.Item2));
                level.Spirit.RestoreAt(guard.transform.position+Vector3.right*2.7f);guard.transform.forward=Vector3.right;
                SetBase(guard,"waypointWaitTimer",10f);Set(level.Spirit,"nextDamageTime",Time.time);Physics.SyncTransforms();
                int hp=level.Spirit.CurrentHP;var attack=guard.GetComponent<EnemyContactDamage>();guard.enabled=attack.enabled=true;
                yield return new WaitForSeconds(1f);Assert.That(guard.IsChasing,Is.False);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));
                yield return new WaitForSeconds(1.3f);Assert.That(guard.IsChasing,Is.True);Assert.That(guard.IsHostile,Is.False);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));
                float until=Time.time+2.5f;
                while(level.Spirit.CurrentHP==hp&&Time.time<until)yield return null;
                Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp-1),encounter.Item1+" did not land a real projectile after spotting the Spirit.");
                attack.enabled=guard.enabled=false;
            }
        }
        [UnityTest] public IEnumerator HeavyCancelsItsTelegraphedSmashWhenSightIsBlocked()
        {
            var heavy=Host("Ground_Heavy_7");Move(heavy,Map(17,22));level.Spirit.RestoreAt(heavy.transform.position+Vector3.right*2.4f);
            var spiritBody=level.Spirit.GetComponent<Rigidbody>();spiritBody.useGravity=false;spiritBody.constraints=RigidbodyConstraints.FreezeAll;
            heavy.transform.forward=Vector3.right;SetBase(heavy,"waypointWaitTimer",10f);Set(level.Spirit,"nextDamageTime",Time.time);Physics.SyncTransforms();
            Assert.That(heavy.CanSee(level.Spirit.transform),Is.True,"Heavy encounter must start with a clear view.");
            var attack=heavy.GetComponent<EnemyContactDamage>();heavy.enabled=attack.enabled=true;int hp=level.Spirit.CurrentHP;
            float warningDeadline=Time.time+6f;
            while(Read<float>(attack,"smashAt")<=0f&&Time.time<warningDeadline)yield return null;
            Assert.That(heavy.IsChasing,Is.True);Assert.That(Read<float>(attack,"smashAt"),Is.GreaterThan(Time.time));Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=(heavy.transform.position+level.Spirit.transform.position)*.5f;wall.transform.localScale=new Vector3(.3f,3f,2f);Physics.SyncTransforms();
            yield return null;yield return null;Assert.That(Read<float>(attack,"smashAt"),Is.Zero);
            yield return new WaitForSeconds(2.3f);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator SearchReacquiresImmediatelyAndReturnsAfterLosingSight()
        {
            var guard=Host("Ground_Guard_3");Move(guard,Map(17,22));level.Spirit.RestoreAt(guard.transform.position+Vector3.right*2.7f);
            guard.transform.forward=Vector3.right;SetBase(guard,"waypointWaitTimer",10f);guard.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSeconds(2.3f);Assert.That(guard.IsChasing,Is.True);
            level.Spirit.RestoreAt(Map(4,24));yield return null;yield return null;Assert.That(guard.IsSearching,Is.True);
            level.Spirit.RestoreAt(guard.transform.position+Vector3.right*2.5f);Physics.SyncTransforms();yield return null;yield return null;
            Assert.That(guard.IsChasing,Is.True,"Search reacquisition must not wait another two seconds.");Assert.That(guard.IsSearching,Is.False);
            level.Spirit.RestoreAt(Map(4,24));yield return new WaitForSeconds(1.8f);Assert.That(guard.IsSearching,Is.False);Assert.That(guard.IsChasing,Is.False);
        }
        [UnityTest] public IEnumerator SharedAttacksCannotReachTheSpiritThroughTheOtherFloor()
        {
            level.Spirit.RestoreAt(Map(8,7));Set(level.Spirit,"nextDamageTime",Time.time);int hp=level.Spirit.CurrentHP;
            foreach(string name in new[]{"Upper_Heavy_7","Upper_Guard_6"})
            {
                var host=Host(name);Move(host,Map(8,7,Upper));var attack=host.GetComponent<EnemyContactDamage>();Set(attack,"firstAttackDelay",0f);Set(attack,"smashWindup",.1f);
                host.OnForcedEject(5f);attack.enabled=host.enabled=true;EventBus.Ejected?.Invoke(host,true);
                yield return new WaitForSeconds(.5f);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));Assert.That(Read<float>(attack,"smashAt"),Is.Zero);
                Assert.That(Object.FindObjectsByType<EnemyShot>(FindObjectsSortMode.None).Length,Is.Zero);
                host.enabled=attack.enabled=false;
            }
        }
        [UnityTest] public IEnumerator SharedDamageUsesSpawnGraceAndOneRecoveryWindowForAllEnemies()
        {
            int hp=level.Spirit.CurrentHP;level.Spirit.TakeDamage(1);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp));
            Set(level.Spirit,"nextDamageTime",Time.time);level.Spirit.TakeDamage(1);level.Spirit.TakeDamage(1);
            Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp-1),"Simultaneous attacks must not stack damage.");
            yield return new WaitForSeconds(5.1f);level.Spirit.TakeDamage(1);Assert.That(level.Spirit.CurrentHP,Is.EqualTo(hp-2));
        }
        [UnityTest] public IEnumerator HeavyBreachesRealWorkshopAndPowerBayPassages()
        {
            Assert.That(Physics.SphereCast(Map(34.5f,25.5f,Upper),.55f,Vector3.back,out _,3f,1,QueryTriggerInteraction.Ignore),Is.True,"The power bay must not have an open bypass around the intact wall.");
            var heavy=Host("Upper_Heavy_5");yield return Possess("K7",heavy);pc.enabled=false;
            foreach(var passage in new[]{("PW",7f),("PE",28f)})
            {
                if(passage.Item1=="PE"){Target("SE").OnHit();yield return null;level.ApplyGates();}
                Vector3 before=Map(passage.Item2,23.1f,Upper),after=Map(passage.Item2,25.1f,Upper);
                Move(heavy,before);var body=heavy.GetComponent<CharacterController>();
                for(int i=0;i<20;i++){body.Move(Vector3.back*.12f);yield return null;}
                Assert.That(heavy.transform.position.z,Is.GreaterThan(Wall(passage.Item1).transform.position.z+.4f),"Intact wall must block the passage.");
                Move(heavy,before);heavy.GetComponent<SmashAbility>().ResetUses();Smash(passage.Item1,before,Vector3.back);
                yield return Walk(heavy,after);
                if(passage.Item1=="PW"){yield return Walk(heavy,Map(11,25.1f,Upper));yield return Walk(heavy,Map(14,25.1f,Upper));}
                else {yield return Walk(heavy,Map(28,27,Upper));yield return Walk(heavy,Map(38,27,Upper));yield return Walk(heavy,Map(38,28.5f,Upper));}
            }
            Assert.That(GameObject.Find("POWER_BAY"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator ExtractionBayChangesFromLockedToReadyAndFinishesOnlyThePlayer()
        {
            var exit=Object.FindFirstObjectByType<Level03Exit>();var beacon=exit.GetComponent<Level03ExtractionBeacon>();Assert.That(beacon,Is.Not.Null);
            Assert.That(GameObject.Find("EXIT_BAY"),Is.Not.Null);Assert.That(GameObject.Find("Gateway arch"),Is.Not.Null);
            level.Spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();Assert.That(exit.IsReady,Is.False);
            Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>()),Is.False);yield return null;
            Assert.That(GameObject.Find("Extraction status").GetComponent<TextMesh>().text,Does.Contain("RESTORE"));
            Target("T3").OnHit();level.ApplyGates();yield return null;
            Assert.That(exit.IsReady,Is.True);Assert.That(GameObject.Find("Extraction status").GetComponent<TextMesh>().text,Is.EqualTo("EXTRACTION READY"));
            Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>())||GameManager.Instance.CurrentState==LevelState.Complete,Is.True);
            Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.Complete));
        }
        [UnityTest] public IEnumerator Level3ContainsTheCurrentTeamPlayerEnemyAndHudComponents()
        {
            yield return SceneManager.LoadSceneAsync("Level_01",LoadSceneMode.Additive);
            var reference=SceneManager.GetSceneByName("Level_01");var roots=reference.GetRootGameObjects();
            var samples=new MonoBehaviour[]{roots.SelectMany(r=>r.GetComponentsInChildren<SpiritController>(true)).First(),roots.SelectMany(r=>r.GetComponentsInChildren<GuardEnemy>(true)).First(),roots.SelectMany(r=>r.GetComponentsInChildren<HeavyEnemy>(true)).First(),roots.SelectMany(r=>r.GetComponentsInChildren<HUDManager>(true)).First()};
            var actual=new MonoBehaviour[]{level.Spirit,level.Hosts.OfType<GuardEnemy>().First(),level.Hosts.OfType<HeavyEnemy>().First(),level.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HUDManager>(true)).First()};
            for(int i=0;i<samples.Length;i++)foreach(var component in samples[i].GetComponents<MonoBehaviour>())
                if(component!=null)Assert.That(actual[i].GetComponent(component.GetType()),Is.Not.Null,"Missing team component "+component.GetType().Name);
            var teamCamera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();var localCamera=level.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();
            Assert.That(localCamera.fieldOfView,Is.EqualTo(teamCamera.fieldOfView));Assert.That(Quaternion.Angle(localCamera.transform.rotation,teamCamera.transform.rotation),Is.LessThan(.1f));
            Assert.That(Read<Vector3>(localCamera.GetComponent<CameraFollow>(),"offset"),Is.EqualTo(Read<Vector3>(teamCamera.GetComponent<CameraFollow>(),"offset")));
            yield return SceneManager.UnloadSceneAsync(reference);
        }
        private static T Read<T>(object obj,string field)
        {for(var t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(field,BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null)return (T)f.GetValue(obj);}throw new System.MissingFieldException(field);}
        private static void SetBase(object obj,string field,object value)=>typeof(EnemyBase).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        private static string SightBlocker(EnemyBase watcher,EnemyBase suspect)
        {
            Physics.Linecast(watcher.transform.position+Vector3.up*.25f,suspect.transform.position,out var hit,~0,QueryTriggerInteraction.Ignore);
            return (hit.collider!=null?hit.collider.name:"none")+" watcher="+watcher.transform.position+" suspect="+suspect.transform.position;
        }
        private IEnumerator Possess(string pickup,EnemyBase host)
        {
            if(pc.CurrentState!=PlayerState.Spirit)Eject();charge.Clear();Collect(pickup);yield return new WaitForSeconds(.6f);Approach(host);Invoke(pc,"Possess",host);Assert.That(pc.PossessedEnemy,Is.SameAs(host),"Could not possess "+host.name);Assert.That(charge.Count,Is.Zero);
        }
        private void Collect(string name){var p=GameObject.Find(name).GetComponent<PossessionChargePickup>();level.Spirit.RestoreAt(p.transform.position+Vector3.up*1.05f);Physics.SyncTransforms();Assert.That(p.TryCollect(charge),Is.True,name);}
        private void Approach(EnemyBase host)
        {
            foreach(var direction in new[]{-host.transform.forward,Vector3.right,Vector3.back,Vector3.left,Vector3.forward})
            {var p=host.transform.position+direction*1.8f;if(!Physics.CheckCapsule(p+Vector3.up*.4f,p-Vector3.up*.4f,.5f,1,QueryTriggerInteraction.Ignore)){level.Spirit.RestoreAt(p);Physics.SyncTransforms();if(host.CanBePossessedFrom(level.Spirit.transform))return;}}
            Assert.Fail("No clear approach to "+host.name);
        }
        private IEnumerator FireAt(string name,Vector3 position)
        {
            var body=pc.PossessedEnemy;Move(body,position);var target=Target(name);var cam=Camera.main;var old=cam.transform.position;var ray=cam.ScreenPointToRay(Input.mousePosition);Assert.That(new Plane(Vector3.up,Level03AimPlane.Height(body.transform)).Raycast(ray,out float d),Is.True);var offset=target.transform.position-ray.GetPoint(d);offset.y=0;cam.transform.position+=offset;body.GetComponent<ShootAbility>().TryExecute();cam.transform.position=old;
            float deadline=Time.time+2;while(!target.IsHit&&Time.time<deadline)yield return null;Assert.That(target.IsHit,Is.True,"Shared shot missed "+name);yield return null;
        }
        private void Smash(string name,Vector3 position,Vector3 direction){var body=pc.PossessedEnemy;Move(body,position);body.transform.forward=direction;body.GetComponent<SmashAbility>().TryExecute();Assert.That(Wall(name).CurrentHP,Is.LessThanOrEqualTo(0),"Shared smash missed "+name);}
        private void Eject(){Assert.That(pc.TryEject(false),Is.True);}
        private void Finish(){var exit=Object.FindFirstObjectByType<Level03Exit>();if(pc.PossessedEnemy!=null){Move(pc.PossessedEnemy,exit.transform.position);Assert.That(exit.TryFinish(pc.PossessedEnemy.GetComponent<Collider>()),Is.True);}else{level.Spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();Assert.That(exit.TryFinish(level.Spirit.GetComponent<Collider>()),Is.True);}}
        private void OpenAll(){foreach(var t in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))t.OnHit();foreach(var w in Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None))w.TakeDamage(2);level.West.RestoreAlarm(false);level.East.RestoreAlarm(false);level.ApplyGates();}
        private IEnumerator Walk(EnemyBase host,Vector3 target)
        {
            float deadline=Time.time+18;
            while(true){if(GameManager.Instance.CurrentState==LevelState.Complete)break;Vector3 d=target-host.transform.position;d.y=0;if(d.magnitude<.12f)break;Assert.That(Time.time,Is.LessThan(deadline),"Stair obstructed for "+host.name+" at "+host.transform.position);host.GetComponent<CharacterController>().Move(d.normalized*Mathf.Min(d.magnitude,host.MoveSpeed*Time.deltaTime));yield return null;}yield return new WaitForSecondsRealtime(.05f);
        }
        private IEnumerator SpiritWalk(Vector3 target)
        {
            var spirit=level.Spirit;spirit.enabled=false;var rb=spirit.GetComponent<Rigidbody>();float deadline=Time.time+8;
            while(true){Vector3 d=target-spirit.transform.position;d.y=0;if(d.magnitude<.15f)break;Assert.That(Time.time,Is.LessThan(deadline),"Spirit stair obstructed at "+spirit.transform.position);rb.linearVelocity=new Vector3(d.normalized.x*6,rb.linearVelocity.y,d.normalized.z*6);yield return new WaitForFixedUpdate();}
            rb.linearVelocity=Vector3.zero;spirit.enabled=true;yield return new WaitForSeconds(.2f);
        }
        private static void Move(EnemyBase h,Vector3 position){var cc=h.GetComponent<CharacterController>();cc.enabled=false;position.y=position.y>=6?Upper+(h is HeavyEnemy?1.45f:1.15f):h is HeavyEnemy?1.45f:1.15f;h.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
        private static Vector3 Map(float x,float y,float floor=0,float lift=1.15f)=>new((x-18)*Level03Director.LayoutUnit,floor+lift,(14-y)*Level03Director.LayoutUnit);
        private static EnemyBase Host(string name)=>GameObject.Find(name).GetComponent<EnemyBase>();
        private static Target Target(string name)=>GameObject.Find(name).GetComponent<Target>();
        private static BreakableWall Wall(string name)=>Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(w=>w.name==name);
        private static Level03SecurityGate Gate(string name)=>GameObject.Find(name).GetComponent<Level03SecurityGate>();
        private static void Invoke(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    }
}
