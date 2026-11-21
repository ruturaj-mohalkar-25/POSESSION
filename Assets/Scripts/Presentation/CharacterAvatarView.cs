using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    [DefaultExecutionOrder(350), DisallowMultipleComponent]
    public sealed class CharacterAvatarView : MonoBehaviour
    {
        private readonly List<Material> owned = new();
        private readonly List<Material> uniform = new();
        private Material navy,black,gold,white,skin,teal,uniformNavy;
        private readonly Dictionary<GameObject,bool> legacyPieces=new();
        private bool warningHidden;
        public bool WarningHidden => warningHidden;
        public bool OriginalBodyRenderingOff => oldForceOff;
        private Renderer body;
        private EnemyBase enemy;
        private SpiritController spirit;
        private Transform model, oldModel;
        private bool oldActive, oldForceOff;
        public Transform Model => model;
        public bool IsShowing => model != null && model.gameObject.activeSelf;
        private void Awake() => Build();
        public void Build()
        {
            if(model != null)return;
            body=GetComponent<Renderer>();if(body==null)return;
            enemy=GetComponent<EnemyBase>();spirit=GetComponent<SpiritController>();
            if(spirit==null && !(enemy is GuardEnemy))return;
            var inherited=transform.Find("Character Avatar");
            bool cloned=inherited!=null;
            if(inherited!=null){inherited.gameObject.SetActive(false);Release(inherited.gameObject);}
            oldForceOff=cloned?false:body.forceRenderingOff;
            oldModel=transform.Find("P5_Model");if(oldModel!=null){oldActive=cloned||oldModel.gameObject.activeSelf;oldModel.gameObject.SetActive(false);}
            navy=Mat(new Color(.055f,.12f,.23f));black=Mat(new Color(.035f,.045f,.06f));gold=Mat(new Color(.88f,.65f,.16f));
            white=Mat(new Color(.91f,.94f,.93f));skin=Mat(new Color(.67f,.43f,.29f));teal=Mat(new Color(.08f,.45f,.43f));
            uniformNavy=Mat(new Color(.055f,.12f,.23f));
            uniform.Add(uniformNavy);
            model=BuildModel(enemy is GuardEnemy);model.name="Character Avatar";
            var mesh=GetComponent<MeshFilter>();var bounds=mesh!=null&&mesh.sharedMesh!=null?mesh.sharedMesh.bounds:new Bounds(Vector3.zero,new Vector3(1,2,1));
            model.localPosition=new Vector3(0,bounds.min.y,0);model.localScale=Vector3.one*(bounds.size.y/2.65f);
            GetComponent<EnemyContactDamage>()?.RefreshPresentationRenderers();
            Refresh();
        }
        private void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(model==null||body==null)return;
            body.forceRenderingOff=true;
            if(oldModel!=null)oldModel.gameObject.SetActive(false);
            if(enemy is GuardEnemy)
            {
                foreach(Transform old in transform)
                    if(old.name=="Guard Cap"||old.name=="Guard Cap Brim"||old.name=="Guard Badge")
                    {if(!legacyPieces.ContainsKey(old.gameObject))legacyPieces[old.gameObject]=old.gameObject.activeSelf;old.gameObject.SetActive(false);}
                // Preserve the established white-body possession cue; equipment keeps its own colours.
                uniformNavy.color=enemy.IsPossessed?Color.white:new Color(.055f,.12f,.23f);
                if(!enemy.IsPossessed)model.localRotation=Quaternion.Euler(0,180,0);
                if(enemy.IsPossessed && Time.timeScale>0f && Camera.main!=null)
                {
                    var ray=Camera.main.ScreenPointToRay(Input.mousePosition);
                    if(new Plane(Vector3.up,transform.position).Raycast(ray,out float distance))
                    {var dir=ray.GetPoint(distance)-transform.position;dir.y=0;if(dir.sqrMagnitude>.01f)model.rotation=Quaternion.LookRotation(dir)*Quaternion.Euler(0,180,0);}
                }
            }
            bool alive=!Application.isPlaying || (enemy!=null?enemy.CurrentHP>0:spirit.CurrentHP>0);
            model.gameObject.SetActive(isActiveAndEnabled && body.enabled && gameObject.activeInHierarchy && alive);
        }
        private readonly Dictionary<Renderer,bool> blinkOriginal=new();
        public static bool IsBodyPiece(Renderer r) => r.name=="Torso"||r.name=="Trouser leg"||r.name=="Sleeve"||r.name=="Head"||r.name=="Hand";
        public void SetWarningHidden(bool hidden)
        {
            warningHidden=hidden;if(model==null)return;
            if(hidden)
            {
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))if(IsBodyPiece(r))
                {if(!blinkOriginal.ContainsKey(r))blinkOriginal[r]=r.forceRenderingOff;r.forceRenderingOff=true;}
            }
            else
            {foreach(var pair in blinkOriginal)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;blinkOriginal.Clear();}
        }
        private void OnPossessed(EnemyBase host,PlayerState state){Refresh();}
        private void OnEjected(EnemyBase host,bool forced){Refresh();}
        private void OnEnable()
        {
            EventBus.Possessed+=OnPossessed;EventBus.Ejected+=OnEjected;
            if(model!=null)Refresh();
        }
        private void OnDisable()
        {
            EventBus.Possessed-=OnPossessed;EventBus.Ejected-=OnEjected;SetWarningHidden(false);
            if(model!=null)model.gameObject.SetActive(false);
            if(body!=null)body.forceRenderingOff=oldForceOff;
            if(oldModel!=null)oldModel.gameObject.SetActive(oldActive);
            foreach(var pair in legacyPieces)if(pair.Key!=null)pair.Key.SetActive(pair.Value);
        }
        private void OnDestroy()
        {
            if(body!=null)body.forceRenderingOff=oldForceOff;
            if(oldModel!=null)oldModel.gameObject.SetActive(oldActive);
            if(model!=null)Release(model.gameObject);
            foreach(var m in owned)if(m!=null)Release(m);
        }
        private static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private Material Mat(Color color){var m=RuntimeMaterials.Lit(color);owned.Add(m);return m;}
        private GameObject Part(Transform root,string name,PrimitiveType type,Vector3 at,Vector3 size,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=2;go.transform.SetParent(root,false);
            go.transform.localPosition=at;go.transform.localScale=size;
            go.GetComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>(type==PrimitiveType.Capsule?"Capsule.fbx":type==PrimitiveType.Cylinder?"Cylinder.fbx":type==PrimitiveType.Sphere?"Sphere.fbx":"Cube.fbx");
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return go;
        }
        private Transform BuildModel(bool officer)
        {
            var root=new GameObject(officer?"Police avatar proposal":"Player avatar proposal").transform;root.SetParent(transform,false);
            Material cloth=officer?uniformNavy:white;
            Part(root,"Torso",PrimitiveType.Capsule,new Vector3(0,1.38f,0),new Vector3(.85f,.57f,.55f),cloth);
            for(int side=-1;side<=1;side+=2)
            {
                Part(root,"Trouser leg",PrimitiveType.Capsule,new Vector3(side*.23f,.48f,0),new Vector3(.31f,.42f,.35f),cloth);
                Part(root,"Boot",PrimitiveType.Cube,new Vector3(side*.23f,.105f,-.1f),new Vector3(.34f,.2f,.53f),officer?black:teal);
                var sleeve=Part(root,"Sleeve",PrimitiveType.Capsule,new Vector3(side*.56f,1.43f,0),new Vector3(.29f,.35f,.34f),cloth);
                sleeve.transform.localRotation=Quaternion.Euler(0,0,side*12);
                Part(root,"Hand",PrimitiveType.Sphere,new Vector3(side*.65f,1.1f,-.04f),Vector3.one*.25f,officer?skin:white);
            }
            Part(root,"Head",PrimitiveType.Sphere,new Vector3(0,2.15f,0),new Vector3(.68f,.76f,.64f),officer?skin:white);
            if(officer)
            {
                Part(root,"Uniform belt",PrimitiveType.Cube,new Vector3(0,1.05f,0),new Vector3(.82f,.13f,.57f),black);
                Part(root,"Belt buckle",PrimitiveType.Cube,new Vector3(0,1.05f,-.3f),new Vector3(.17f,.13f,.04f),gold);
                Part(root,"Gold chest badge",PrimitiveType.Cube,new Vector3(-.19f,1.65f,-.28f),new Vector3(.13f,.19f,.045f),gold);
                Part(root,"Chest radio",PrimitiveType.Cube,new Vector3(.2f,1.62f,-.29f),new Vector3(.14f,.23f,.07f),black);
                Part(root,"Cap crown",PrimitiveType.Cylinder,new Vector3(0,2.49f,0),new Vector3(.77f,.13f,.73f),navy);
                Part(root,"Cap band",PrimitiveType.Cylinder,new Vector3(0,2.4f,0),new Vector3(.79f,.04f,.75f),black);
                Part(root,"Cap visor",PrimitiveType.Cube,new Vector3(0,2.4f,-.39f),new Vector3(.67f,.065f,.45f),navy);
                Part(root,"Cap badge",PrimitiveType.Cube,new Vector3(0,2.49f,-.37f),new Vector3(.15f,.16f,.04f),gold);
                Part(root,"Sidearm slide",PrimitiveType.Cube,new Vector3(.68f,1.23f,-.36f),new Vector3(.15f,.17f,.6f),black);
                var grip=Part(root,"Sidearm grip",PrimitiveType.Cube,new Vector3(.68f,1.08f,-.2f),new Vector3(.13f,.28f,.17f),black);grip.transform.localRotation=Quaternion.Euler(-12,0,0);
                Part(root,"Sidearm muzzle",PrimitiveType.Cube,new Vector3(.68f,1.23f,-.67f),new Vector3(.11f,.11f,.025f),navy);
            }
            else
            {
                Part(root,"Face panel",PrimitiveType.Sphere,new Vector3(0,2.13f,-.24f),new Vector3(.48f,.4f,.2f),Mat(new Color(.14f,.22f,.25f)));
                Part(root,"Teal collar",PrimitiveType.Cylinder,new Vector3(0,1.89f,0),new Vector3(.58f,.07f,.53f),teal);
                Part(root,"Chest identity patch",PrimitiveType.Cube,new Vector3(0,1.5f,-.29f),new Vector3(.18f,.16f,.04f),teal);
            }
            for(int side=-1;side<=1;side+=2)Part(root,"Eye",PrimitiveType.Sphere,new Vector3(side*.13f,2.17f,-.32f),new Vector3(.065f,.085f,.04f),officer?black:white);
            return root;
        }
    }
    public static class CharacterPresentationInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void First()=>Install(SceneManager.GetActiveScene());
        private static void Loaded(Scene scene,LoadSceneMode mode)=>Install(scene);
        public static void Install(Scene scene)
        {
            if(!scene.IsValid()||!scene.isLoaded)return;
            bool actors=false;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var spirit in root.GetComponentsInChildren<SpiritController>(true))
                {
                    actors=true;
                    if(spirit.GetComponent<PlayerIdentityView>()==null)spirit.gameObject.AddComponent<PlayerIdentityView>();
                    if(spirit.GetComponent<CharacterAvatarView>()==null)spirit.gameObject.AddComponent<CharacterAvatarView>().Build();
                }
                foreach(var guard in root.GetComponentsInChildren<GuardEnemy>(true))
                    if(guard.GetComponent<CharacterAvatarView>()==null)guard.gameObject.AddComponent<CharacterAvatarView>().Build();
            }
            if(actors)
            {
                CharacterShadowLighting.Install(scene);
                bool found=false;
                foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<GoldContactShadows>()!=null)found=true;
                if(!found){var go=new GameObject("Character contact shadows");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<GoldContactShadows>().Build(scene);}
            }
        }
    }
}
