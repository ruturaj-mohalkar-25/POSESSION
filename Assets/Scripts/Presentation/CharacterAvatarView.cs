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
        private readonly List<(Transform part, Vector3 rest, Quaternion rotation, int side)> gaitParts = new();
        private Vector3 previousPosition;
        private Vector3 gaitDirection=Vector3.back;
        private float gaitPhase, gaitWeight;
        public float GaitPhase => gaitPhase;
        public float GaitWeight => gaitWeight;
        private bool oldActive, oldForceOff;
        public Transform Model => model;
        public bool IsShowing => model != null && model.gameObject.activeSelf;
        private void Awake() => Build();
        public void Build()
        {
            if(model != null)return;
            body=GetComponent<Renderer>();if(body==null)return;
            enemy=GetComponent<EnemyBase>();spirit=GetComponent<SpiritController>();
            if(spirit==null && enemy==null)return;
            var inherited=transform.Find("Character Avatar");
            bool cloned=inherited!=null;
            if(inherited!=null){inherited.gameObject.SetActive(false);Release(inherited.gameObject);}
            oldForceOff=cloned?false:body.forceRenderingOff;
            oldModel=transform.Find("P5_Model");if(oldModel!=null){oldActive=cloned||oldModel.gameObject.activeSelf;oldModel.gameObject.SetActive(false);}
            navy=Mat(new Color(.055f,.12f,.23f));black=Mat(new Color(.035f,.045f,.06f));gold=Mat(new Color(.88f,.65f,.16f));
            white=Mat(new Color(.91f,.94f,.93f));skin=Mat(new Color(.67f,.43f,.29f));teal=Mat(new Color(.08f,.45f,.43f));
            uniformNavy=Mat(enemy is HeavyEnemy?new Color(.95f,.39f,.055f):new Color(.055f,.12f,.23f));
            uniform.Add(uniformNavy);
            model=BuildModel(enemy is GuardEnemy);model.name="Character Avatar";
            var mesh=GetComponent<MeshFilter>();var bounds=mesh!=null&&mesh.sharedMesh!=null?mesh.sharedMesh.bounds:new Bounds(Vector3.zero,new Vector3(1,2,1));
            model.localPosition=new Vector3(0,bounds.min.y,0);model.localScale=Vector3.one*(bounds.size.y/2.65f);
            GetComponent<EnemyContactDamage>()?.RefreshPresentationRenderers();
            previousPosition=transform.position;
            Refresh();
        }
        private void LateUpdate(){Refresh();AdvanceGait(Time.deltaTime);}
        // Driven by travelled distance, so blocked input never produces walking in place.
        public void AdvanceGait(float deltaTime)
        {
            Vector3 travel=transform.position-previousPosition;previousPosition=transform.position;travel.y=0;
            if(model==null||deltaTime<=0f)return;
            bool moving=travel.magnitude>.0005f&&travel.magnitude<3f&&IsShowing;
            gaitWeight=Mathf.MoveTowards(gaitWeight,moving?1f:0f,deltaTime*9f);
            if(moving)gaitPhase+=travel.magnitude*8f;
            if(moving)gaitDirection=model.InverseTransformDirection(travel.normalized);
            Vector3 local=gaitDirection;local.y=0;local.Normalize();
            foreach(var item in gaitParts)
            {
                float wave=Mathf.Sin(gaitPhase+(item.side<0?Mathf.PI:0))*gaitWeight;
                float lift=Mathf.Max(0,wave)*.17f;
                item.part.localPosition=item.rest+local*(wave*.43f)+Vector3.up*lift;
                item.part.localRotation=item.part.name=="Boot"?item.rotation:Quaternion.AngleAxis(wave*24f,Vector3.Cross(Vector3.up,local))*item.rotation;
            }
            if(spirit!=null&&moving)model.rotation=Quaternion.LookRotation(travel)*Quaternion.Euler(0,180,0);
        }
        public void Refresh()
        {
            if(model==null||body==null)return;
            body.forceRenderingOff=true;
            if(oldModel!=null)oldModel.gameObject.SetActive(false);
            if(enemy!=null)
            {
                foreach(Transform old in transform)
                    if(old.name=="Guard Cap"||old.name=="Guard Cap Brim"||old.name=="Guard Badge"||old.name=="Heavy Left Shoulder"||old.name=="Heavy Right Shoulder"||old.name=="Heavy Chest Plate"||old.name=="Heavy Chest Mark"||old.name=="Heavy Belt"
                        || (enemy is EngineerEnemy && (old.name=="Equipment helmet"||old.name=="Equipment toolkit")))
                    {if(!legacyPieces.ContainsKey(old.gameObject))legacyPieces[old.gameObject]=old.gameObject.activeSelf;old.gameObject.SetActive(false);}
                // Preserve the established white-body possession cue; equipment keeps its own colours.
                uniformNavy.color=enemy.IsPossessed?Color.white:enemy is HeavyEnemy?new Color(.95f,.39f,.055f):new Color(.055f,.12f,.23f);
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
            previousPosition=transform.position;
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
            var mesh=Resources.GetBuiltinResource<Mesh>(type==PrimitiveType.Capsule?"Capsule.fbx":type==PrimitiveType.Cylinder?"Cylinder.fbx":type==PrimitiveType.Sphere?"Sphere.fbx":"Cube.fbx");
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            // Builtin FBX meshes are not sized like CreatePrimitive (sphere/capsule are 2x).
            var expected=type==PrimitiveType.Capsule||type==PrimitiveType.Cylinder?new Vector3(1,2,1):Vector3.one;
            var actual=mesh.bounds.size;
            go.transform.localScale=Vector3.Scale(size,new Vector3(expected.x/actual.x,expected.y/actual.y,expected.z/actual.z));
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return go;
        }
        private Transform BuildModel(bool officer)
        {
            var root=new GameObject(officer?"Police avatar proposal":"Player avatar proposal").transform;root.SetParent(transform,false);
            bool heavy=enemy is HeavyEnemy;
            bool engineer=enemy is EngineerEnemy;
            Material cloth=enemy!=null?uniformNavy:white;
            Part(root,"Torso",PrimitiveType.Capsule,new Vector3(0,1.38f,0),new Vector3(.85f,.57f,.55f),cloth);
            for(int side=-1;side<=1;side+=2)
            {
                var leg=Part(root,"Trouser leg",PrimitiveType.Capsule,new Vector3(side*.25f,.48f,0),new Vector3(.31f,.42f,.35f),cloth).transform;
                var boot=Part(root,"Boot",PrimitiveType.Cube,new Vector3(side*.25f,.105f,-.16f),new Vector3(.35f,.2f,.65f),enemy!=null?black:teal).transform;
                gaitParts.Add((leg,leg.localPosition,leg.localRotation,side));gaitParts.Add((boot,boot.localPosition,boot.localRotation,side));
                var sleeve=Part(root,"Sleeve",PrimitiveType.Capsule,new Vector3(side*.56f,1.43f,0),new Vector3(.29f,.35f,.34f),cloth);
                sleeve.transform.localRotation=Quaternion.Euler(0,0,side*12);
                Part(root,"Hand",PrimitiveType.Sphere,new Vector3(side*.65f,1.1f,-.04f),Vector3.one*.25f,enemy!=null?skin:white);
            }
            Part(root,"Head",PrimitiveType.Sphere,new Vector3(0,2.15f,0),new Vector3(.68f,.76f,.64f),enemy!=null?skin:white);
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
            else if(heavy || engineer)
            {
                Part(root,"Hardhat crown",PrimitiveType.Sphere,new Vector3(0,2.43f,0),new Vector3(.79f,.43f,.76f),gold);
                Part(root,"Hardhat rim",PrimitiveType.Cylinder,new Vector3(0,2.35f,0),new Vector3(.91f,.035f,.87f),gold);
                if(heavy)
                {
                    Part(root,"Hammer handle",PrimitiveType.Cylinder,new Vector3(.81f,1.22f,-.12f),new Vector3(.09f,.51f,.09f),black);
                    Part(root,"Hammer head",PrimitiveType.Cube,new Vector3(.81f,1.76f,-.12f),new Vector3(.56f,.29f,.35f),navy);
                }
                else
                {
                    Part(root,"Engineer toolkit",PrimitiveType.Cube,new Vector3(.6f,1.04f,-.02f),new Vector3(.3f,.34f,.36f),gold);
                    Part(root,"Engineer toolkit handle",PrimitiveType.Cube,new Vector3(.6f,1.26f,-.02f),new Vector3(.17f,.09f,.12f),black);
                }
                Part(root,"Work belt",PrimitiveType.Cube,new Vector3(0,1.06f,0),new Vector3(.86f,.16f,.6f),black);
                if(heavy)foreach(int side in new[]{-1,1})Part(root,"Reflective vest stripe",PrimitiveType.Cube,new Vector3(side*.23f,1.48f,-.29f),new Vector3(.085f,.64f,.04f),gold);
            }
            else
            {
                Part(root,"Face panel",PrimitiveType.Sphere,new Vector3(0,2.13f,-.24f),new Vector3(.48f,.4f,.2f),Mat(new Color(.14f,.22f,.25f)));
                Part(root,"Teal collar",PrimitiveType.Cylinder,new Vector3(0,1.89f,0),new Vector3(.58f,.07f,.53f),teal);
                Part(root,"Chest identity patch",PrimitiveType.Cube,new Vector3(0,1.5f,-.29f),new Vector3(.18f,.16f,.04f),teal);
            }
            for(int side=-1;side<=1;side+=2)Part(root,"Eye",PrimitiveType.Sphere,new Vector3(side*.13f,2.17f,-.32f),new Vector3(.065f,.085f,.04f),enemy!=null?black:white);
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
                foreach(var guard in root.GetComponentsInChildren<EnemyBase>(true))
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
