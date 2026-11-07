#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Warehouse.Tests
{
    public sealed class WarehouseSafetyTests
    {
        private WarehouseLevel level;
        private SpiritController spirit;
        private PossessionController possession;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale=1;EventBus.Clear();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Level03Warehouse/Level_03_Warehouse.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;level=Object.FindFirstObjectByType<WarehouseLevel>();spirit=level.Spirit;possession=level.Possession;
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("WarehouseSafetyCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [UnityTest] public IEnumerator ManualEjectGrantsTwoSecondsOfRealDamageProtectionThenExpires()
        {
            Possess(Host("Sorting_hall_Guard"));Assert.That(possession.TryEject(false),Is.True);
            var shield=spirit.GetComponent<WarehouseEjectShield>();int hp=spirit.CurrentHP;int damageEvents=0;EventBus.SpiritDamaged+=(a,b)=>damageEvents++;
            Assert.That(spirit.DamageProtectionRemaining,Is.EqualTo(2f).Within(.02f));Assert.That(shield.IsVisible,Is.True);
            spirit.TakeDamage(100);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));Assert.That(GameManager.Instance.CurrentState,Is.EqualTo(LevelState.InProgress));
            yield return new WaitForSeconds(1.85f);spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));Assert.That(damageEvents,Is.Zero);
            yield return new WaitForSeconds(.2f);Assert.That(spirit.IsDamageProtected,Is.False);Assert.That(shield.IsVisible,Is.False);
            spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp-1));Assert.That(damageEvents,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator TimerForcedEjectAlsoProtectsAgainstAnActualEnemyBullet()
        {
            var host=Host("Sorting_hall_Guard");Possess(host);Set(possession,"possessTimer",.01f);yield return new WaitForSeconds(.06f);
            Assert.That(possession.CurrentState,Is.EqualTo(PlayerState.Spirit));Assert.That(host.IsHostile,Is.True);
            Assert.That(spirit.DamageProtectionRemaining,Is.InRange(1.85f,2f));int hp=spirit.CurrentHP;
            var projectile=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab"),spirit.transform.position-host.transform.forward*2f,Quaternion.LookRotation(host.transform.forward));
            projectile.AddComponent<EnemyShot>().Init(1,6f);yield return new WaitForSeconds(.25f);
            Assert.That(spirit.CurrentHP,Is.EqualTo(hp));Assert.That(projectile==null,Is.True,"The bullet should be consumed on the protected Spirit.");
        }
        [Test] public void FreshSpiritAndAnEjectWithoutAHostDoNotGetFreeProtection()
        {
            Assert.That(spirit.IsDamageProtected,Is.False);possession.TryEject(false);Assert.That(spirit.IsDamageProtected,Is.False);
            spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(spirit.MaxHP-1));
        }
        [Test] public void EveryWarehouseHostHasAnAssignedHomeContainingItsSpawnAndPatrol()
        {
            foreach(var host in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                Assert.That(host.HasHomeBounds,Is.True,host.name);Assert.That(host.IsInsideHome(host.transform.position),Is.True,host.name);
                var points=(Transform[])typeof(EnemyBase).GetField("waypoints",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(host);
                Assert.That(points.All(p=>p!=null&&host.IsInsideHome(p.position)),Is.True,host.name+" patrol leaves its room");
            }
        }
        [UnityTest] public IEnumerator GuardDropsPursuitAsSoonAsSpiritLeavesItsRoom()
        {
            var host=Host("Sorting_hall_Guard");Move(host,Map(61,65),Vector3.left);spirit.RestoreAt(Map(58,65));host.enabled=true;
            yield return new WaitForSeconds(.08f);Assert.That(host.IsChasing,Is.True,"Guard must still chase inside its room.");
            spirit.RestoreAt(Map(55.5f,65));Physics.SyncTransforms();Assert.That(host.IsChasing,Is.False);
            Assert.That(host.CanPursue(spirit.transform),Is.False);int hp=spirit.CurrentHP;
            host.GetComponent<EnemyContactDamage>().enabled=true;
            yield return new WaitForSeconds(2f);
            Assert.That(host.IsChasing,Is.False);Assert.That(host.IsInsideHome(host.transform.position),Is.True);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator HostileHeavyCannotSmashSpiritAcrossItsRoomBoundary()
        {
            var host=Host("Service_lobby_Heavy");Move(host,Map(36.5f,48),Vector3.left);spirit.RestoreAt(Map(34,48));Physics.SyncTransforms();
            host.enabled=true;host.GetComponent<EnemyContactDamage>().enabled=true;
            host.OnForcedEject(5f);EventBus.Ejected?.Invoke(host,true);int hp=spirit.CurrentHP;
            Assert.That(spirit.IsDamageProtected,Is.False,"This synthetic NPC event must not grant the player's shield.");
            yield return new WaitForSeconds(1.5f);
            Assert.That(host.IsChasing,Is.False);Assert.That(host.IsInsideHome(host.transform.position),Is.True);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator PlayerCanDriveBodyOutsideItsHomeAndItReturnsWithoutFollowingSpirit()
        {
            var host=Host("Sorting_hall_Guard");Move(host,Map(58.5f,65),Vector3.left);Possess(host);
            typeof(EnemyBase).GetMethod("Move",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(host,new object[]{Vector3.left*3f});
            Assert.That(host.IsInsideHome(host.transform.position),Is.False,"Player movement must not be clamped to the NPC's room.");
            Assert.That(possession.TryEject(false),Is.True);Assert.That(host.CanPursue(spirit.transform),Is.False);
            host.enabled=true;yield return new WaitForSeconds(2f);
            Assert.That(host.IsInsideHome(host.transform.position),Is.True);Assert.That(host.IsChasing,Is.False);
        }
        [TestCase("Sorting_hall_Guard")] [TestCase("Machine_Heavy")]
        public void ManualEjectPrefersBehindTheHostWhenClear(string name)
        {
            var host=Host(name);Possess(host);Assert.That(possession.TryEject(false),Is.True);
            var offset=spirit.transform.position-host.transform.position;offset.y=0;
            Assert.That(Vector3.Dot(offset.normalized,host.transform.forward),Is.LessThan(-.95f));
            Assert.That(spirit.IsDamageProtected,Is.True);
        }
        [Test] public void RearWallUsesASafeSideWithoutEjectingThroughTheWall()
        {
            var host=Host("Sorting_hall_Guard");Move(host,Map(70,65),Vector3.forward);Possess(host);
            var wall=new GameObject("Rear eject obstacle");wall.transform.SetPositionAndRotation(host.transform.position-host.transform.forward*1.5f,host.transform.rotation);
            wall.AddComponent<BoxCollider>().size=new Vector3(3f,3f,.25f);Physics.SyncTransforms();
            Assert.That(possession.TryEject(false),Is.True);
            var offset=spirit.transform.position-host.transform.position;offset.y=0;
            Assert.That(Vector3.Dot(offset,-host.transform.forward),Is.LessThan(1f),"Do not teleport past the blocked rear position.");
            var capsule=spirit.GetComponent<CapsuleCollider>();
            Assert.That(Physics.ComputePenetration(capsule,spirit.transform.position,spirit.transform.rotation,wall.GetComponent<Collider>(),wall.transform.position,wall.transform.rotation,out _,out _),Is.False);
            Assert.That(spirit.IsDamageProtected,Is.True);
        }
        [UnityTest] public IEnumerator ProtectionDoesNotDependOnTheSceneHavingAVisualComponent()
        {
            Object.DestroyImmediate(spirit.GetComponent<WarehouseEjectShield>());
            var host=Host("Sorting_hall_Guard");Possess(host);bool protectedAtEvent=false;
            EventBus.Ejected+=(e,forced)=>protectedAtEvent=spirit.IsDamageProtected;
            Assert.That(possession.TryEject(false),Is.True);Assert.That(protectedAtEvent,Is.True);
            int hp=spirit.CurrentHP;spirit.TakeDamage(99);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
            WarehouseSceneSafety.Install(SceneManager.GetActiveScene());yield return null;
            Assert.That(spirit.GetComponent<WarehouseEjectShield>().IsVisible,Is.True);
        }
        [Test] public void OlderLoadedWarehouseSceneRecoversMissingVisualAndRoomAssignments()
        {
            Object.DestroyImmediate(spirit.GetComponent<WarehouseEjectShield>());
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                typeof(EnemyBase).GetField("useHomeBounds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemy,false);
            WarehouseSceneSafety.Install(SceneManager.GetActiveScene());WarehouseSceneSafety.Install(SceneManager.GetActiveScene());
            Assert.That(spirit.GetComponents<WarehouseEjectShield>().Length,Is.EqualTo(1));
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                Assert.That(enemy.HasHomeBounds,Is.True,enemy.name);
                Assert.That(enemy.IsInsideHome(enemy.transform.position),Is.True,enemy.name);
            }
        }
        private void Possess(EnemyBase host)
        {
            Assert.That(spirit.GetComponent<PossessionCharge>().TryCollect(),Is.True);
            spirit.RestoreAt(host.transform.position-host.transform.forward*1.8f);Physics.SyncTransforms();
            typeof(PossessionController).GetMethod("Possess",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(possession,new object[]{host});
            Assert.That(possession.PossessedEnemy,Is.SameAs(host));
        }
        private static EnemyBase Host(string name)=>GameObject.Find(name).GetComponent<EnemyBase>();
        private static Vector3 Map(float x,float y)=>new((x-70)*.8f,1.15f,(90-y)*.8f);
        private static void Move(EnemyBase host,Vector3 at,Vector3 forward)
        {
            var cc=host.GetComponent<CharacterController>();cc.enabled=false;at.y=host.transform.position.y;host.transform.SetPositionAndRotation(at,Quaternion.LookRotation(forward));cc.enabled=true;Physics.SyncTransforms();
        }
        private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    }
}
#endif
