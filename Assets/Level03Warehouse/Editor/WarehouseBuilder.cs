using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Possession.Warehouse;
using Object=UnityEngine.Object;

namespace Possession.Editor
{
    public static class WarehouseBuilder
    {
        public const string Root="Assets/Level03Warehouse";
        public const string ScenePath=Root+"/Level_03_Warehouse.unity";
        [Serializable] public class Point { public float x,y; }
        [Serializable] public class Area { public float x,y,width,depth;public string name; }
        [Serializable] public class Segment { public float x1,y1,x2,y2;public string name;public bool transparent; }
        [Serializable] public class Gate:Segment { public string key;public Point[] targets; }
        [Serializable] public class Actor:Point { public string kind,name; }
        [Serializable] public class Layout
        {
            public float unitSize,levelSeconds,possessionSeconds;public Point start,exit,health;
            public Area[] rooms,halls,covers;public Point[] sources;public Actor[] actors;
            public Segment[] barriers,partitions;public Gate[] doors;
        }
        private static Layout data;
        private static Material floorMat,wallMat,coverMat,steelMat,chargeMat,glowMat;
        private static GameObject targetTemplate,wallTemplate,guardTemplate,heavyTemplate;
        private static readonly Color[] Palette={new(.2f,.8f,1f),new(1f,.7f,.2f),new(1f,.35f,.6f),new(.65f,.4f,1f)};
        private static float Unit => data != null ? data.unitSize : .65f;
        public static Vector3 Map(float x,float y,float height=1.15f)=>new((x-70f)*Unit,height,(90f-y)*Unit);

        [MenuItem("Possession/Level Design/Level 3 Warehouse/Build and Open")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            data=JsonUtility.FromJson<Layout>(File.ReadAllText(Root+"/Data/warehouse-layout.json"));
            var source=EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity");
            if(!EditorSceneManager.SaveScene(source,ScenePath,true))throw new IOException("Could not copy the shared scene shell.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var spirit=Object.FindFirstObjectByType<SpiritController>();
            var manager=Object.FindFirstObjectByType<GameManager>();
            var hud=Object.FindFirstObjectByType<HUDManager>();
            guardTemplate=Object.Instantiate(Object.FindFirstObjectByType<GuardEnemy>().gameObject);
            heavyTemplate=Object.Instantiate(Object.FindFirstObjectByType<HeavyEnemy>().gameObject);
            targetTemplate=Object.Instantiate(Object.FindFirstObjectByType<Target>().gameObject);
            wallTemplate=Object.Instantiate(Object.FindFirstObjectByType<BreakableWall>().gameObject);
            var templates=new[]{guardTemplate,heavyTemplate,targetTemplate,wallTemplate};
            foreach(var template in templates)template.SetActive(false);
            foreach(var root in scene.GetRootGameObjects())
            {
                if(templates.Contains(root)||root.GetComponent<SpiritController>()!=null||root.GetComponent<GameManager>()!=null
                    ||root.GetComponent<Camera>()!=null||root.GetComponent<Light>()!=null||root.GetComponent<EventSystem>()!=null
                    ||root.GetComponentInChildren<HUDManager>(true)!=null)continue;
                Object.DestroyImmediate(root);
            }
            spirit.transform.position=Map(data.start.x,data.start.y);
            if(spirit.GetComponent<PossessionCharge>()==null)spirit.gameObject.AddComponent<PossessionCharge>();
            if(spirit.GetComponent<WarehouseEjectShield>()==null)spirit.gameObject.AddComponent<WarehouseEjectShield>();
            Set(manager,"levelTime",data.levelSeconds);
            Set(spirit.GetComponent<PossessionController>(),"possessDuration",data.possessionSeconds);
            var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();Set(follow,"target",spirit.transform);
            var offset=new SerializedObject(follow).FindProperty("offset").vector3Value;
            cam.transform.position=spirit.transform.position+offset;
            floorMat=Mat("Floor",new Color(.43f,.46f,.49f));wallMat=Mat("Walls",new Color(.27f,.33f,.4f));
            coverMat=Mat("Cargo",new Color(.38f,.3f,.22f));steelMat=Mat("Steel",new Color(.18f,.23f,.28f));
            chargeMat=Mat("Charge",new Color(.35f,1f,.7f),true);glowMat=Mat("Exit",new Color(.2f,1f,.65f),true);
            var world=new GameObject("Warehouse_Layout").transform;
            var floors=new GameObject("Floors").transform;floors.SetParent(world);
            foreach(var area in data.rooms.Concat(data.halls))
                Cube(floors,string.IsNullOrEmpty(area.name)?"Hall_Floor":area.name.Replace(' ','_')+"_Floor",Map(area.x+area.width/2,area.y+area.depth/2,0),new Vector3(area.width*Unit,.2f,area.depth*Unit),floorMat);
            BoundaryWalls(world);
            foreach(var p in data.partitions)Wall(world,p.x1,p.y1,p.x2,p.y2);
            for(int i=0;i<data.covers.Length;i++)
            {
                var r=data.covers[i];float height=i==data.covers.Length-1?1f:2.2f;
                // Keep cover footprints at their original physical size as rooms grow.
                var cube=Cube(world,"Cover_"+i,Map(r.x+r.width/2,r.y+r.depth/2,height/2+.1f),new Vector3(r.width*.5f,height,r.depth*.5f),coverMat);
                cube.AddComponent<VisibilityOccluder>();
            }
            var doors=new Dictionary<string,Door>();
            for(int i=0;i<data.doors.Length;i++)doors[data.doors[i].key]=BuildDoor(world,data.doors[i],Palette[i]);
            var walls=new Dictionary<string,BreakableWall>();
            foreach(var barrier in data.barriers)walls[barrier.name]=BuildBarrier(world,barrier);
            for(int i=0;i<data.actors.Length;i++)BuildEnemy(world,data.actors[i],i);
            for(int i=0;i<data.sources.Length;i++)BuildSource(world,data.sources[i],i);
            BuildHealthSocket(world);
            var level=BuildExit(world,data.exit);
            Set(level,"spirit",spirit);Set(level,"inspection",doors["inspection"]);Set(level,"exitPassage",doors["exit-passage"]);
            Set(level,"cargoEntry",doors["cargo-entry"]);Set(level,"cargo",doors["cargo"]);Set(level,"loadingWall",walls["Loading wall"]);
            var settings=new SerializedObject(hud);var objective=settings.FindProperty("objectiveText").objectReferenceValue;
            settings.FindProperty("objectives").arraySize=0;settings.ApplyModifiedPropertiesWithoutUndo();Set(level,"objective",objective);
            // Shared HUD callbacks now route the third campaign level to Finish / Play Again.
            foreach(var template in templates)Object.DestroyImmediate(template);
            CampaignCombatSetup.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Warehouse campaign Level 3 built: original layout with wider rooms/passages; 4 Guards, 2 Heavies, 4 target doors, 3 cracked walls and 9 charge sources.");
        }
        [MenuItem("Possession/Level Design/Level 3 Warehouse/Open Local Scene")]
        public static void Open(){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorSceneManager.OpenScene(ScenePath);}

        private static void BoundaryWalls(Transform parent)
        {
            bool Inside(int x,int y)=>data.rooms.Concat(data.halls).Any(r=>x+.5f>=r.x&&x+.5f<r.x+r.width&&y+.5f>=r.y&&y+.5f<r.y+r.depth);
            // Merge collinear unit edges, retaining all open room/hall connections from the plan.
            int minX=Mathf.FloorToInt(data.rooms.Concat(data.halls).Min(r=>r.x));
            int maxX=Mathf.CeilToInt(data.rooms.Concat(data.halls).Max(r=>r.x+r.width));
            int minY=Mathf.FloorToInt(data.rooms.Concat(data.halls).Min(r=>r.y));
            int maxY=Mathf.CeilToInt(data.rooms.Concat(data.halls).Max(r=>r.y+r.depth));
            for(int y=minY;y<=maxY;y++)
            {
                int? start=null;
                for(int x=minX;x<=maxX;x++)
                {
                    bool edge=x<maxX&&Inside(x,y-1)!=Inside(x,y);
                    if(edge&&!start.HasValue)start=x;
                    if(!edge&&start.HasValue){Wall(parent,start.Value,y,x,y);start=null;}
                }
            }
            for(int x=minX;x<=maxX;x++)
            {
                int? start=null;
                for(int y=minY;y<=maxY;y++)
                {
                    bool edge=y<maxY&&Inside(x-1,y)!=Inside(x,y);
                    if(edge&&!start.HasValue)start=y;
                    if(!edge&&start.HasValue){Wall(parent,x,start.Value,x,y);start=null;}
                }
            }
        }
        private static void Wall(Transform parent,float x1,float y1,float x2,float y2)
        {
            var a=Map(x1,y1,3.1f);var b=Map(x2,y2,3.1f);
            var go=Cube(parent,"BoundaryWall",(a+b)/2,new Vector3(Mathf.Max(.4f,Mathf.Abs(b.x-a.x)),6f,Mathf.Max(.4f,Mathf.Abs(b.z-a.z))),wallMat);
            go.AddComponent<VisibilityOccluder>();
        }
        private static Door BuildDoor(Transform parent,Gate g,Color color)
        {
            float width=Vector2.Distance(new Vector2(g.x1,g.y1),new Vector2(g.x2,g.y2))*Unit;
            var node=new GameObject(g.key+"_Door");node.transform.SetParent(parent);node.transform.position=Map((g.x1+g.x2)/2,(g.y1+g.y2)/2,3.1f);
            node.transform.rotation=Quaternion.Euler(0,g.x1==g.x2?90f:0f,0);
            node.layer=7;node.AddComponent<BoxCollider>().size=new Vector3(width,6f,.55f);
            var door=node.AddComponent<Door>();var targets=new List<Target>();
            var groupMat=Mat(g.key+"_Signal",color,true);
            for(int i=0;i<g.targets.Length;i++)
            {
                var p=g.targets[i];var obj=Object.Instantiate(targetTemplate,parent);obj.name=g.key+"_Target_"+(i+1);
                obj.transform.position=Map(p.x,p.y,1.6f);obj.transform.localScale=Vector3.one;obj.SetActive(true);
                var r=obj.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=groupMat;
                targets.Add(obj.GetComponent<Target>());
                var stand=Cube(obj.transform,"Target stand",obj.transform.position-Vector3.up*.8f,new Vector3(.12f,1.4f,.12f),steelMat);
                Object.DestroyImmediate(stand.GetComponent<Collider>());
            }
            Array(door,"requiredTargets",targets.Cast<Object>().ToArray());Set(door,"isExit",false);
            var view=node.AddComponent<WarehouseDoorView>();Array(view,"targets",targets.Cast<Object>().ToArray());Set(view,"groupColor",color);Set(view,"width",width);
            var left=LocalCube(node.transform,"Left sliding leaf",new Vector3(-width*.25f,0,0),new Vector3(width/2,5.5f,.3f),steelMat);
            var right=LocalCube(node.transform,"Right sliding leaf",new Vector3(width*.25f,0,0),new Vector3(width/2,5.5f,.3f),steelMat);
            Set(view,"leftLeaf",left.transform);Set(view,"rightLeaf",right.transform);
            LocalCube(node.transform,"Door jamb left",new Vector3(-width/2-.12f,0,0),new Vector3(.24f,6f,.65f),steelMat);
            LocalCube(node.transform,"Door jamb right",new Vector3(width/2+.12f,0,0),new Vector3(.24f,6f,.65f),steelMat);
            LocalCube(node.transform,"Door header",new Vector3(0,2.85f,0),new Vector3(width+.5f,.3f,.65f),steelMat);
            var lamps=new List<Object>();
            for(int i=0;i<targets.Count;i++)lamps.Add(LocalCube(node.transform,"Target lamp "+(i+1),new Vector3(((i+.5f)/targets.Count-.5f)*width*.75f,2.55f,-.4f),new Vector3(.25f,.25f,.15f),groupMat).GetComponent<Renderer>());
            Array(view,"lamps",lamps.ToArray());
            return door;
        }
        private static BreakableWall BuildBarrier(Transform parent,Segment s)
        {
            var obj=Object.Instantiate(wallTemplate,parent);obj.name=s.name.Replace(' ','_');obj.SetActive(true);
            obj.transform.position=Map((s.x1+s.x2)/2,(s.y1+s.y2)/2,3.1f);obj.transform.rotation=Quaternion.Euler(0,s.x1==s.x2?90f:0f,0);
            float width=Vector2.Distance(new Vector2(s.x1,s.y1),new Vector2(s.x2,s.y2))*Unit;obj.transform.localScale=new Vector3(width,6,.5f);
            if(s.transparent)
            {
                foreach(var child in obj.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var mat=Mat("Cracked recovery glass",new Color(.25f,.75f,.7f,.22f));
                mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHABLEND_ON");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
                obj.GetComponent<Renderer>().sharedMaterial=mat;
                // A visibly cracked see-through panel retains its full shared wall collider.
                var crack=new GameObject("Visible crack").AddComponent<LineRenderer>();crack.transform.SetParent(obj.transform,false);
                crack.useWorldSpace=false;crack.widthMultiplier=.035f;crack.sharedMaterial=Mat("Crack glow",new Color(1f,.65f,.2f),true);crack.positionCount=5;
                crack.SetPositions(new[]{new Vector3(-.3f,-.45f,-.55f),new Vector3(.08f,-.15f,-.55f),new Vector3(-.1f,0,-.55f),new Vector3(.2f,.2f,-.55f),new Vector3(0,.45f,-.55f)});
            }
            return obj.GetComponent<BreakableWall>();
        }
        private static void BuildEnemy(Transform parent,Actor a,int index)
        {
            bool heavy=a.kind!="Guard";var obj=Object.Instantiate(heavy?heavyTemplate:guardTemplate,parent);obj.name=a.name;
            obj.transform.position=Map(a.x,a.y,heavy?1.45f:1.15f);obj.transform.rotation=Quaternion.identity;obj.SetActive(true);
            var enemy=obj.GetComponent<EnemyBase>();
            var room=data.rooms.FirstOrDefault(r=>a.x>=r.x&&a.x<=r.x+r.width&&a.y>=r.y&&a.y<=r.y+r.depth);
            if(room==null && a.name=="Right_junction_Heavy")room=new Area{x=98,y=41,width=23,depth=12};
            if(room==null)throw new InvalidOperationException("No home room for "+a.name);
            Set(enemy,"useHomeBounds",true);
            Set(enemy,"homeBounds",new Bounds(Map(room.x+room.width/2,room.y+room.depth/2,3),new Vector3(room.width*Unit,10f,room.depth*Unit)));
            var controller=obj.GetComponent<CharacterController>();controller.minMoveDistance=0;
            var route=new GameObject(a.name+"_Patrol").transform;route.SetParent(parent);
            float distance=heavy||a.name=="Guard_room_Guard"?2f:3f;var points=new Transform[2];
            for(int i=0;i<2;i++){points[i]=new GameObject("Waypoint_"+i).transform;points[i].SetParent(route);points[i].position=Map(a.x+(i==0?-distance:distance),a.y,obj.transform.position.y);}
            Array(enemy,"waypoints",points.Cast<Object>().ToArray());Set(enemy,"patrolStartDelay",index*.25f);
        }
        private static void BuildSource(Transform parent,Point p,int index)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Puzzle/SoulCoin.prefab");GameObject obj;
            if(prefab!=null)obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            else
            {
                obj=new GameObject();obj.AddComponent<SphereCollider>().radius=1.1f;
                var gem=LocalCube(obj.transform,"Charge crystal",Vector3.up,new Vector3(.4f,.7f,.4f),chargeMat);
                gem.transform.localRotation=Quaternion.Euler(25,45,20);var pickup=obj.AddComponent<PossessionChargePickup>();Set(pickup,"shard",gem.GetComponent<Renderer>());
            }
            obj.name="Charge_Source_"+(index+1).ToString("00");obj.transform.SetParent(parent);obj.transform.position=Map(p.x,p.y,.1f);
            var source=obj.GetComponent<PossessionChargePickup>();Set(source,"reforms",true);Set(source,"cooldown",8f);obj.GetComponent<SphereCollider>().isTrigger=true;
        }
        private static void BuildHealthSocket(Transform parent)
        {
            var obj=new GameObject("Health_Pickup_Placement");obj.transform.SetParent(parent);obj.transform.position=Map(data.health.x,data.health.y,.25f);
            obj.AddComponent<WarehouseHealthSocket>();
            var mat=Mat("Health socket",new Color(.25f,1f,.5f),true);
            LocalCube(obj.transform,"Health cross vertical",new Vector3(0,.65f,0),new Vector3(.25f,1f,.25f),mat);
            LocalCube(obj.transform,"Health cross horizontal",new Vector3(0,.65f,0),new Vector3(.8f,.25f,.25f),mat);
        }
        private static WarehouseLevel BuildExit(Transform parent,Point p)
        {
            var obj=new GameObject("Warehouse_Extraction");obj.transform.SetParent(parent);obj.transform.position=Map(p.x,p.y,1.2f);
            var trigger=obj.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(6,3,4);
            var deck=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_CheckerMat.mat")??floorMat;
            LocalCube(obj.transform,"Extraction deck",new Vector3(0,-1.02f,0),new Vector3(6,.12f,4),deck);
            var lights=new List<Object>();
            foreach(float side in new[]{-1f,1f})lights.Add(LocalCube(obj.transform,"Extraction gate",new Vector3(side*3,1,0),new Vector3(.2f,4,.2f),glowMat).GetComponent<Renderer>());
            lights.Add(LocalCube(obj.transform,"Extraction crown",new Vector3(0,3,0),new Vector3(6.2f,.2f,.2f),glowMat).GetComponent<Renderer>());
            var sign=new GameObject("Exit sign").AddComponent<TextMesh>();sign.transform.SetParent(obj.transform,false);sign.transform.localPosition=new Vector3(0,2.3f,-.2f);sign.transform.rotation=Quaternion.Euler(55,0,0);sign.transform.localScale=Vector3.one*.07f;sign.text="EXIT";sign.fontSize=56;sign.anchor=TextAnchor.MiddleCenter;sign.color=new Color(.3f,1f,.6f);
            var level=obj.AddComponent<WarehouseLevel>();Array(level,"exitLights",lights.ToArray());return level;
        }
        private static GameObject Cube(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent);obj.transform.position=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=mat;return obj;
        }
        private static GameObject LocalCube(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat)
        {
            var obj=Cube(parent,name,Vector3.zero,scale,mat);obj.transform.localPosition=pos;obj.transform.localRotation=Quaternion.identity;Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        private static Material Mat(string name,Color color,bool unlit=false)
        {
            string path=Root+"/Materials/"+name.Replace(' ','_')+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find(unlit?"Sprites/Default":"Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;EditorUtility.SetDirty(mat);return mat;
        }
        private static void Set(Object target,string field,object value)
        {
            var settings=new SerializedObject(target);var p=settings.FindProperty(field);if(p==null)throw new MissingFieldException(target.GetType().Name,field);
            if(value is float f)p.floatValue=f;else if(value is bool b)p.boolValue=b;else if(value is Color c)p.colorValue=c;else if(value is Bounds bounds)p.boundsValue=bounds;else p.objectReferenceValue=value as Object;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Array(Object target,string field,Object[] values)
        {
            var settings=new SerializedObject(target);var p=settings.FindProperty(field);p.arraySize=values.Length;
            for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
