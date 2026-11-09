using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class CampaignConsistencyTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1; var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("ConsistencyCleanup"));
            yield return SceneManager.UnloadSceneAsync(old); EventBus.Clear();
        }

        [UnityTest] public IEnumerator EveryStageJumpsImmediatelyAfterManualAndForcedGuardAndHeavyEjection()
        {
            foreach (string scene in CampaignLevels.SceneNames)
            foreach (bool heavy in new[]{false,true})
            foreach (bool forced in new[]{false,true})
            {
                yield return Load(scene); var spirit=Spirit(); var pc=spirit.GetComponent<PossessionController>();
                var host=PrepareHost(heavy); Assert.That(pc.TryPossess(host),Is.True,scene+" possess "+host.name);
                bool protectedAtEvent=false;EventBus.Ejected+=(e,f)=>protectedAtEvent=spirit.IsDamageProtected&&spirit.GetComponent<EjectShieldVisual>().IsVisible;
                var forward=host.transform.forward;
                if(forced){Set(pc,"possessTimer",.01f);yield return new WaitForSeconds(.06f);}
                else Assert.That(pc.TryEject(false),Is.True);
                Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit),scene+" eject");
                Assert.That(protectedAtEvent,Is.True,"Shield must already be active at the eject event.");
                var offset=spirit.transform.position-host.transform.position;offset.y=0;
                Assert.That(Vector3.Dot(offset.normalized,forward),Is.LessThan(-.95f),scene+" rear ejection");
                Assert.That(host.IsHostile,Is.EqualTo(forced));
                Assert.That(spirit.GetComponents<EjectShieldVisual>().Length,Is.EqualTo(1));
                Assert.That(spirit.GetComponent<Rigidbody>().isKinematic,Is.False);
                float y=spirit.transform.position.y; spirit.RequestJump(); yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                Assert.That(spirit.GetComponent<Rigidbody>().linearVelocity.y,Is.GreaterThan(6f),scene+" Space after eject");
                yield return new WaitForSeconds(.15f);
                Assert.That(spirit.transform.position.y,Is.GreaterThan(y+.5f),scene+" actual jump height");
            }
        }

        [UnityTest] public IEnumerator RepeatedLandingsAndBufferedSpaceNeverRequireANewCollisionEnter()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var rb=spirit.GetComponent<Rigidbody>();
                for(int i=0;i<3;i++)
                {
                    // Reset visibility in an existing floor contact, like possession/ejection.
                    spirit.RestoreAt(spirit.transform.position);spirit.RequestJump();
                    yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                    Assert.That(rb.linearVelocity.y,Is.GreaterThan(6f),scene+" repeat "+i);
                    float deadline=Time.time+2.5f;
                    while(!spirit.IsGrounded&&Time.time<deadline)yield return null;
                    Assert.That(spirit.IsGrounded,Is.True,scene+" must land again");
                }
                var at=spirit.transform.position;spirit.RestoreAt(at+Vector3.up*.25f);rb.linearVelocity=Vector3.down*2f;
                spirit.RequestJump();yield return new WaitForSeconds(.15f);
                Assert.That(rb.linearVelocity.y,Is.GreaterThan(5f),scene+" buffered press just before landing");
            }
        }

        [UnityTest] public IEnumerator SpaceDoesNotDoubleJumpOrActWhilePossessedOrPaused()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var rb=spirit.GetComponent<Rigidbody>();
                spirit.RequestJump();yield return new WaitForSeconds(.25f);float vy=rb.linearVelocity.y;
                spirit.RequestJump();yield return new WaitForSeconds(.05f);
                Assert.That(rb.linearVelocity.y,Is.LessThan(vy+.1f),scene+" no air jump");
                var host=PrepareHost(false);var pc=spirit.GetComponent<PossessionController>();Assert.That(pc.TryPossess(host),Is.True);
                spirit.RequestJump();yield return new WaitForFixedUpdate();Assert.That(rb.isKinematic,Is.True);
                Assert.That(pc.TryEject(false),Is.True);
                Time.timeScale=0;float remaining=spirit.DamageProtectionRemaining;
                spirit.RequestJump();Assert.That(pc.TryPossess(host),Is.False);Assert.That(pc.TryEject(false),Is.False);
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(spirit.DamageProtectionRemaining,Is.EqualTo(remaining).Within(.01f));
                Time.timeScale=1;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                Assert.That(rb.linearVelocity.y,Is.LessThan(1f),"A Space press while paused must not launch on resume.");
            }
        }

        [UnityTest] public IEnumerator ShieldAndDamageExpireTogetherRegardlessOfPossessionCooldown()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var pc=spirit.GetComponent<PossessionController>();
                Set(pc,"ejectCooldown",scene==CampaignLevels.FirstScene?.25f:4f);
                Assert.That(pc.TryPossess(PrepareHost(false)),Is.True);Assert.That(pc.TryEject(false),Is.True);
                int hp=spirit.CurrentHP;var visual=spirit.GetComponent<EjectShieldVisual>();
                spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
                yield return new WaitForSeconds(1.85f);spirit.TakeDamage(1);
                Assert.That(spirit.CurrentHP,Is.EqualTo(hp));Assert.That(visual.IsVisible,Is.True);
                yield return new WaitForSeconds(.22f);
                Assert.That(pc.IsEjectShielded,Is.False);Assert.That(visual.IsVisible,Is.False);
                spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp-1));
            }
        }

        [UnityTest] public IEnumerator EjectionBesideARearWallFindsAClearSideOnEveryStage()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var pc=spirit.GetComponent<PossessionController>();var host=PrepareHost(false);
                Assert.That(pc.TryPossess(host),Is.True);
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Test rear obstruction";
                wall.transform.position=host.transform.position-Vector3.forward*1.5f;wall.transform.localScale=new Vector3(4,5,.25f);
                Physics.SyncTransforms();Assert.That(pc.TryEject(false),Is.True,scene);
                var offset=spirit.transform.position-host.transform.position;
                Assert.That(offset.z,Is.GreaterThan(-1f),"Do not cross the rear wall.");
                var capsule=spirit.GetComponent<Collider>();
                Assert.That(Physics.ComputePenetration(capsule,spirit.transform.position,spirit.transform.rotation,wall.GetComponent<Collider>(),wall.transform.position,wall.transform.rotation,out _,out _),Is.False);
            }
        }

        [UnityTest] public IEnumerator DefeatedAndDestroyedHostsReturnAVisibleProtectedSpirit()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            foreach(bool destroy in new[]{false,true})
            {
                yield return Load(scene);var spirit=Spirit();var pc=spirit.GetComponent<PossessionController>();var host=PrepareHost(false);
                Assert.That(pc.TryPossess(host),Is.True);
                if(destroy)Object.Destroy(host.gameObject);else host.TakeDamage(100);
                yield return null;yield return null;
                Assert.That(pc.CurrentState,Is.EqualTo(PlayerState.Spirit),scene+" host loss");
                Assert.That(spirit.GetComponent<Collider>().enabled,Is.True);Assert.That(spirit.IsDamageProtected,Is.True);
                Assert.That(spirit.GetComponent<Rigidbody>().isKinematic,Is.False);
            }
        }

        [UnityTest] public IEnumerator SharedSettingsCuesAndPatrolBoundariesMatchAcrossCampaign()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var pc=spirit.GetComponent<PossessionController>();
                Assert.That(spirit.MoveSpeed,Is.EqualTo(8f));Assert.That(spirit.JumpSpeed,Is.EqualTo(8f));
                Assert.That(spirit.MaxHP,Is.EqualTo(5));Assert.That(pc.PossessRange,Is.EqualTo(5f));
                Assert.That(Read<Vector3>(Camera.main.GetComponent<CameraFollow>(),"offset"),Is.EqualTo(new Vector3(0,25,-18)));
                foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    Assert.That(e.MoveSpeed,Is.EqualTo(e is HeavyEnemy?4f:5f));Assert.That(e.HasHomeBounds,Is.True,e.name);
                    Assert.That(e.IsInsideHome(e.transform.position),Is.True,e.name);
                    foreach(var wp in Read<Transform[]>(e,"waypoints"))Assert.That(e.IsInsideHome(wp.position),Is.True,e.name+" waypoint");
                }
                var host=PrepareHost(false);var selected=pc.FindPossessableEnemy();Assert.That(selected,Is.SameAs(host));
                Assert.That(pc.TryPossess(host),Is.True);Assert.That(pc.TryEject(false),Is.True);
                spirit.GetComponent<PossessionCharge>().TryCollect();Assert.That(pc.CanStartPossession,Is.False,"Cooldown must agree with the green cue.");
            }
        }

        [UnityTest] public IEnumerator AttacksFreezeDuringPauseAndStayStoppedAfterCompletion()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            foreach(bool complete in new[]{false,true})
            {
                yield return Load(scene);var spirit=Spirit();var host=PrepareHost(true);
                host.ConfigureHomeBounds(new Bounds(spirit.transform.position,Vector3.one*50f));
                var attack=host.GetComponent<EnemyContactDamage>();attack.enabled=true;
                host.OnForcedEject(5f);EventBus.Ejected?.Invoke(host,true);Set(attack,"nextAttackTime",Time.time-.1f);
                int hp=spirit.CurrentHP;
                if(complete){EventBus.LevelCompleted?.Invoke();Time.timeScale=1f;}
                else Time.timeScale=0;
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(Read<float>(attack,"smashAt"),Is.EqualTo(0f));
                Assert.That(spirit.CurrentHP,Is.EqualTo(hp));
                if(complete)
                {
                    spirit.TakeDamage(1);Assert.That(spirit.CurrentHP,Is.EqualTo(hp),"Finished levels stop damage.");
                    Assert.That(spirit.GetComponent<PossessionController>().CanStartPossession,Is.False);
                }
                else
                {
                    Time.timeScale=1;
                    float until=Time.time+1.6f;
                    while(spirit.CurrentHP==hp&&Time.time<until)yield return null;
                    Assert.That(spirit.CurrentHP,Is.LessThan(hp),"The real attack must resume normally: "+scene+" distance="+Vector3.Distance(host.transform.position,spirit.transform.position)+" range="+Read<float>(attack,"smashRange")+" kind="+Field(attack,"attack").GetValue(attack)+" hostile="+host.IsHostile+" windup="+Read<float>(attack,"smashAt")+" protected="+spirit.IsDamageProtected);
                }
            }
        }

        [UnityTest] public IEnumerator HuntedContactDamageRespectsPauseAndCompletion()
        {
            yield return Load(CampaignLevels.SecondScene);
            var director=Object.FindFirstObjectByType<Level02Director>();int hp=director.Spirit.CurrentHP;
            Time.timeScale=0;Assert.That(director.TryContactDamage(),Is.False);
            Assert.That(director.Spirit.CurrentHP,Is.EqualTo(hp));
            Time.timeScale=1;Assert.That(director.TryContactDamage(),Is.True);
            EventBus.LevelCompleted?.Invoke();Time.timeScale=1;Set(director,"nextContactHit",0f);
            Assert.That(director.TryContactDamage(),Is.False);
            Assert.That(director.Spirit.CurrentHP,Is.EqualTo(hp-1));
        }

        [UnityTest] public IEnumerator GhostModelSyncNeverEnablesInactiveShieldRenderers()
        {
            foreach(string scene in CampaignLevels.SceneNames)
            {
                yield return Load(scene);var spirit=Spirit();var visual=spirit.GetComponent<EjectShieldVisual>();
                var sync=spirit.GetComponent<SpiritVisualSync>();
                // Rescan after every effect exists to reproduce the former initialization-order race.
                typeof(SpiritVisualSync).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(sync,null);
                var late=typeof(SpiritVisualSync).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
                visual.Refresh();late.Invoke(sync,null);
                Assert.That(visual.IsVisible,Is.False,scene+" ghost sync enabled the inactive shield ring");
                Assert.That(Read<Renderer>(visual,"bubble").enabled,Is.False,scene+" ghost sync enabled the inactive bubble");
                var pc=spirit.GetComponent<PossessionController>();Assert.That(pc.TryPossess(PrepareHost(false)),Is.True);
                late.Invoke(sync,null);
                Assert.That(spirit.transform.Find("P5_Model").GetComponentsInChildren<Renderer>().All(r=>!r.enabled),Is.True);
                Assert.That(pc.TryEject(false),Is.True);late.Invoke(sync,null);
                Assert.That(visual.IsVisible,Is.True);Assert.That(Read<Renderer>(visual,"bubble").enabled,Is.True);
            }
        }

        private static EnemyBase PrepareHost(bool heavy)
        {
            var spirit=Spirit();var baseAt=spirit.transform.position;
            // Use the genuine entry floor and controller, with other NPCs paused for deterministic input tests.
            var host=Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).First(e=>(e is HeavyEnemy)==heavy);
            var cc=host.GetComponent<CharacterController>();cc.enabled=false;
            host.transform.SetPositionAndRotation(new Vector3(baseAt.x,host.transform.position.y,baseAt.z+3f),Quaternion.identity);cc.enabled=true;
            spirit.RestoreAt(baseAt);Physics.SyncTransforms();
            return host;
        }
        private static SpiritController Spirit()=>Object.FindFirstObjectByType<SpiritController>();
        private static IEnumerator Load(string scene)
        {
            Time.timeScale=1;EventBus.Clear();yield return SceneManager.LoadSceneAsync(scene);
            foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
            foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
            yield return null;yield return new WaitForFixedUpdate();yield return null;
        }
        private static FieldInfo Field(object obj,string name)
        {for(var type=obj.GetType();type!=null;type=type.BaseType){var f=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(f!=null)return f;}throw new System.Exception(name);}
        private static T Read<T>(object obj,string name)=>(T)Field(obj,name).GetValue(obj);
        private static void Set(object obj,string name,object value)=>Field(obj,name).SetValue(obj,value);
    }
}
