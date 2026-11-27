using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Possession.Tests
{
    public sealed class AvatarWardrobeTests
    {
        private const string Key="Possession.Avatar.Outfit";
        private bool existed;private int saved;
        [SetUp] public void Setup(){existed=PlayerPrefs.HasKey(Key);saved=PlayerPrefs.GetInt(Key);PlayerPrefs.DeleteKey(Key);}
        [UnityTearDown] public IEnumerator Cleanup(){if(existed)PlayerPrefs.SetInt(Key,saved);else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();Time.timeScale=1;var old=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("AvatarCleanup"));yield return SceneManager.UnloadSceneAsync(old);EventBus.Clear();}
        private static Button Button(string name)=>Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).SingleOrDefault(b=>b.name==name);
        [UnityTest] public IEnumerator EquippingUSCVarsityPersistsAndDressesTheActualPlayer()
        {
            EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            var entry=Button("AvatarButton");Assert.That(entry,Is.Not.Null,"Home needs an Avatar entry point.");entry.onClick.Invoke();yield return null;
            var page=GameObject.Find("AvatarPage");Assert.That(page,Is.Not.Null);
            Assert.That(page.GetComponentsInChildren<RawImage>().Length,Is.EqualTo(5));
            Assert.That(page.GetComponentsInChildren<RawImage>().All(r=>r.texture!=null),Is.True);
            Button("Outfit_1").onClick.Invoke();
            Assert.That(PlayerPrefs.GetInt(Key,0),Is.Zero,"Preview must not save before Equip.");
            Button("EquipAvatar").onClick.Invoke();Assert.That(PlayerPrefs.GetInt(Key),Is.EqualTo(1));
            yield return SceneManager.LoadSceneAsync(CampaignLevels.FirstScene);yield return null;
            foreach(var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))Object.DestroyImmediate(reporter.gameObject);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var view=spirit.GetComponent<CharacterAvatarView>();
            Assert.That(view.Model.Find("USC Varsity").gameObject.activeSelf,Is.True);
            var color=view.Model.Find("Torso").GetComponent<Renderer>().sharedMaterial.color;Assert.That(color.r,Is.GreaterThan(color.b*2));
            Assert.That(spirit.GetComponent<Collider>().enabled,Is.True);Assert.That(spirit.MoveSpeed,Is.EqualTo(8));
            foreach(var guard in Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None))Assert.That(guard.GetComponent<CharacterAvatarView>().Model.Find("USC Varsity"),Is.Null,"NPC uniforms remain distinct.");
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;Button("AvatarButton").onClick.Invoke();yield return null;
            Assert.That(Button("EquipAvatar").interactable,Is.False,"Saved choice reopens as equipped.");
        }
        [UnityTest] public IEnumerator EveryOutfitAppliesAcrossCampaignStagesAndSurvivesEjection()
        {
            string[] groups={"Classic outfit","USC Varsity","Cardinal Hoodie","Sport Jacket"};
            for(int i=0;i<4;i++)
            {
                PlayerAvatarSelection.Equip(i);EventBus.Clear();Time.timeScale=1;
                yield return SceneManager.LoadSceneAsync(CampaignLevels.PlayableSceneNames[i]);yield return null;
                foreach(var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))Object.DestroyImmediate(reporter.gameObject);
                foreach(var tracker in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None))tracker.PersistReports=false;
                foreach(var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))enemy.enabled=false;
                foreach(var damage in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None))damage.enabled=false;
                var spirit=Object.FindFirstObjectByType<SpiritController>();var view=spirit.GetComponent<CharacterAvatarView>();
                Assert.That(view.EquippedOutfit,Is.EqualTo(i));Assert.That(view.Model.Find(groups[i]).gameObject.activeSelf,Is.True);
                var pc=spirit.GetComponent<PossessionController>();while(spirit.GetComponent<PossessionCharge>().TryCollect()){}
                var guard=Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).First();bool entered=false;
                foreach(var direction in new[]{-guard.transform.forward,Vector3.left,Vector3.right,Vector3.back})
                {spirit.RestoreAt(guard.transform.position+direction*2.5f);Physics.SyncTransforms();if(pc.TryPossess(guard)){entered=true;break;}}
                Assert.That(entered,Is.True,"Fixture must possess through the real eligibility rules.");
                Assert.That(pc.TryEject(false),Is.True);yield return null;
                Assert.That(view.IsShowing,Is.True);Assert.That(view.EquippedOutfit,Is.EqualTo(i));Assert.That(view.Model.Find(groups[i]).gameObject.activeSelf,Is.True);
                Assert.That(spirit.GetComponent<Collider>().enabled,Is.True);
            }
        }
        [UnityTest] public IEnumerator BackDiscardsUnappliedChangesAndInvalidPreferencesUseClassic()
        {
            PlayerPrefs.SetInt(Key,999);EventBus.Clear();Time.timeScale=1;yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            var entry=Button("AvatarButton");Assert.That(entry,Is.Not.Null);entry.onClick.Invoke();yield return null;
            Button("Outfit_2").onClick.Invoke();Button("AvatarBack").onClick.Invoke();entry.onClick.Invoke();yield return null;
            Assert.That(Object.FindFirstObjectByType<USCAvatarWardrobe>().Draft,Is.Zero);
            Assert.That(Button("Outfit_0").transform.Find("Selected check").gameObject.activeSelf,Is.True);
        }
    }
}
