using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class CampaignCombatTuningTests
    {
        private static readonly Vector3 Arena = new Vector3(0f, 1.4f, 34f);
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1f;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("CombatTuningCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        [UnityTest] public IEnumerator AllCampaignBodiesAreLargerGroundedAndKeepTheMergedPresentation()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                Assert.That(spirit.transform.lossyScale,Is.EqualTo(Vector3.one*1.25f));
                Assert.That(spirit.CurrentHP,Is.EqualTo(5));
                Assert.That(spirit.GetComponent<Collider>().bounds.min.y,Is.EqualTo(.15f).Within(.02f));
                foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    bool heavy=enemy is HeavyEnemy;
                    Assert.That(enemy.transform.lossyScale.x,Is.EqualTo(heavy?1.625f:1.25f).Within(.001f));
                    Assert.That(enemy.DetectionRange,Is.EqualTo(heavy?7f:9f));
                    Assert.That(enemy.VisionAngle,Is.EqualTo(75f));
                    var cc=enemy.GetComponent<CharacterController>();
                    Assert.That(cc.bounds.min.y,Is.EqualTo(.15f).Within(.02f),enemy.name);
                    var attack=enemy.GetComponent<EnemyContactDamage>();
                    Assert.That(Read<float>(attack,heavy?"smashRange":"shootRange"),Is.EqualTo(heavy?3.5f:8f));
                    if (!heavy)
                    {
                        Assert.That(Read<GameObject>(attack,"projectilePrefab"),Is.Not.Null);
                        Assert.That(Read<Transform>(attack,"firePoint"),Is.Not.Null);
                    }
                    float radius=cc.radius*enemy.transform.lossyScale.x;
                    float half=cc.height*enemy.transform.lossyScale.y*.5f-radius;
                    var hits=Physics.OverlapCapsule(cc.bounds.center+Vector3.up*half,cc.bounds.center-Vector3.up*half,radius*.97f,1<<0,QueryTriggerInteraction.Ignore)
                        .Where(c=>c.GetComponentInParent<EnemyBase>()==null&&c.GetComponentInParent<SpiritController>()==null
                            &&!c.name.EndsWith("_Floor")&&!c.name.StartsWith("Hall_")).ToArray();
                    Assert.That(hits,Is.Empty,enemy.name+" must not spawn embedded in cover or a wall.");
                }
                Assert.That(Read<Vector3>(Camera.main.GetComponent<CameraFollow>(),"offset"),Is.EqualTo(new Vector3(0,15,-11)));
                var ammo=Object.FindFirstObjectByType<AmmoDisplay>(FindObjectsInactive.Include);
                Assert.That(Read<Sprite>(ammo,"bulletSprite"),Is.Not.Null);Assert.That(Read<Sprite>(ammo,"hammerSprite"),Is.Not.Null);
                if(scene==CampaignLevels.FirstScene)Assert.That(GameObject.Find("Heavy_03").transform.position.z,Is.EqualTo(14f).Within(.02f));
                if(scene==CampaignLevels.SecondScene)Assert.That(spirit.GetComponent<PossessionCharge>().Count,Is.EqualTo(1));
            }
        }
        [UnityTest] public IEnumerator WiderLongerVisionDetectsFlanksButKeepsRearAndWallCounterplay()
        {
            yield return Load(CampaignLevels.FirstScene);var guard=Guard();var spirit=Spirit();Move(guard,Arena,Vector3.forward);
            var ray=Quaternion.Euler(0,30,0)*Vector3.forward;
            spirit.RestoreAt(Arena+ray*8f);Physics.SyncTransforms();Assert.That(guard.CanSee(spirit.transform),Is.True);
            spirit.RestoreAt(Arena+Vector3.forward*9.5f);Physics.SyncTransforms();Assert.That(guard.CanSee(spirit.transform),Is.False);
            spirit.RestoreAt(Arena-Vector3.forward*3f);Physics.SyncTransforms();Assert.That(guard.CanSee(spirit.transform),Is.False);
            Assert.That(guard.CanBePossessedFrom(spirit.transform),Is.True);
            spirit.RestoreAt(Arena+Vector3.forward*3f);Physics.SyncTransforms();Assert.That(guard.CanBePossessedFrom(spirit.transform),Is.False);
            spirit.RestoreAt(Arena+Vector3.forward*8f);Wall(Arena+Vector3.forward*4f);Physics.SyncTransforms();
            Assert.That(guard.CanSee(spirit.transform),Is.False);
        }
        [UnityTest] public IEnumerator VisibleRedConeMatchesTheNewDetectionAngleAndDistance()
        {
            yield return Load(CampaignLevels.FirstScene);var guard=Guard();Move(guard,Arena,Vector3.forward);
            yield return null;yield return null;
            var vertices=GameObject.Find(guard.name+" Vision Cone").GetComponent<MeshFilter>().sharedMesh.vertices;
            Assert.That(vertices.Skip(1).Max(v=>v.magnitude),Is.EqualTo(9f).Within(.02f));
            Assert.That(Vector3.Angle(vertices[1],vertices[vertices.Length-1]),Is.EqualTo(75f).Within(.1f));
        }
        [UnityTest] public IEnumerator GuardActuallyHitsTheSpiritFromSixMetresAfterItsWarning()
        {
            yield return Load(CampaignLevels.FirstScene);var guard=Guard();var spirit=Spirit();Move(guard,Arena,Vector3.forward);
            spirit.RestoreAt(Arena+Vector3.forward*6f);Physics.SyncTransforms();Set(guard,"chaseTarget",spirit);
            guard.GetComponent<EnemyContactDamage>().enabled=true;int hp=spirit.CurrentHP;
            yield return new WaitForSeconds(.25f);Assert.That(spirit.CurrentHP,Is.EqualTo(hp),"Keep the reaction warning.");
            float until=Time.time+1.8f;while(spirit.CurrentHP==hp&&Time.time<until)yield return null;
            Assert.That(spirit.CurrentHP,Is.LessThan(hp),"Increasing the cone must change actual shooting reach too.");
        }
        [UnityTest] public IEnumerator HeavyActuallyReachesThreePointTwoMetresAfterItsWindup()
        {
            yield return Load(CampaignLevels.FirstScene);var heavy=Object.FindFirstObjectByType<HeavyEnemy>();var spirit=Spirit();
            Move(heavy,Arena,Vector3.forward);spirit.RestoreAt(Arena+Vector3.forward*3.2f);Physics.SyncTransforms();
            Set(heavy,"chaseTarget",spirit);heavy.GetComponent<EnemyContactDamage>().enabled=true;int hp=spirit.CurrentHP;
            yield return new WaitForSeconds(.25f);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
            float until=Time.time+2f;while(spirit.CurrentHP==hp&&Time.time<until)yield return null;
            Assert.That(spirit.CurrentHP,Is.LessThan(hp));
        }
        [UnityTest] public IEnumerator LongerGuardAttacksStillCannotShootThroughASolidWall()
        {
            yield return Load(CampaignLevels.FirstScene);var guard=Guard();var spirit=Spirit();Move(guard,Arena,Vector3.forward);
            spirit.RestoreAt(Arena+Vector3.forward*6f);Wall(Arena+Vector3.forward*3f);Physics.SyncTransforms();
            Set(guard,"chaseTarget",spirit);guard.GetComponent<EnemyContactDamage>().enabled=true;int hp=spirit.CurrentHP;
            yield return new WaitForSeconds(1.5f);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator LargerSpiritStillEjectsSafelyBehindAndReceivesTwoSecondsProtection()
        {
            yield return Load(CampaignLevels.FirstScene);var guard=Guard();var spirit=Spirit();Move(guard,Arena,Vector3.forward);
            spirit.RestoreAt(Arena-Vector3.forward*3f);Physics.SyncTransforms();var pc=spirit.GetComponent<PossessionController>();
            typeof(PossessionController).GetMethod("Possess",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pc,new object[]{guard});
            Assert.That(pc.PossessedEnemy,Is.SameAs(guard));var forward=guard.transform.forward;
            Assert.That(pc.TryEject(false),Is.True);
            Assert.That(Vector3.Dot(spirit.transform.position-guard.transform.position,-forward),Is.GreaterThan(.8f));
            int hp=spirit.CurrentHP;spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
            Assert.That(spirit.DamageProtectionRemaining,Is.EqualTo(2f).Within(.03f));
            yield return new WaitForSeconds(2.1f);spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp-1));
        }
        [UnityTest] public IEnumerator LargerHeavyAttacksDoNotPassThroughSolidWalls()
        {
            yield return Load(CampaignLevels.FirstScene);var heavy=Object.FindFirstObjectByType<HeavyEnemy>();var spirit=Spirit();
            Move(heavy,Arena,Vector3.forward);spirit.RestoreAt(Arena+Vector3.forward*3.2f);
            Wall(Arena+Vector3.forward*1.6f);Physics.SyncTransforms();Set(heavy,"chaseTarget",spirit);
            heavy.GetComponent<EnemyContactDamage>().enabled=true;int hp=spirit.CurrentHP;
            yield return new WaitForSeconds(1.6f);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator RaycastAndTriggerShareOneSpiritHitAndShieldAbsorbsTheBullet()
        {
            yield return Load(CampaignLevels.FirstScene);var spirit=Spirit();int hp=spirit.CurrentHP;int damageEvents=0;
            EventBus.SpiritDamaged+=(a,b)=>damageEvents++;
            var shot=new GameObject("Raycast hostile bullet").AddComponent<Projectile>();var enemyShot=shot.gameObject.AddComponent<EnemyShot>();enemyShot.Init(1,8f);
            typeof(Projectile).GetMethod("HandleHit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shot,new object[]{spirit.GetComponent<Collider>()});
            Assert.That(enemyShot.TryHitSpirit(spirit.GetComponent<Collider>()),Is.True);
            Assert.That(spirit.CurrentHP,Is.EqualTo(hp-1));Assert.That(damageEvents,Is.EqualTo(1));
            spirit.GrantDamageProtection(2f);
            var shielded=new GameObject("Shielded hostile bullet").AddComponent<Projectile>();var second=shielded.gameObject.AddComponent<EnemyShot>();second.Init(1,8f);
            typeof(Projectile).GetMethod("HandleHit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shielded,new object[]{spirit.GetComponent<Collider>()});
            Assert.That(spirit.CurrentHP,Is.EqualTo(hp-1));Assert.That(damageEvents,Is.EqualTo(1));
        }
        private static IEnumerator Load(string scene)
        {
            Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(scene);yield return null;
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<HuntedEnemyPressure>(FindObjectsSortMode.None))e.enabled=false;
        }
        private static GuardEnemy Guard()=>GameObject.Find("Guard_03").GetComponent<GuardEnemy>();
        private static SpiritController Spirit()=>Object.FindFirstObjectByType<SpiritController>();
        private static T Read<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o,string f,object v)=>typeof(EnemyBase).GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
        private static void Move(EnemyBase e,Vector3 at,Vector3 forward)
        {
            var cc=e.GetComponent<CharacterController>();cc.enabled=false;at.y=e.transform.position.y;
            e.transform.SetPositionAndRotation(at,Quaternion.LookRotation(forward));cc.enabled=true;Physics.SyncTransforms();
        }
        private static void Wall(Vector3 at)
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Tuning sight blocker";at.y=2f;
            wall.transform.position=at;wall.transform.localScale=new Vector3(7f,4f,.5f);
        }
    }
}
