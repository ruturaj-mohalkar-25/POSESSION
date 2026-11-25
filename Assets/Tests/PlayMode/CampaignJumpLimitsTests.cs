using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Possession.Tests {
public sealed class CampaignJumpLimitsTests {
 [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("JumpLimitsCleanup"));yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();}
 [UnityTest] public IEnumerator FirstThreeStagesCannotJumpFromCrateOntoTheirLowestBoundaryWall(){
  foreach(var scene in CampaignLevels.SceneNames){
   EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync(scene);yield return null;
   foreach(var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))e.enabled=false;
   foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
   foreach(var e in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))Object.DestroyImmediate(e.gameObject);
   foreach(var e in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None))e.PersistReports=false;
   var walls=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>!c.isTrigger&&c.bounds.size.y>=2.9f&&c.name.ToLowerInvariant().Contains("wall")&&c.GetComponentInParent<EnemyBase>()==null).ToArray();
   Assert.That(walls,Is.Not.Empty,scene);float height=walls.Min(c=>c.bounds.size.y);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(20,1,20);
   var crate=new GameObject("Jump fixture crate");crate.transform.position=new Vector3(1000,0,1000);HidingCrate.Build(crate.transform);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Wall_JumpFixture";wall.transform.position=new Vector3(1000,height*.5f,1001.8f);wall.transform.localScale=new Vector3(10,height,.4f);
   var spirit=Object.FindFirstObjectByType<SpiritController>();var body=spirit.GetComponent<Rigidbody>();var capsule=spirit.GetComponent<Collider>();
   spirit.RestoreAt(new Vector3(1000,crate.GetComponent<Collider>().bounds.max.y+capsule.bounds.extents.y+.03f,1000));
   var driver=spirit.gameObject.AddComponent<CampaignJumpInput>();driver.Spirit=spirit;
   yield return new WaitForSeconds(.3f);Assert.That(spirit.IsGrounded,Is.True,scene);
   float startFeet=capsule.bounds.min.y,peakFeet=startFeet;
   spirit.RequestJump();float approachAt=Time.time+.55f;
   while(Time.time<approachAt){peakFeet=Mathf.Max(peakFeet,capsule.bounds.min.y);yield return null;}
   Assert.That(peakFeet-startFeet,Is.InRange(.15f,.45f),scene+" cover hop must not provide another full-height jump.");
   bool stood=false;float until=Time.time+1.7f;
   while(Time.time<until){driver.Input=new Vector2(0,Mathf.Clamp((1001.8f-body.position.z)/(spirit.MoveSpeed*Time.fixedDeltaTime),-1,1));if(spirit.IsGrounded&&capsule.bounds.min.y>height-.1f)stood=true;yield return null;}
   driver.Input=Vector2.zero;
   Assert.That(stood,Is.False,scene+" must not land on boundary from crate.");
   Assert.That(body.position.z,Is.LessThan(1001.4f),scene+" must stay inside boundary.");
  }
 }
 [UnityTest] public IEnumerator FirstThreeStagesUseAShortVisibleGroundJump(){
  foreach(var scene in CampaignLevels.SceneNames){EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync(scene);yield return null;
   foreach(var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))e.enabled=false;
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(20,1,20);
   var spirit=Object.FindFirstObjectByType<SpiritController>();var collider=spirit.GetComponent<Collider>();
   spirit.RestoreAt(new Vector3(1000,collider.bounds.extents.y+.03f,1000));yield return new WaitForSeconds(.25f);
   float start=collider.bounds.min.y,peak=start;spirit.RequestJump();float until=Time.time+1.5f;
   while(Time.time<until){peak=Mathf.Max(peak,collider.bounds.min.y);yield return null;}
   Assert.That(peak-start,Is.InRange(.6f,1.4f),scene+" short jump should stay visible without reaching architecture.");
  }
 }
 [UnityTest] public IEnumerator TutorialKeepsItsExistingJump(){EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync(TutorialScene.Name);yield return null;Assert.That(Object.FindFirstObjectByType<SpiritController>().JumpSpeed,Is.EqualTo(8f));}
}
[DefaultExecutionOrder(-10000)] public sealed class CampaignJumpInput:MonoBehaviour {public SpiritController Spirit;public Vector2 Input;private void FixedUpdate()=>typeof(SpiritController).GetField("moveInput",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Spirit,Input);}
}
