using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class Level02MazeBuilder
    {
        const string Path = "Assets/Scenes/Level_02.unity";
        const float Unit = 1.6f;
        static readonly RectInt[] Rooms = {
            new(1,9,5,7), new(9,9,6,7), new(9,1,7,5), new(20,1,7,5), new(9,19,7,4),
            new(20,19,7,4), new(29,9,4,7), new(1,1,5,5), new(1,19,5,4), new(20,9,4,5) };
        static readonly RectInt[] Halls = {
            new(6,12,3,2),new(11,6,2,3),new(16,2,4,2),new(25,6,2,4),new(27,8,4,2),
            new(11,16,2,3),new(16,20,4,2),new(25,15,2,4),new(27,14,4,2),new(3,6,2,3),new(6,20,3,2),new(21,6,2,3) };
        static readonly float[,] Covers = {
            {2,12,2,1},{10.7f,11.5f,1.6f,.5f},{10,2,1,1},{21,4.7f,1,.7f},{10,22,1,.6f},
            {23,20,1,1},{29.3f,15.5f,1,.5f},{2,3,2,1},{2,20.5f,.6f,1.5f},{20.5f,11,1,1} };
        public static Vector3 Map(float x,float y,float height=1.15f) => new((x-17f)*Unit,height,(12f-y)*Unit);

        [MenuItem("Possession/Level 2/Build Hunted Charge Maze")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Path))
            {
                AssetDatabase.CopyAsset("Assets/Scenes/Level_01.unity",Path);
                AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
            }
            var scene=EditorSceneManager.OpenScene(Path);
            var spirit=Object.FindFirstObjectByType<SpiritController>();
            var manager=Object.FindFirstObjectByType<GameManager>();
            var hud=Object.FindFirstObjectByType<HUDManager>();
            var camera=Camera.main;
            var guardTemplate=Object.Instantiate(Object.FindFirstObjectByType<GuardEnemy>().gameObject);
            var heavyTemplate=Object.Instantiate(Object.FindFirstObjectByType<HeavyEnemy>().gameObject);
            guardTemplate.SetActive(false); heavyTemplate.SetActive(false);
            // Copy the team's authored puzzle visuals without editing their source scene or prefabs.
            var styleScene=EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity",OpenSceneMode.Additive);
            var styles=styleScene.GetRootGameObjects();
            var targetTemplate=Object.Instantiate(styles.SelectMany(r=>r.GetComponentsInChildren<Target>(true)).First().gameObject);
            var doorTemplate=Object.Instantiate(styles.SelectMany(r=>r.GetComponentsInChildren<Door>(true)).First(d=>d.GetComponent<LevelExit>()==null).gameObject);
            var wallTemplate=Object.Instantiate(styles.SelectMany(r=>r.GetComponentsInChildren<BreakableWall>(true)).First().gameObject);
            foreach(var template in new[]{targetTemplate,doorTemplate,wallTemplate}) {SceneManager.MoveGameObjectToScene(template,scene);template.SetActive(false);}
            EditorSceneManager.CloseScene(styleScene,true);SceneManager.SetActiveScene(scene);
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root==guardTemplate || root==heavyTemplate || root==targetTemplate || root==doorTemplate || root==wallTemplate || root.GetComponent<GameManager>()!=null
                    || root.GetComponent<SpiritController>()!=null || root.GetComponent<Camera>()!=null
                    || root.GetComponentInChildren<HUDManager>(true)!=null || root.GetComponent<EventSystem>()!=null) continue;
                Object.DestroyImmediate(root);
            }
            foreach(var old in manager.GetComponents<Level02Director>()) Object.DestroyImmediate(old);
            if(spirit.GetComponent<PossessionCharge>()==null) spirit.gameObject.AddComponent<PossessionCharge>();
            var chargeSo=new SerializedObject(spirit.GetComponent<PossessionCharge>());
            chargeSo.FindProperty("startingCharges").intValue=1;
            chargeSo.ApplyModifiedPropertiesWithoutUndo();
            spirit.transform.position=Map(3,14);
            LevelBuildKit.BuildLighting();
            camera.backgroundColor=new Color(.08f,.08f,.1f);
            camera.fieldOfView=40f;
            camera.transform.rotation=Quaternion.Euler(55,0,0);
            camera.transform.position=spirit.transform.position+new Vector3(0,25,-18);
            Set(camera.GetComponent<CameraFollow>(),"offset",new Vector3(0,25,-18));
            Set(camera.GetComponent<CameraFollow>(),"target",spirit.transform);
            var world=new GameObject("Level02_ChargeMaze").transform;
            var floor=Material("CourtyardStone",new Color(.33f,.42f,.36f));
            var wallMat=Material("SunlitPlaster",new Color(.55f,.72f,.90f));
            var coverMat=Material("OldWood",new Color(.23f,.20f,.18f));
            var shardMat=Material("ChargeEnergy",new Color(.35f,1f,.72f),true);
            var dustMat=Material("DustPrint",new Color(.52f,.52f,.47f));
            var grid=new bool[35,25];
            for(int i=0;i<Rooms.Length;i++)
            {
                string name=i<7?"R"+(i+1):"D"+(i-6);
                var room=new GameObject(name).transform; room.SetParent(world);
                Floor(room,name+"_Floor",Rooms[i],floor);
                foreach(var cell in Rooms[i].allPositionsWithin) grid[cell.x,cell.y]=true;
            }
            for(int i=0;i<Halls.Length;i++)
            {
                Floor(world,"Hall_"+i,Halls[i],floor);
                foreach(var cell in Halls[i].allPositionsWithin) grid[cell.x,cell.y]=true;
            }
            var walls=new GameObject("SolidWalls").transform; walls.SetParent(world);
            for(int y=0;y<25;y++)
            {
                int x=0; while(x<35)
                {
                    if(!grid[x,y] || y>0 && grid[x,y-1]) {x++;continue;}
                    int from=x++;while(x<35 && grid[x,y] && (y==0 || !grid[x,y-1]))x++;
                    Wall(walls,from,y,x,y,wallMat);
                }
                x=0;while(x<35)
                {
                    if(!grid[x,y] || y<24 && grid[x,y+1]) {x++;continue;}
                    int from=x++;while(x<35 && grid[x,y] && (y==24 || !grid[x,y+1]))x++;
                    Wall(walls,from,y+1,x,y+1,wallMat);
                }
            }
            for(int x=0;x<35;x++)
            {
                int y=0;while(y<25)
                {
                    if(!grid[x,y] || x>0 && grid[x-1,y]){y++;continue;}
                    int from=y++;while(y<25 && grid[x,y] && (x==0 || !grid[x-1,y]))y++;
                    Wall(walls,x,from,x,y,wallMat);
                }
                y=0;while(y<25)
                {
                    if(!grid[x,y] || x<34 && grid[x+1,y]){y++;continue;}
                    int from=y++;while(y<25 && grid[x,y] && (x==34 || !grid[x+1,y]))y++;
                    Wall(walls,x+1,from,x+1,y,wallMat);
                }
            }
            for(int i=0;i<Covers.GetLength(0);i++)
            {
                var pos=Map(Covers[i,0]+Covers[i,2]*.5f,Covers[i,1]+Covers[i,3]*.5f,1.35f);
                Cube(world,"Cover_"+i,pos,new Vector3(Covers[i,2]*Unit,2.7f,Covers[i,3]*Unit),coverMat);
            }
            var targetN=Target(world,"TN",13,3.5f,targetTemplate);
            var targetS=Target(world,"TS",14.5f,20,targetTemplate);
            var gateN=Gate(world,"GN",18,3,targetN,doorTemplate);
            var gateS=Gate(world,"GS",18,21,targetS,doorTemplate);
            var barrierN=Barrier(world,"BN",26,7.25f,wallTemplate);
            var barrierS=Barrier(world,"BS",12,17.25f,wallTemplate);
            foreach(var template in new[]{targetTemplate,doorTemplate,wallTemplate})Object.DestroyImmediate(template);
            var director=manager.gameObject.AddComponent<Level02Director>();
            Set(director,"spirit",spirit);Set(director,"courtyard",Map(10,14));
            Set(director,"northDoor",gateN);Set(director,"southDoor",gateS);
            Set(director,"northWall",barrierN);Set(director,"southWall",barrierS);
            Pickup(world,"C1",12,13.5f,true,shardMat);Pickup(world,"C2",23,3,true,shardMat);
            Pickup(world,"C3",12,21,true,shardMat);Pickup(world,"C4",3.5f,22,true,shardMat);
            Pickup(world,"C5",5,5,true,shardMat);Pickup(world,"C6",18,10,true,shardMat);
            Pickup(world,"C7",26,21,true,shardMat);Pickup(world,"C8",30,8,true,shardMat);
            var checkpoint=new GameObject("Courtyard checkpoint");checkpoint.transform.SetParent(world);checkpoint.transform.position=Map(12,12.5f,2f);
            var cv=checkpoint.AddComponent<BoxCollider>();cv.isTrigger=true;cv.size=new Vector3(9.3f,5f,10.8f);
            Set(checkpoint.AddComponent<Level02CheckpointVolume>(),"director",director);
            var exit=new GameObject("ExitZone");exit.transform.SetParent(world);exit.transform.position=Map(31,12,1.3f);
            var ec=exit.AddComponent<BoxCollider>();ec.isTrigger=true;ec.size=new Vector3(3f,3f,3f);
            Set(exit.AddComponent<Level02Exit>(),"director",director);
            Cube(exit.transform,"ExitPlatform",Map(31,12,.18f),new Vector3(3f,.15f,3f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_Exit.mat"));
            var enemies=new GameObject("HuntedEnemies").transform;enemies.SetParent(world);
            Enemy(enemies,guardTemplate,"Guard_Entrance",4.7f,10.5f,5,14,5f);
            Enemy(enemies,guardTemplate,"Guard_Courtyard",10.5f,10.5f,14,10.5f,5f);
            Enemy(enemies,heavyTemplate,"Heavy_Courtyard",13.6f,15,10,15,3f);
            Enemy(enemies,guardTemplate,"Guard_North",14.5f,2,14.5f,5,5f);
            Enemy(enemies,heavyTemplate,"Heavy_North",24.5f,4.5f,25,2,3f);
            Enemy(enemies,guardTemplate,"Guard_South",14.2f,22,13.2f,19.8f,5f);
            Enemy(enemies,guardTemplate,"Guard_GateChamber",24.5f,21.7f,21,21.8f,5f);
            Enemy(enemies,heavyTemplate,"Heavy_Exit",31.5f,14.3f,31,10,3f);
            Object.DestroyImmediate(guardTemplate);Object.DestroyImmediate(heavyTemplate);
            var config=LevelConfigAssets.Load("Level_02");LevelBuildKit.ApplyConfig(config,manager,spirit.gameObject);
            foreach(var enemy in world.GetComponentsInChildren<EnemyBase>())
            {Set(enemy,"detectionRange",enemy is HeavyEnemy?3f:5f);Set(enemy,"chaseStopDistance",1.5f);}
            foreach(var w in new[] {barrierN,barrierS})w.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_Brick.mat");
            // Daylight clues are floor marks and sunlit alcoves, rather than glowing routes.
            Prints(world,Map(3,14,.13f),Map(8,13,.13f),dustMat);
            Prints(world,Map(3,9,.13f),Map(4,4,.13f),dustMat);
            Prints(world,Map(12,12,.13f),Map(12,8,.13f),dustMat);
            Prints(world,Map(13,15,.13f),Map(12,17,.13f),dustMat);
            var sun=Material("SunlitAlcove",new Color(.42f,.44f,.40f));
            Cube(world,"D3_SunPatch",Map(22,11,.13f),new Vector3(3f,.03f,3.8f),sun);
            var objective=hud.transform.Find("ObjectiveBar");
            if(objective!=null)
            {
                objective.gameObject.SetActive(false);
                Set(hud,"objectiveText",objective.GetComponentInChildren<UnityEngine.UI.Text>(true));
            }
            foreach(var t in world.GetComponentsInChildren<WorldLabel>())Object.DestroyImmediate(t.gameObject);
            CampaignSpaceSetup.Apply(scene);
            CampaignCombatSetup.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,Path);
            LevelBuildKit.RegisterScene(Path);AssetDatabase.SaveAssets();
            Debug.Log("Level 2 Hunted built: 7 rooms, 3 dead ends, 8 enemies, 8 shards, 2 independent routes.");
        }
        static void Floor(Transform parent,string name,RectInt rect,Material mat)
        { var p=Map(rect.center.x,rect.center.y,0);Cube(parent,name,p,new Vector3(rect.width*Unit,.2f,rect.height*Unit),mat); }
        static void Wall(Transform parent,float x1,float y1,float x2,float y2,Material mat)
        { var a=Map(x1,y1,3f);var b=Map(x2,y2,3f);Cube(parent,"Wall",(a+b)*.5f,new Vector3(Mathf.Max(.35f,Mathf.Abs(b.x-a.x)),6f,Mathf.Max(.35f,Mathf.Abs(b.z-a.z))),mat); }
        static GameObject Cube(Transform parent,string name,Vector3 pos,Vector3 size,Material mat)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=pos;go.transform.localScale=size;go.isStatic=true;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
        static Material Material(string name,Color color,bool unlit=false)
        {
            Directory.CreateDirectory("Assets/Level02/Materials");
            string path="Assets/Level02/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find(unlit?"Sprites/Default":"Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;EditorUtility.SetDirty(mat);return mat;
        }
        static Target Target(Transform parent,string name,float x,float y,GameObject template)
        {var go=Puzzle(parent,name,Map(x,y,1.6f),template);go.transform.rotation=Quaternion.identity;Set(go.GetComponent<Target>(),"isHit",false);return go.GetComponent<Target>();}
        static Door Gate(Transform parent,string name,float x,float y,Target target,GameObject template)
        {
            var go=Puzzle(parent,name,Map(x,y,3f),template);go.transform.rotation=Quaternion.Euler(0,90,0);
            var door=go.GetComponent<Door>();var so=new SerializedObject(door);var targets=so.FindProperty("requiredTargets");targets.arraySize=1;targets.GetArrayElementAtIndex(0).objectReferenceValue=target;so.FindProperty("isExit").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<Level02GateVisual>();return door;
        }
        static BreakableWall Barrier(Transform parent,string name,float x,float y,GameObject template)
        {var go=Puzzle(parent,name,Map(x,y,3f),template);go.transform.rotation=Quaternion.identity;return go.GetComponent<BreakableWall>();}
        static GameObject Puzzle(Transform parent,string name,Vector3 position,GameObject template)
        {var go=Object.Instantiate(template,parent);go.name=name;go.transform.position=position;go.SetActive(true);go.layer=LevelBuildKit.PuzzleLayer;return go;}
        static void Pickup(Transform parent,string name,float x,float y,bool repeat,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=Map(x,y,.1f);
            var collider=go.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=1.1f;
            var shard=GameObject.CreatePrimitive(PrimitiveType.Cube);shard.name="Energy shard";shard.transform.SetParent(go.transform,false);
            shard.transform.localPosition=Vector3.up;shard.transform.localScale=new Vector3(.45f,.7f,.45f);shard.transform.localRotation=Quaternion.Euler(35,45,35);
            Object.DestroyImmediate(shard.GetComponent<Collider>());shard.GetComponent<Renderer>().sharedMaterial=mat;
            var pickup=go.AddComponent<PossessionChargePickup>();Set(pickup,"reforms",repeat);Set(pickup,"shard",shard.GetComponent<Renderer>());
        }
        static void Enemy(Transform parent,GameObject template,string name,float x,float y,float endX,float endY,float detect)
        {
            var go=Object.Instantiate(template,parent);go.name=name;go.SetActive(true);go.transform.position=Map(x,y,template.GetComponent<HeavyEnemy>()!=null?1.45f:1.15f);go.layer=LevelBuildKit.EnemyLayer;
            go.GetComponent<CharacterController>().minMoveDistance=0f;
            var enemy=go.GetComponent<EnemyBase>();Set(enemy,"moveSpeed",enemy is HeavyEnemy?4f:5f);Set(enemy,"detectionRange",detect);Set(enemy,"patrolStartDelay",((int)x%4)*.25f);
            var marker=new GameObject("SpawnPoint_"+name);marker.transform.SetParent(parent);marker.transform.position=go.transform.position;
            var so=new SerializedObject(enemy);var array=so.FindProperty("waypoints");array.arraySize=2;
            for(int i=0;i<2;i++){var p=new GameObject(name+"_WP"+i);p.transform.SetParent(parent);p.transform.position=i==0?go.transform.position:Map(endX,endY,go.transform.position.y);array.GetArrayElementAtIndex(i).objectReferenceValue=p.transform;}
            so.ApplyModifiedPropertiesWithoutUndo();if(go.GetComponent<HuntedEnemyPressure>()==null)go.AddComponent<HuntedEnemyPressure>();
            var attacks=go.GetComponent<EnemyContactDamage>();
            Set(attacks,"firstAttackDelay",2f);Set(attacks,"shootInterval",3f);
            Set(attacks,"smashWindup",1.1f);Set(attacks,"smashCooldown",3f);Set(attacks,"smashRange",2.3f);
            Set(attacks,"touchCooldown",3f);
        }
        static void Prints(Transform parent,Vector3 from,Vector3 to,Material mat)
        {int count=Mathf.FloorToInt(Vector3.Distance(from,to)/.9f);for(int i=0;i<count;i++){var p=Vector3.Lerp(from,to,(i+.5f)/count);p.x+=(i%2==0?.13f:-.13f);Cube(parent,"Dust footprint",p,new Vector3(.14f,.025f,.3f),mat).GetComponent<Collider>().enabled=false;}}
        static void Set(Object target,string field,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(field);
            if(value is float f)p.floatValue=f;else if(value is bool b)p.boolValue=b;else if(value is Vector3 v)p.vector3Value=v;else p.objectReferenceValue=value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
