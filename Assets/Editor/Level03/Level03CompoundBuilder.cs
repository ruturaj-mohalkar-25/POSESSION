using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Possession.Editor
{
    public static class Level03CompoundBuilder
    {
        const string ScenePath="Assets/Scenes/Level_03.unity";
        const float Unit=Level03Director.LayoutUnit, Height=Level03Director.FloorHeight;
        [Serializable] public class Layout { public Floor[] floors; }
        [Serializable] public class Floor { public string name;public float height;public Rect[] rooms,halls,cover;public Actor[] actors;public Pickup[] pickups;public Wire[] wires; }
        [Serializable] public class Rect {public float x,y,width,height;}
        [Serializable] public class Actor {public string kind,wing;public float x,y;public bool enabled;}
        [Serializable] public class Pickup {public string name;public float x,y;public bool reforms;}
        [Serializable] public class Wire {public string wing;public Vector2[] points;}
        static GameObject targetStyle,doorStyle,wallStyle,guardStyle,heavyStyle;
        static Material floorMat,wallMat,coverMat,chargeMat,wireMat;
        public static Vector3 Map(float x,float y,float floor=0,float lift=1.15f)=>new((x-18f)*Unit,floor+lift,(14f-y)*Unit);
        [MenuItem("Possession/Level 3/Build Alarm Compound")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))AssetDatabase.CopyAsset("Assets/Scenes/Level_01.unity",ScenePath);
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var spirit=Object.FindFirstObjectByType<SpiritController>();var manager=Object.FindFirstObjectByType<GameManager>();var camera=Camera.main;
            guardStyle=Object.Instantiate(Object.FindFirstObjectByType<GuardEnemy>().gameObject);guardStyle.SetActive(false);
            heavyStyle=Object.Instantiate(Object.FindFirstObjectByType<HeavyEnemy>().gameObject);heavyStyle.SetActive(false);
            var style=EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity",OpenSceneMode.Additive);var roots=style.GetRootGameObjects();
            targetStyle=Object.Instantiate(roots.SelectMany(r=>r.GetComponentsInChildren<Target>(true)).First().gameObject);
            doorStyle=Object.Instantiate(roots.SelectMany(r=>r.GetComponentsInChildren<Door>(true)).First(d=>d.GetComponent<LevelExit>()==null).gameObject);
            wallStyle=Object.Instantiate(roots.SelectMany(r=>r.GetComponentsInChildren<BreakableWall>(true)).First().gameObject);
            var styles=new[]{guardStyle,heavyStyle,targetStyle,doorStyle,wallStyle};
            foreach(var item in new[]{targetStyle,doorStyle,wallStyle}){SceneManager.MoveGameObjectToScene(item,scene);item.SetActive(false);}
            EditorSceneManager.CloseScene(style,true);SceneManager.SetActiveScene(scene);
            foreach(var root in scene.GetRootGameObjects())
            {
                if(styles.Contains(root)||root.GetComponent<GameManager>()!=null||root.GetComponent<Camera>()!=null||root.GetComponent<SpiritController>()!=null||root.GetComponentInChildren<HUDManager>(true)!=null||root.GetComponent<EventSystem>()!=null)continue;
                Object.DestroyImmediate(root);
            }
            foreach(var c in manager.GetComponents<Level03Director>())Object.DestroyImmediate(c);
            foreach(var c in manager.GetComponents<Level03Circuit>())Object.DestroyImmediate(c);
            foreach(var c in spirit.GetComponents<PossessionCharge>())Object.DestroyImmediate(c);
            spirit.gameObject.AddComponent<PossessionCharge>();spirit.transform.position=Map(4,24);
            LevelBuildKit.BuildLighting();RenderSettings.ambientLight=new Color(.34f,.38f,.49f);
            var moon=Object.FindFirstObjectByType<Light>();moon.color=new Color(.6f,.73f,1f);moon.intensity=.55f;
            camera.backgroundColor=new Color(.025f,.035f,.055f);camera.fieldOfView=45;camera.transform.rotation=Quaternion.Euler(57,0,0);camera.transform.position=spirit.transform.position+new Vector3(0,17,-11);
            Set(camera.GetComponent<CameraFollow>(),"target",spirit.transform);Set(camera.GetComponent<CameraFollow>(),"offset",new Vector3(0,17,-11));
            if(camera.GetComponent<Level03FloorVisibility>()==null)camera.gameObject.AddComponent<Level03FloorVisibility>();
            floorMat=Mat("Slate",new Color(.34f,.37f,.43f));wallMat=Mat("NightStone",new Color(.31f,.36f,.46f));coverMat=Mat("Cargo",new Color(.26f,.24f,.22f));chargeMat=Mat("Charge",new Color(.35f,1f,.72f),true);wireMat=Mat("Wire",new Color(.2f,.6f,1f),true);
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Level03/CompoundLayout.json"));
            var world=new GameObject("Level03_AlarmCompound").transform;
            var circuits=new Dictionary<string,List<Renderer>>{{"west",new()},{"east",new()}};
            foreach(var data in layout.floors)
            {
                var root=new GameObject(data.name=="ground"?"GroundFloor":"UpperFloor").transform;root.SetParent(world);
                Geometry(root,data);
                for(int i=0;i<data.rooms.Length;i++)
                {var r=data.rooms[i];var zone=new GameObject(i<6?(data.height==0?"G":"U")+(i+1):"D"+(i-5+(data.height==0?0:2)));zone.transform.SetParent(root);zone.transform.position=Map(r.x+r.width/2,r.y+r.height/2,data.height,0);}
                for(int i=0;i<data.cover.Length;i++){var r=data.cover[i];Cube(root,"Cover_"+i,Map(r.x+r.width/2,r.y+r.height/2,data.height,1f),new Vector3(r.width*Unit*.72f,2f,r.height*Unit*.72f),coverMat);}
                for(int i=0;i<data.actors.Length;i++)if(data.actors[i].enabled)Enemy(root,data.actors[i],data.height,i);
                foreach(var p in data.pickups)Shard(root,p,data.height);
                foreach(var wire in data.wires)
                    for(int i=1;i<wire.points.Length;i++)
                    {var a=Map(wire.points[i-1].x,wire.points[i-1].y,data.height,.18f);var b=Map(wire.points[i].x,wire.points[i].y,data.height,.18f);var go=Cube(root,"Alarm wire",(a+b)/2,new Vector3(.045f,.025f,Vector3.Distance(a,b)),wireMat);go.transform.rotation=Quaternion.LookRotation(b-a);Object.DestroyImmediate(go.GetComponent<Collider>());circuits[wire.wing].Add(go.GetComponent<Renderer>());}
                // Small lanterns mark decisions without floor instructions or long popups.
                foreach(var h in data.halls.Where((_,i)=>i%2==0))
                {var marker=Cube(root,"Lantern",Map(h.x+.25f,h.y+.25f,data.height,.7f),new Vector3(.18f,.35f,.18f),Mat("Lantern",new Color(.75f,.6f,.26f),true));Object.DestroyImmediate(marker.GetComponent<Collider>());}
            }
            Stair(world,"A",16,3,5,6);Stair(world,"B",29,19,5,6);Stair(world,"C",29,3,5,6);
            var t1=Target(world,"T1",7,7.5f,0);var jw=Target(world,"JW",3,3.5f,0);
            var relay=Target(world,"R",3,3.5f,Height);var je=Target(world,"JE",15,24.5f,Height);var t3=Target(world,"T3",20,20,Height);var se=Target(world,"SE",22,19.5f,Height);
            var pw=Barrier(world,"PW",7,24.5f,Height,1.4f,.5f,1.4f);var pe=Barrier(world,"PE",28,24.5f,Height,1.4f,.5f,1.4f);
            Barrier(world,"B1",31.5f,24.8f,0,5*Unit,.4f,6f);Barrier(world,"B3",31.5f,9.4f,0,5*Unit,.4f,6f);
            var mainGate=Gate(world,"Gate_A",16,9,21,9,0);var maintenanceGate=Gate(world,"Gate_C",29,9,34,9,0);var bridge=Gate(world,"West_Bridge",16,14.5f,19,14.5f,Height);
            var ew=Gate(world,"Extraction_West",25.5f,5,25.5f,8,Height);var es=Gate(world,"Extraction_South",29,11,32,11,Height);var service=Gate(world,"PE_Service",27,23,29,23,Height);
            // Seal the panel pocket; its target-operated entrance is the sole approach.
            Cube(world,"PE_Pocket_Wall",Map(29.25f,24.5f,Height,3f),new Vector3(.3f,6f,3f*Unit),wallMat);
            foreach(var old in manager.GetComponents<Level03RouteMap>())Object.DestroyImmediate(old);
            var map=manager.gameObject.AddComponent<Level03RouteMap>();Set(map,"layout",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Level03/CompoundLayout.json"));
            var director=manager.gameObject.AddComponent<Level03Director>();var west=manager.gameObject.AddComponent<Level03Circuit>();var east=manager.gameObject.AddComponent<Level03Circuit>();
            Set(west,"wing",0);Set(west,"junction",jw);Set(west,"panel",pw);Array(west,"wires",circuits["west"].Cast<Object>().ToArray());
            Set(east,"wing",1);Set(east,"junction",je);Set(east,"panel",pe);Array(east,"wires",circuits["east"].Cast<Object>().ToArray());
            Set(director,"spirit",spirit);Set(director,"mainTarget",t1);Set(director,"relay",relay);Set(director,"exitTarget",t3);Set(director,"serviceTarget",se);
            Set(director,"stairA",mainGate);Set(director,"bridge",bridge);Set(director,"extractionWest",ew);Set(director,"extractionSouth",es);Set(director,"maintenance",maintenanceGate);Set(director,"service",service);Set(director,"west",west);Set(director,"east",east);
            var exit=new GameObject("Extraction_Exit");exit.transform.SetParent(world);exit.transform.position=Map(34.7f,3.5f,Height,1.4f);var trigger=exit.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(2.3f,2.8f,2.3f);exit.AddComponent<Level03Exit>();Cube(exit.transform,"Exit pad",Map(34.7f,3.5f,Height,.18f),new Vector3(2.3f,.15f,2.3f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_Exit.mat"));
            RouteSign(world,"MAIN A ↑",4.5f,17.5f,0,new Color(.3f,.65f,1f));
            RouteSign(world,"SERVICE B →",9,22.5f,0,new Color(1f,.72f,.28f));
            RouteSign(world,"SERVICE B →",23,22.5f,0,new Color(1f,.72f,.28f));
            RouteSign(world,"A ↑",18.5f,10.5f,0,new Color(.3f,.65f,1f));
            RouteSign(world,"B ↑",31.5f,26.5f,0,new Color(1f,.72f,.28f));
            RouteSign(world,"RECORDS →",9,22.5f,Height,new Color(.7f,.85f,1f));
            RouteSign(world,"EXIT ↑",30.5f,15,Height,new Color(.35f,1f,.65f));
            Checkpoint(world,"Main stair shelter",22,6.5f,0,"K2");Checkpoint(world,"Records shelter",14,23,Height,"K5");
            foreach(var item in styles)Object.DestroyImmediate(item);
            FinalizePatrols(world);
            LevelBuildKit.ApplyConfig(LevelConfigAssets.Load("Level_03"),manager,spirit.gameObject);
            foreach(var h in world.GetComponentsInChildren<EnemyBase>()){Set(h,"detectionRange",h is HeavyEnemy?2.8f:3.2f);Set(h,"chaseStopDistance",1.8f);Set(h,"aiSpeedMultiplier",.4f);Set(h,"detectionDelay",2f);Set(h,"requireLineOfSight",true);Set(h,"waypointWaitTime",1.5f);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);LevelBuildKit.RegisterScene(ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("Level 3 Alarm Compound built: two floors, three physical stairs, nine hosts, ten charge sources, two alarm circuits.");
        }
        static void FinalizePatrols(Transform world)
        {
            Physics.SyncTransforms();
            foreach(var enemy in world.GetComponentsInChildren<EnemyBase>())
            {
                Vector3 start=enemy.transform.position;bool clear=false;
                foreach(var offset in new[]{Vector3.zero,Vector3.right*.6f,Vector3.left*.6f,Vector3.forward*.6f,Vector3.back*.6f,Vector3.right*1.1f,Vector3.left*1.1f,Vector3.forward*1.1f,Vector3.back*1.1f})
                {
                    Vector3 point=start+offset;
                    if(Physics.CheckSphere(point,.7f,1,QueryTriggerInteraction.Ignore)||!Physics.Raycast(point,Vector3.down,2f,1,QueryTriggerInteraction.Ignore))continue;
                    start=point;clear=true;break;
                }
                if(!clear)throw new InvalidOperationException("No clear spawn for "+enemy.name);
                enemy.transform.position=start;GameObject.Find("SpawnPoint_"+enemy.name).transform.position=start;
                var so=new SerializedObject(enemy);var route=so.FindProperty("waypoints");var first=(Transform)route.GetArrayElementAtIndex(0).objectReferenceValue;var second=(Transform)route.GetArrayElementAtIndex(1).objectReferenceValue;first.position=start;
                clear=false;
                foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    Vector3 end=start+direction*1.2f;
                    if(Physics.SphereCast(start,.7f,direction,out _,1.2f,1,QueryTriggerInteraction.Ignore)||!Physics.Raycast(end,Vector3.down,2f,1,QueryTriggerInteraction.Ignore))continue;
                    second.position=end;clear=true;break;
                }
                if(!clear)throw new InvalidOperationException("No clear patrol segment for "+enemy.name);
            }
            Physics.SyncTransforms();
        }
        static void Geometry(Transform parent,Floor data)
        {
            var grid=new bool[44,30];
            foreach(var r in data.rooms.Concat(data.halls))for(int x=(int)r.x;x<r.x+r.width;x++)for(int y=(int)r.y;y<r.y+r.height;y++)grid[x+4,y]=true;
            bool Hole(int x,int y)=>data.height>0 && ((x>=16&&x<21&&y>=3&&y<9)||(x>=29&&x<34&&((y>=3&&y<9)||(y>=19&&y<25))));
            for(int y=0;y<30;y++)for(int x=0;x<44;)
            {
                if(!grid[x,y]||Hole(x-4,y)){x++;continue;}int start=x++;while(x<44&&grid[x,y]&&!Hole(x-4,y))x++;
                Cube(parent,"Floor",Map((start+x)/2f-4,y+.5f,data.height,0),new Vector3((x-start)*Unit,.2f,Unit),floorMat);
            }
            for(int y=0;y<30;y++)for(int x=0;x<44;x++)if(grid[x,y])
            {
                if(y==0||!grid[x,y-1])Wall(parent,x-4,y,x-3,y,data.height);
                if(y==29||!grid[x,y+1])Wall(parent,x-4,y+1,x-3,y+1,data.height);
                if(x==0||!grid[x-1,y])Wall(parent,x-4,y,x-4,y+1,data.height);
                if(x==43||!grid[x+1,y])Wall(parent,x-3,y,x-3,y+1,data.height);
            }
        }
        static void Wall(Transform parent,float x1,float y1,float x2,float y2,float height)
        {var a=Map(x1,y1,height,3f);var b=Map(x2,y2,height,3f);Cube(parent,"Wall",(a+b)/2,new Vector3(Mathf.Max(.3f,Mathf.Abs(a.x-b.x)),6,Mathf.Max(.3f,Mathf.Abs(a.z-b.z))),wallMat);}
        static void Stair(Transform parent,string name,float x,float y,float width,float run)
        {
            var go=new GameObject("Stair_"+name);go.transform.SetParent(parent);go.transform.position=Map(x+width/2,y+run/2,0,.1f);
            string path="Assets/Level03/StairRamp.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool newRamp=mesh==null;if(newRamp)mesh=new Mesh();mesh.vertices=new[]{new Vector3(-width*Unit/2,0,-run*Unit/2),new Vector3(width*Unit/2,0,-run*Unit/2),new Vector3(-width*Unit/2,Height,run*Unit/2),new Vector3(width*Unit/2,Height,run*Unit/2)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();if(newRamp)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
            const int steps=24;
            for(int i=0;i<steps;i++){float top=(i+1f)/steps*Height;var step=Cube(go.transform,"Step",go.transform.position+new Vector3(0,top-.15f,-run*Unit/2+(i+.5f)*run*Unit/steps),new Vector3(width*Unit,.3f,run*Unit/steps),floorMat);Object.DestroyImmediate(step.GetComponent<Collider>());}
            string sidePath="Assets/Level03/StairSide.asset";var sideMesh=AssetDatabase.LoadAssetAtPath<Mesh>(sidePath);
            bool newSide=sideMesh==null;if(newSide)sideMesh=new Mesh();
            float railZ=run*Unit/2,railT=.175f;
            // Full-height shaft walls prevent entry from underneath and protect the upper floor openings.
            sideMesh.vertices=new[]{new Vector3(-railT,0,-railZ),new Vector3(railT,0,-railZ),new Vector3(-railT,0,railZ),new Vector3(railT,0,railZ),new Vector3(-railT,Height+6f,-railZ),new Vector3(railT,Height+6f,-railZ),new Vector3(-railT,Height+6f,railZ),new Vector3(railT,Height+6f,railZ)};
            sideMesh.triangles=new[]{0,1,2,1,3,2,4,6,5,5,6,7,0,2,4,2,6,4,1,5,3,3,5,7,0,4,1,1,4,5,2,3,6,3,7,6};sideMesh.RecalculateNormals();sideMesh.RecalculateBounds();
            if(newSide)AssetDatabase.CreateAsset(sideMesh,sidePath);else EditorUtility.SetDirty(sideMesh);
            Cube(go.transform,"Stair backing",go.transform.position+new Vector3(0,Height/2,run*Unit/2+.3f),new Vector3(width*Unit,Height,.3f),wallMat);
            Cube(go.transform,"Stair Wall Upper Guard",go.transform.position+new Vector3(0,Height+3f,-run*Unit/2),new Vector3(width*Unit,6f,.3f),wallMat);
            foreach(float side in new[]{-1f,1f})
            {var rail=new GameObject("Stair Wall");rail.transform.SetParent(go.transform,false);rail.transform.localPosition=Vector3.right*(side*width*Unit/2);rail.AddComponent<MeshFilter>().sharedMesh=sideMesh;rail.AddComponent<MeshRenderer>().sharedMaterial=wallMat;rail.AddComponent<MeshCollider>().sharedMesh=sideMesh;}

        }
        static Target Target(Transform parent,string name,float x,float y,float floor)
        {var go=Object.Instantiate(targetStyle,parent);go.name=name;go.transform.position=Map(x,y,floor,1.6f);go.SetActive(true);Set(go.GetComponent<Target>(),"isHit",false);Sign(go.transform,name,Vector3.up*1.4f);return go.GetComponent<Target>();}
        static BreakableWall Barrier(Transform parent,string name,float x,float y,float floor,float width,float depth,float height)
        {var go=Object.Instantiate(wallStyle,parent);go.name=name;go.transform.position=Map(x,y,floor,height/2);go.transform.localScale=new Vector3(width,height,depth);go.SetActive(true);if(name=="PW"||name=="PE")Sign(go.transform,name,Vector3.up*1.4f);return go.GetComponent<BreakableWall>();}
        static Level03SecurityGate Gate(Transform parent,string name,float x1,float y1,float x2,float y2,float floor)
        {var go=Object.Instantiate(doorStyle,parent);go.name=name;go.transform.position=(Map(x1,y1,floor,3f)+Map(x2,y2,floor,3f))/2;go.transform.rotation=Quaternion.identity;go.transform.localScale=new Vector3(Mathf.Max(.35f,Mathf.Abs(x2-x1)*Unit),6f,Mathf.Max(.35f,Mathf.Abs(y2-y1)*Unit));Array(go.GetComponent<Door>(),"requiredTargets",System.Array.Empty<Object>());Set(go.GetComponent<Door>(),"isExit",false);go.SetActive(true);return go.AddComponent<Level03SecurityGate>();}
        static void Enemy(Transform parent,Actor data,float floor,int index)
        {
            bool heavy=data.kind=="W";string name=(floor==0?"Ground_":"Upper_")+(heavy?"Heavy_":"Guard_")+index;
            var go=Object.Instantiate(heavy?heavyStyle:guardStyle,parent);go.name=name;go.transform.position=Map(data.x,data.y,floor,heavy?1.45f:1.15f);go.SetActive(true);var enemy=go.GetComponent<EnemyBase>();Set(enemy,"moveSpeed",heavy?2.2f:3.25f);Set(enemy,"patrolStartDelay",index*.28f);go.GetComponent<CharacterController>().minMoveDistance=0;
            if(go.GetComponent<HuntedEnemyPressure>()!=null)Object.DestroyImmediate(go.GetComponent<HuntedEnemyPressure>());
            foreach(var old in go.GetComponents<Level03EnemyPressure>())Object.DestroyImmediate(old);
            foreach(var old in go.GetComponents<Level03AlarmLink>())Object.DestroyImmediate(old);
            foreach(var child in go.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Linked antenna").ToArray())Object.DestroyImmediate(child.gameObject);
            go.AddComponent<Level03EnemyPressure>();
            var spawn=new GameObject("SpawnPoint_"+name);spawn.transform.SetParent(parent);spawn.transform.position=go.transform.position;
            // Short local patrols keep each enemy inside its encounter and clear of stair holes.
            Physics.SyncTransforms();
            Vector3 end=go.transform.position;
            foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                if(!Physics.SphereCast(go.transform.position,.7f,direction,out _,1.3f,1,QueryTriggerInteraction.Ignore)) {end=go.transform.position+direction*1.2f;break;}
            var points=new Transform[2];for(int i=0;i<2;i++){var p=new GameObject(name+"_WP"+i);p.transform.SetParent(parent);p.transform.position=i==0?go.transform.position:end;points[i]=p.transform;}Array(enemy,"waypoints",points.Cast<Object>().ToArray());
            var attacks=go.GetComponent<EnemyContactDamage>();Set(attacks,"firstAttackDelay",heavy?2f:3.5f);Set(attacks,"shootInterval",5f);Set(attacks,"smashWindup",2.2f);Set(attacks,"smashCooldown",5f);
            if(data.wing!="")
            {var link=go.AddComponent<Level03AlarmLink>();link.wing=data.wing=="west"?AlarmWing.West:AlarmWing.East;var dot=Cube(go.transform,"Linked antenna",go.transform.position+Vector3.up*1.4f,Vector3.one*.18f,Mat(data.wing+"_Link",data.wing=="west"?new Color(.2f,.6f,1f):new Color(.8f,.4f,1f),true));Object.DestroyImmediate(dot.GetComponent<Collider>());}
        }
        static void Shard(Transform parent,Pickup data,float floor)
        {var go=new GameObject(data.name);go.transform.SetParent(parent);go.transform.position=Map(data.x,data.y,floor,.1f);var sphere=go.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=1.1f;var gem=Cube(go.transform,"Energy shard",go.transform.position+Vector3.up,new Vector3(.45f,.7f,.45f),chargeMat);Object.DestroyImmediate(gem.GetComponent<Collider>());gem.transform.rotation=Quaternion.Euler(35,45,35);var pickup=go.AddComponent<PossessionChargePickup>();Set(pickup,"reforms",data.reforms);Set(pickup,"shard",gem.GetComponent<Renderer>());}
        static void Checkpoint(Transform parent,string label,float x,float y,float floor,string source)
        {var go=new GameObject("Checkpoint_"+label);go.transform.SetParent(parent);go.transform.position=Map(x,y,floor,.15f);var checkpoint=go.AddComponent<Level03Checkpoint>();checkpoint.label=label;checkpoint.source=GameObject.Find(source).GetComponent<PossessionChargePickup>();var beacon=Cube(go.transform,"Beacon",go.transform.position+Vector3.up*.45f,new Vector3(.35f,.9f,.35f),Mat("Checkpoint",new Color(.4f,.75f,1f),true));Object.DestroyImmediate(beacon.GetComponent<Collider>());}
        static void RouteSign(Transform parent,string title,float x,float y,float floor,Color color)
        {
            var post=new GameObject("Route marker");post.transform.SetParent(parent);post.transform.position=Map(x,y,floor,2.8f);
            Sign(post.transform,title,Vector3.zero);var text=post.GetComponentInChildren<TextMesh>();text.color=color;text.transform.localScale=Vector3.one*.13f;
        }
        static void Sign(Transform parent,string title,Vector3 local)
        {var go=new GameObject("Sign");go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.rotation=Quaternion.Euler(57,0,0);go.transform.localScale=new Vector3(1f/parent.lossyScale.x,1f/parent.lossyScale.y,1f/parent.lossyScale.z)*.1f;var text=go.AddComponent<TextMesh>();text.text=title;text.fontSize=40;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.8f,.85f,1f);}
        static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Material mat)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
        static Material Mat(string name,Color color,bool unlit=false)
        {string path="Assets/Level03/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find(unlit?"Sprites/Default":"Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;EditorUtility.SetDirty(mat);return mat;}
        static void Array(Object target,string field,Object[] values)
        {var so=new SerializedObject(target);var array=so.FindProperty(field);array.arraySize=values.Length;for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        static void Set(Object target,string field,object value)
        {var so=new SerializedObject(target);var p=so.FindProperty(field);if(value is float f)p.floatValue=f;else if(value is int i){if(p.propertyType==SerializedPropertyType.Float)p.floatValue=i;else p.intValue=i;}else if(value is bool b)p.boolValue=b;else if(value is Vector3 v)p.vector3Value=v;else p.objectReferenceValue=value as Object;so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
