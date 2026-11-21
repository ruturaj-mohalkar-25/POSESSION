using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Possession
{
    public sealed class CharacterShadowLighting : MonoBehaviour
    {
        private static readonly HashSet<CharacterShadowLighting> owners=new();
        private static ShadowQuality previousQuality;
        private static ShadowResolution previousResolution;
        private static float previousDistance;
        private Light key;
        private Quaternion rotation;
        private LightShadows previousShadows;
        private float strength,bias,normalBias;
        private bool configured;
        private readonly Dictionary<Renderer,ShadowCastingMode> casters=new();
        public static void Install(Scene scene)
        {
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<CharacterShadowLighting>()!=null)return;
            var go=new GameObject("F soft character lighting");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<CharacterShadowLighting>().Configure();
        }
        private void Awake()=>Configure();
        private void OnEnable()=>Configure();
        public void Configure()
        {
            if(configured)return;configured=true;
            if(owners.Count==0){previousQuality=QualitySettings.shadows;previousResolution=QualitySettings.shadowResolution;previousDistance=QualitySettings.shadowDistance;}
            owners.Add(this);QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=65;
            foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var l in root.GetComponentsInChildren<Light>())
                if(l.type==LightType.Directional && (key==null||l.intensity>key.intensity))key=l;
            if(key!=null)
            {
                rotation=key.transform.rotation;previousShadows=key.shadows;strength=key.shadowStrength;bias=key.shadowBias;normalBias=key.shadowNormalBias;
                key.transform.rotation=Quaternion.Euler(62,40,0);key.shadows=LightShadows.Soft;key.shadowStrength=.72f;key.shadowBias=.025f;key.shadowNormalBias=.15f;
            }
            RefreshCasters();
        }
        private void RefreshCasters()
        {
            foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                foreach(var r in enemy.GetComponentsInChildren<MeshRenderer>(true))
                    if(r.sharedMaterial!=null && r.sharedMaterial.renderQueue<2500 && !casters.ContainsKey(r))
                    {casters[r]=r.shadowCastingMode;r.shadowCastingMode=ShadowCastingMode.On;}
        }
        private float nextScan;
        private void Update()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.5f;
            CharacterPresentationInstaller.Install(gameObject.scene);RefreshCasters();
        }
        private void Release()
        {
            if(!configured)return;configured=false;owners.Remove(this);
            if(owners.Count==0){QualitySettings.shadows=previousQuality;QualitySettings.shadowResolution=previousResolution;QualitySettings.shadowDistance=previousDistance;}
            if(key!=null){key.transform.rotation=rotation;key.shadows=previousShadows;key.shadowStrength=strength;key.shadowBias=bias;key.shadowNormalBias=normalBias;}
            foreach(var pair in casters)if(pair.Key!=null)pair.Key.shadowCastingMode=pair.Value;casters.Clear();
        }
        private void OnDisable()=>Release();
        private void OnDestroy()=>Release();
    }
}
