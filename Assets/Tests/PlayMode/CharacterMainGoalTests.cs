using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Possession.Tests
{
    public class CharacterMainGoalTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("CharacterGoalCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();
        }
        private static IEnumerator Load(string name)
        {
            EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync(name);
            foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))enemy.enabled=false;
            foreach(var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))attack.enabled=false;
            yield return null;yield return null;
        }
        [UnityTest] public IEnumerator EveryStageAndTutorialGetsVisibleAvatarsAndAllEnemyContactShadows()
        {
            foreach(var name in CampaignLevels.SceneNames.Concat(new[]{TutorialScene.Name}))
            {
                yield return Load(name);
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                var avatar=spirit.GetComponent<CharacterAvatarView>();Assert.That(avatar.IsShowing,Is.True,name);
                Assert.That(avatar.Model.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(spirit.GetComponent<Renderer>().forceRenderingOff,Is.True);
                var enemies=Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                foreach(var guard in enemies.OfType<GuardEnemy>())
                {
                    var view=guard.GetComponent<CharacterAvatarView>();Assert.That(view.IsShowing,Is.True);
                    Assert.That(view.Model.Find("Cap crown"),Is.Not.Null);Assert.That(view.Model.Find("Sidearm slide"),Is.Not.Null);
                    Assert.That(view.Model.GetComponentsInChildren<Collider>(true),Is.Empty);
                }
                var shadows=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Gold_EnemyContactShadow").ToArray();
                Assert.That(shadows.Length,Is.EqualTo(enemies.Length),name);
                Assert.That(shadows.All(s=>s.enabled),Is.True,name);
                CharacterPresentationInstaller.Install(SceneManager.GetActiveScene());
                Assert.That(Object.FindObjectsByType<GoldContactShadows>(FindObjectsSortMode.None),Has.Length.EqualTo(1));
            }
        }
        [UnityTest] public IEnumerator RealPossessionWhitesOnlyUniformAndAvoidsDoubleShadowThenEjectsCleanly()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var pc=spirit.GetComponent<PossessionController>();
            var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();
            bool entered=false;
            foreach(var dir in new[]{-guard.transform.forward,Vector3.left,Vector3.right,Vector3.back})
            {
                spirit.RestoreAt(guard.transform.position+dir*2.5f);Physics.SyncTransforms();if(pc.TryPossess(guard)){entered=true;break;}
            }
            Assert.That(entered,Is.True);yield return null;yield return null;
            var avatar=guard.GetComponent<CharacterAvatarView>();
            Assert.That(spirit.GetComponent<CharacterAvatarView>().IsShowing,Is.False);
            Assert.That(avatar.IsShowing,Is.True);
            Assert.That(avatar.Model.Find("Torso").GetComponent<Renderer>().sharedMaterial.color,Is.EqualTo(Color.white));
            Assert.That(avatar.Model.Find("Cap crown").GetComponent<Renderer>().sharedMaterial.color.b,Is.LessThan(.4f));
            var shadows=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.Contains("ContactShadow")&&r.enabled
                && Vector2.Distance(new Vector2(r.transform.position.x,r.transform.position.z),new Vector2(guard.transform.position.x,guard.transform.position.z))<.1f).ToArray();
            Assert.That(shadows,Has.Length.EqualTo(1),"A possessed Guard must not receive both its NPC and player stamp.");
            Assert.That(pc.TryEject(false),Is.True);yield return null;yield return null;
            Assert.That(spirit.GetComponent<CharacterAvatarView>().IsShowing,Is.True);
            Assert.That(avatar.Model.Find("Torso").GetComponent<Renderer>().sharedMaterial.color.b,Is.LessThan(.4f));
            Assert.That(spirit.IsDamageProtected,Is.True);
        }
        [UnityTest] public IEnumerator DisableAndReenableRestoresOriginalBodyThenTheAvatarWithoutDuplicatingModels()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var view=spirit.GetComponent<CharacterAvatarView>();
            var renderer=spirit.GetComponent<Renderer>();view.enabled=false;
            Assert.That(renderer.forceRenderingOff,Is.False);Assert.That(view.IsShowing,Is.False);
            view.enabled=true;yield return null;
            Assert.That(renderer.forceRenderingOff,Is.True);Assert.That(view.IsShowing,Is.True);
            Assert.That(spirit.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Character Avatar"),Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RuntimeClonesDoNotKeepDuplicateVisualBodies()
        {
            yield return Load(CampaignLevels.FirstScene);
            var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();
            var clone=Object.Instantiate(guard.gameObject);clone.transform.position=guard.transform.position+Vector3.right*6;
            yield return null;yield return new WaitForSeconds(.6f);
            var view=clone.GetComponent<CharacterAvatarView>();Assert.That(view.IsShowing,Is.True);
            Assert.That(clone.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Character Avatar"),Is.EqualTo(1));
            Assert.That(clone.GetComponent<Collider>().enabled,Is.True);
        }
        [UnityTest] public IEnumerator CombatWarningTintsTheVisiblePoliceModelAndThenRestoresIt()
        {
            yield return Load(CampaignLevels.FirstScene);
            var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();
            var attack=guard.GetComponent<EnemyContactDamage>();attack.enabled=true;
            var torso=guard.GetComponent<CharacterAvatarView>().Model.Find("Torso").GetComponent<Renderer>();
            var material=torso.sharedMaterial;
            typeof(EnemyContactDamage).GetMethod("Blink",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(attack,null);
            var block=new MaterialPropertyBlock();torso.GetPropertyBlock(block);
            Assert.That(block.HasColor("_Color"),Is.True,"Warning must tint the new visible model, not just the hidden legacy capsule.");
            Color tint=block.GetColor("_Color");Assert.That(tint.r,Is.GreaterThan(tint.g));
            yield return new WaitForSeconds(.2f);torso.GetPropertyBlock(block);
            Assert.That(block.HasColor("_Color"),Is.False);Assert.That(torso.sharedMaterial,Is.SameAs(material));
        }
        [UnityTest] public IEnumerator AdditiveLightingOwnersRestoreOriginalQualityAfterTheLastOwnerLeaves()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            var quality=QualitySettings.shadows;var resolution=QualitySettings.shadowResolution;float distance=QualitySettings.shadowDistance;
            yield return Load(CampaignLevels.FirstScene);
            var first=Object.FindFirstObjectByType<CharacterShadowLighting>();
            var auxiliary=SceneManager.CreateScene("CharacterGoalAux");CharacterShadowLighting.Install(auxiliary);
            var second=auxiliary.GetRootGameObjects().Select(r=>r.GetComponent<CharacterShadowLighting>()).First(c=>c!=null);
            first.enabled=false;Assert.That(QualitySettings.shadows,Is.EqualTo(ShadowQuality.All));
            second.enabled=false;Assert.That(QualitySettings.shadows,Is.EqualTo(quality));
            Assert.That(QualitySettings.shadowResolution,Is.EqualTo(resolution));Assert.That(QualitySettings.shadowDistance,Is.EqualTo(distance));
            yield return SceneManager.UnloadSceneAsync(auxiliary);
        }
        [UnityTest] public IEnumerator DisablingGuardAvatarDuringPossessionKeepsFallbackVisibleAfterEjection()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var pc=spirit.GetComponent<PossessionController>();
            var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();guard.enabled=true;guard.MoveSpeed=0;
            bool entered=false;
            foreach(var dir in new[]{-guard.transform.forward,Vector3.left,Vector3.right,Vector3.back})
            {spirit.RestoreAt(guard.transform.position+dir*2.5f);Physics.SyncTransforms();if(pc.TryPossess(guard)){entered=true;break;}}
            Assert.That(entered,Is.True);yield return null;yield return null;
            var avatar=guard.GetComponent<CharacterAvatarView>();avatar.enabled=false;
            EventBus.PossessTimerUpdated?.Invoke(.2f,10f);yield return null;
            Assert.That(pc.TryEject(false),Is.True);
            Assert.That(guard.GetComponent<Renderer>().forceRenderingOff,Is.False);
            yield return null;yield return null;
            Assert.That(guard.GetComponent<Renderer>().forceRenderingOff,Is.False);
        }
        [UnityTest] public IEnumerator DisabledAvatarRespectsAnExternallyHiddenFallbackThroughPossession()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var pc=spirit.GetComponent<PossessionController>();
            var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();guard.enabled=true;guard.MoveSpeed=0;
            guard.GetComponent<CharacterAvatarView>().enabled=false;var body=guard.GetComponent<Renderer>();body.forceRenderingOff=true;
            bool entered=false;foreach(var dir in new[]{-guard.transform.forward,Vector3.left,Vector3.right,Vector3.back})
            {spirit.RestoreAt(guard.transform.position+dir*2.5f);Physics.SyncTransforms();if(pc.TryPossess(guard)){entered=true;break;}}
            Assert.That(entered,Is.True);yield return null;
            Assert.That(pc.TryEject(false),Is.True);Assert.That(body.forceRenderingOff,Is.True);
        }
        [UnityTest] public IEnumerator AvatarPartsMatchTheApprovedPrimitiveProportions()
        {
            yield return Load(CampaignLevels.FirstScene);
            foreach(var avatar in Object.FindObjectsByType<CharacterAvatarView>(FindObjectsSortMode.None))
            {
                var torso=avatar.Model.Find("Torso");var mesh=torso.GetComponent<MeshFilter>().sharedMesh;
                var size=Vector3.Scale(mesh.bounds.size,torso.localScale);
                Assert.That(size.x,Is.EqualTo(.85f).Within(.001f));Assert.That(size.y,Is.EqualTo(1.14f).Within(.001f));
                var head=avatar.Model.Find("Head");size=Vector3.Scale(head.GetComponent<MeshFilter>().sharedMesh.bounds.size,head.localScale);
                Assert.That(size.y,Is.EqualTo(.76f).Within(.001f));
            }
        }
        [UnityTest] public IEnumerator AllActorsHaveDistanceDrivenFeetThatStopAndPause()
        {
            yield return Load(CampaignLevels.FirstScene);
            foreach(var avatar in Object.FindObjectsByType<CharacterAvatarView>(FindObjectsSortMode.None))
            {
                Assert.That(avatar.Model.Find("Trouser leg"),Is.Not.Null,avatar.name);
                var feet=avatar.Model.GetComponentsInChildren<Transform>().Where(t=>t.name=="Boot").ToArray();
                Assert.That(feet.Length,Is.EqualTo(2),avatar.name);
                for(int i=0;i<12;i++){avatar.transform.position+=Vector3.forward*.045f;avatar.AdvanceGait(.02f);}
                Assert.That(avatar.GaitWeight,Is.GreaterThan(.8f));
                Assert.That(Mathf.Abs(feet[0].localPosition.z-feet[1].localPosition.z),Is.GreaterThan(.15f),"Feet must visibly alternate.");
                var before=feet.Select(t=>t.localPosition).ToArray();float phase=avatar.GaitPhase;
                avatar.AdvanceGait(0);Assert.That(avatar.GaitPhase,Is.EqualTo(phase));
                Assert.That(feet.Select(t=>t.localPosition),Is.EqualTo(before));
                for(int i=0;i<30;i++)avatar.AdvanceGait(.02f);
                Assert.That(avatar.GaitWeight,Is.EqualTo(0));
                Assert.That(feet[0].localPosition.y,Is.EqualTo(.105f).Within(.001f));
                Assert.That(feet[1].localPosition.y,Is.EqualTo(.105f).Within(.001f));
            }
            Assert.That(Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).All(h=>h.GetComponent<CharacterAvatarView>()!=null),Is.True);
        }
        [UnityTest] public IEnumerator HeavyAvatarPreservesPossessedSmashGuide()
        {
            yield return Load(CampaignLevels.FirstScene);
            var heavy=Object.FindFirstObjectByType<HeavyEnemy>();
            EventBus.Possessed?.Invoke(heavy,PlayerState.Possessed_Heavy);
            heavy.GetComponent<CharacterAvatarView>().Refresh();yield return null;
            var guide=heavy.transform.Find("Heavy Smash Guide");
            Assert.That(guide,Is.Not.Null);Assert.That(guide.gameObject.activeInHierarchy,Is.True);
            Assert.That(guide.GetComponentsInChildren<LineRenderer>().Any(r=>r.enabled),Is.True);
        }
        [UnityTest] public IEnumerator SolidCrateBlocksSightAndClipsRedConeAtItsFrontFace()
        {
            yield return Load(CampaignLevels.FirstScene);
            var go=new GameObject("Cover fixture guard");go.transform.position=new Vector3(1000,1.05f,1000);
            var guard=go.AddComponent<GuardEnemy>();guard.enabled=false;
            var target=new GameObject("Cover fixture target");target.transform.position=go.transform.position+Vector3.forward*4;
            Physics.SyncTransforms();Assert.That(guard.CanSee(target.transform),Is.True);
            var crate=new GameObject("Decor_HidingCrate");crate.transform.position=new Vector3(1000,0,1002.7f);HidingCrate.Build(crate.transform);
            Physics.SyncTransforms();Assert.That(guard.CanSee(target.transform),Is.False,"Solid cover must stop actual enemy detection.");
            var box=crate.GetComponent<BoxCollider>();Assert.That(box.isTrigger,Is.False);Assert.That(box.bounds.size.y,Is.GreaterThan(2));
            Assert.That(crate.GetComponentsInChildren<Collider>().Count(c=>c.enabled),Is.EqualTo(1));
            var view=go.AddComponent<VisionConeView>();
            typeof(VisionConeView).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(view,null);
            var cone=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Single(f=>f.name=="Cover fixture guard Vision Cone");
            var vertex=cone.sharedMesh.vertices[49];
            Assert.That(cone.transform.TransformPoint(vertex).z,Is.LessThanOrEqualTo(box.bounds.min.z+.02f),"Red cone must end at cover rather than continue behind it.");
            Object.DestroyImmediate(crate);Physics.SyncTransforms();Assert.That(guard.CanSee(target.transform),Is.True);
            Object.DestroyImmediate(target);Object.DestroyImmediate(go);
        }
        [UnityTest] public IEnumerator SpiritDeathHidesItsVisualModel()
        {
            yield return Load(CampaignLevels.FirstScene);
            var spirit=Object.FindFirstObjectByType<SpiritController>();spirit.TakeDamage(spirit.MaxHP);
            yield return null;yield return null;Assert.That(spirit.GetComponent<CharacterAvatarView>().IsShowing,Is.False);
        }
    }
}
