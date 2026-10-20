using System;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.LocalLevel04.Editor
{
    public static class LocalLevel04Builder
    {
        public const string ScenePath = "Assets/LocalLevel04/Level_04_Local.unity";
        private const string Root = "Assets/LocalLevel04";
        private static Transform geometry, routes;
        private static Material wallMat, guardMat, heavyMat, redMat, cyanMat, amberMat;

        [MenuItem("Possession/Local Experiments/Open Level 04")]
        public static void Open()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) { Build(); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Possession/Local Experiments/Rebuild Level 04")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            geometry = new GameObject("Local04_Geometry").transform;
            routes = new GameObject("Local04_PatrolRoutes").transform;
            wallMat = Mat("Wall", new Color(.2f, .25f, .34f));
            guardMat = Mat("Guard", new Color(.1f, .55f, .95f));
            heavyMat = Mat("Heavy", new Color(1f, .45f, .06f));
            redMat = Mat("Target", new Color(1f, .12f, .15f));
            cyanMat = Mat("GuardGate", new Color(.08f, .7f, .8f));
            amberMat = Mat("HeavyGate", new Color(.95f, .53f, .12f));
            Floor("West_Wing", 0, 14, 0, 40, new Color(.29f, .38f, .45f));
            Floor("Central_Wing", 14, 42, 0, 40, new Color(.38f, .35f, .29f));
            Floor("East_Wing", 42, 66, 0, 40, new Color(.28f, .37f, .34f));
            Floor("Cache_Floor", 14.6f, 30.4f, 11.6f, 27.4f, new Color(.42f, .35f, .43f), .01f);
            Floor("Finish_Floor", 42.6f, 65.4f, 29.6f, 39.4f, new Color(.2f, .48f, .37f), .01f);
            Wall("Boundary_West", 0, 20, 1, 41);
            Wall("Boundary_East", 66, 20, 1, 41);
            Wall("Boundary_South", 33, 0, 66, 1);
            Wall("Boundary_North", 33, 40, 66, 1);
            Wall("Vestibule_Left", 1.5f, 10, 3, 1);
            Wall("Vestibule_Right", 11.5f, 10, 5, 1);
            Wall("West_Cover", 7, 21, 4, 5);
            Wall("West_Divider_Lower", 14, 14.85f, 1, 29.7f);
            Wall("West_Divider_Upper", 14, 37.15f, 1, 5.7f);
            Wall("Cache_North", 22.5f, 28, 17, 1);
            Wall("Cache_South", 22.5f, 11, 17, 1);
            Wall("Cache_East_Lower", 31, 14.35f, 1, 6.7f);
            Wall("Cache_East_Upper", 31, 25.15f, 1, 5.7f);
            Wall("East_Divider_Lower", 42, 1.35f, 1, 2.7f);
            Wall("East_Divider_Upper", 42, 23.65f, 1, 32.7f);
            Wall("East_Baffle_South", 50.5f, 10, 17, 1);
            Wall("East_Baffle_North", 50.5f, 20, 17, 1);
            Wall("East_Baffle_End", 59, 15, 1, 10);
            Wall("Finish_Divider_Left", 46.35f, 29, 8.7f, 1);
            Wall("Finish_Divider_Right", 60.65f, 29, 10.7f, 1);

            var targetA = MakeTarget("Target_01_West", new Vector3(10, 1.6f, 36));
            var targetB = MakeTarget("Target_02_CacheRelay", new Vector3(18, 1.6f, 16));
            var doorA = Door("01_Guard_Door", 14, 32, 1, 4.6f, targetA);
            var wallA = Breakable("02_Heavy_Cache_Wall", 31, 20, 1, 4.6f);
            var doorB = Door("03_Guard_Relay_Door", 42, 5, 1, 4.6f, targetB);
            var wallB = Breakable("04_Heavy_Finish_Wall", 53, 29, 4.6f, 1);

            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            manager.gameObject.AddComponent<AnalyticsTracker>();
            var spiritPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spirit.prefab");
            var spiritObject = (GameObject)PrefabUtility.InstantiatePrefab(spiritPrefab);
            spiritObject.name = "Spirit";
            spiritObject.transform.position = new Vector3(6, 1.15f, 4);
            spiritObject.GetComponent<Renderer>().sharedMaterial = Mat("Spirit", new Color(.8f, .95f, 1));

            Host("Guard_West", false, new Vector3(6, 1.15f, 31),
                new Vector3(5, 0, 30), new Vector3(9, 0, 34));
            Host("Heavy_Cache", true, new Vector3(35, 1.45f, 28),
                new Vector3(35, 0, 28), new Vector3(38, 0, 23));
            Host("Guard_Relay", false, new Vector3(35, 1.15f, 12),
                new Vector3(35, 0, 12), new Vector3(37, 0, 17));
            Host("Heavy_Finish", true, new Vector3(61, 1.45f, 23),
                new Vector3(61, 0, 23), new Vector3(56, 0, 25));
            Host("Guard_East_Sentinel", false, new Vector3(61, 1.15f, 6),
                new Vector3(52, 0, 5), new Vector3(62, 0, 7));

            var goal = new GameObject("ExitZone");
            goal.transform.position = new Vector3(53, 1.5f, 36);
            var exit = goal.AddComponent<LevelExit>();
            var trigger = goal.GetComponent<BoxCollider>();
            trigger.size = new Vector3(6, 3, 4); trigger.isTrigger = true;
            Set(exit, "puzzleDoor", doorB); Set(exit, "breakableWall", wallB);

            var config = Config();
            Possession.Editor.LevelBuildKit.ApplyConfig(config, manager, spiritObject);
            var hud = CopySharedHUD(scene);
            ConfigureObjectives(hud, doorA, wallA, doorB, wallB);

            Label("Start", "START / THE RELAY RUN\nE: POSSESS   LMB: ABILITY", new Vector3(6, .14f, 7), .25f, Color.white);
            Label("West", "01 / GUARD RANGE", new Vector3(7, .14f, 26), .3f, Color.cyan);
            Label("Cache", "RELAY CACHE\nGUARD REQUIRED INSIDE", new Vector3(22, .16f, 23), .25f, Color.white);
            Label("Heavy1", "02 / TAKE THE HEAVY", new Vector3(36, .14f, 33), .25f, new Color(1, .7f, .25f));
            Label("RelayGuard", "03 / RETURN AS GUARD", new Vector3(35, .14f, 8), .23f, Color.cyan);
            Label("LongWay", "LONG WAY ROUND >", new Vector3(51, .14f, 5), .23f, Color.white);
            Label("Heavy2", "04 / FINAL HEAVY", new Vector3(58, .14f, 24), .23f, new Color(1, .7f, .25f));
            Label("Finish", "FINISH\nENTER AS SPIRIT", new Vector3(53, .15f, 36), .4f, Color.white);
            Label("TargetA", "SHOOT / 01", new Vector3(10, .15f, 37.5f), .2f, Color.white);
            Label("TargetB", "SHOOT / RELAY", new Vector3(19, .16f, 14), .2f, Color.white);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .6f, .65f);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .055f, .085f);
            // Match the perspective view and follow distance used in Levels 01–03.
            camera.orthographic = false; camera.fieldOfView = 40; camera.farClipPlane = 200;
            var offset = new Vector3(0, 25, -13);
            camera.transform.position = spiritObject.transform.position + offset;
            camera.transform.rotation = Quaternion.Euler(55, 0, 0);
            camera.gameObject.AddComponent<AudioListener>();
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            Set(follow, "target", spiritObject.transform); Set(follow, "offset", offset);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Possession.Editor.LevelBuildKit.RegisterScene(ScenePath);
            Debug.Log("Level 04 built with shared gameplay, HUD and level config. Open it and press Play.");
        }

        // Updates only presentation in the saved local scene, preserving hand-edited layout.
        public static void ApplyPerspectiveToSavedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var camera = Camera.main;
            var spirit = UnityEngine.Object.FindFirstObjectByType<SpiritController>();
            camera.orthographic = false;
            camera.fieldOfView = 40;
            camera.transform.rotation = Quaternion.Euler(55, 0, 0);
            Vector3 offset = new Vector3(0, 25, -13);
            camera.transform.position = spirit.transform.position + offset;
            Set(camera.GetComponent<CameraFollow>(), "offset", offset);
            RenderSettings.ambientLight = new Color(.6f, .6f, .65f);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                {
                    light.intensity = 1;
                    light.shadows = LightShadows.Soft;
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                }
            EditorSceneManager.SaveScene(scene);
        }

        private static EnemyBase Host(string name, bool heavy, Vector3 position, params Vector3[] points)
        {
            string path = heavy ? "Assets/Prefabs/Enemies/Heavy.prefab" : "Assets/Prefabs/Enemies/Guard.prefab";
            var node = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            node.name = name; node.transform.position = position;
            node.GetComponent<Renderer>().sharedMaterial = heavy ? heavyMat : guardMat;
            var enemy = node.GetComponent<EnemyBase>();
            var settings = new SerializedObject(enemy);
            var route = settings.FindProperty("waypoints"); route.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject(name + "_WP" + (i + 1)); wp.transform.SetParent(routes);
                wp.transform.position = points[i]; route.GetArrayElementAtIndex(i).objectReferenceValue = wp.transform;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            var hostile = node.GetComponent<EnemyContactDamage>();
            if (hostile == null) hostile = node.AddComponent<EnemyContactDamage>();
            Set(hostile, "dustMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_FXDust.mat"));
            var shot = node.GetComponent<ShootAbility>();
            if (shot != null)
            {
                var so = new SerializedObject(shot);
                Set(hostile, "projectilePrefab", so.FindProperty("projectilePrefab").objectReferenceValue);
                Set(hostile, "firePoint", so.FindProperty("firePoint").objectReferenceValue);
            }
            if (heavy && node.GetComponent<HeavySmashIndicator>() == null)
                node.AddComponent<HeavySmashIndicator>();
            return enemy;
        }

        private static Door Door(string name, float x, float z, float sx, float sz, Target target)
        {
            var cube = Cube(name, new Vector3(x, 3.1f, z), new Vector3(sx, 6, sz), cyanMat);
            cube.layer = 7;
            var door = cube.AddComponent<Door>();
            var so = new SerializedObject(door);
            var targets = so.FindProperty("requiredTargets"); targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
            Label(name + "_Sign", "SHOOT TARGET TO OPEN", new Vector3(x, .14f, z), .19f, Color.cyan);
            return door;
        }

        private static BreakableWall Breakable(string name, float x, float z, float sx, float sz)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Puzzle/BreakableWall.prefab");
            var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            node.name = name; node.layer = 7;
            node.transform.position = new Vector3(x, 3.1f, z);
            node.transform.localScale = new Vector3(sx, 6, sz);
            node.GetComponent<Renderer>().sharedMaterial = amberMat;
            Label(name + "_Sign", "HEAVY / SMASH WALL", new Vector3(x, .14f, z), .19f, Color.yellow);
            return node.GetComponent<BreakableWall>();
        }

        private static LevelConfig Config()
        {
            const string path = "Assets/Config/LevelConfig_Level_04.asset";
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<LevelConfig>();
            var so = new SerializedObject(config);
            so.FindProperty("levelName").stringValue = "Level_04_Local";
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(config, "possessionTimer", 10f); Set(config, "levelTime", 120f);
            Set(config, "guardDetectionRange", 10f);
            Set(config, "goldParTime", 60f); Set(config, "silverParTime", 85f); Set(config, "bronzeParTime", 110f);
            Directory.CreateDirectory("Assets/Config"); AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static HUDManager CopySharedHUD(Scene destination)
        {
            var template = EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity", OpenSceneMode.Additive);
            try
            {
                var hud = template.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HUDManager>(true)).Single();
                SceneManager.SetActiveScene(destination);
                var clone = UnityEngine.Object.Instantiate(hud.transform.root.gameObject);
                clone.name = "HUD_Canvas";
                SceneManager.MoveGameObjectToScene(clone, destination);
                var events = template.GetRootGameObjects().FirstOrDefault(root => root.GetComponent<EventSystem>() != null);
                if (events != null)
                {
                    var input = UnityEngine.Object.Instantiate(events);
                    input.name = "EventSystem";
                    SceneManager.MoveGameObjectToScene(input, destination);
                }
                return clone.GetComponentInChildren<HUDManager>(true);
            }
            finally { EditorSceneManager.CloseScene(template, true); }
        }

        private static void ConfigureObjectives(HUDManager hud, Door firstDoor, BreakableWall firstWall,
            Door secondDoor, BreakableWall secondWall)
        {
            var so = new SerializedObject(hud);
            var steps = so.FindProperty("puzzleObjectives"); steps.arraySize = 4;
            UnityEngine.Object[] gates = { firstDoor, firstWall, secondDoor, secondWall };
            string[] messages = {
                "Possess the west GUARD and shoot the north-west TARGET",
                "Possess the central HEAVY and smash the CACHE wall",
                "Bring a GUARD into the cache and shoot the RELAY target",
                "Go around the east baffle and smash the FINAL wall as HEAVY"
            };
            for (int i = 0; i < gates.Length; i++)
            {
                var step = steps.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("door").objectReferenceValue = gates[i] as Door;
                step.FindPropertyRelative("wall").objectReferenceValue = gates[i] as BreakableWall;
                step.FindPropertyRelative("instruction").stringValue = messages[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Target MakeTarget(string name, Vector3 point)
        {
            var node = Cube(name, point, new Vector3(1.5f, 1.5f, 1.5f), redMat);
            node.layer = 7;
            return node.AddComponent<Target>();
        }
        private static void Wall(string name, float x, float z, float sx, float sz)
        {
            var wall = Cube(name, new Vector3(x, 3.1f, z), new Vector3(sx, 6, sz), wallMat);
            wall.transform.SetParent(geometry);
        }
        private static void Floor(string name, float minX, float maxX, float minZ, float maxZ, Color color, float lift = 0)
        {
            var floor = Cube(name, new Vector3((minX + maxX) / 2, lift, (minZ + maxZ) / 2),
                new Vector3(maxX - minX, .2f, maxZ - minZ), Mat(name, color));
            floor.transform.SetParent(geometry);
            // Inset colour overlays do not create a second physical floor.
            if (lift > 0) UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        }
        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Cube); node.name = name;
            node.transform.position = position; node.transform.localScale = scale;
            node.GetComponent<Renderer>().sharedMaterial = material;
            return node;
        }
        private static TextMesh Label(string name, string text, Vector3 position, float size, Color color)
        {
            var node = new GameObject(name); node.transform.position = position;
            node.transform.rotation = Quaternion.Euler(90, 0, 0);
            var mesh = node.AddComponent<TextMesh>(); mesh.text = text; mesh.characterSize = size * .55f;
            mesh.fontSize = 52; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.color = color; mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.GetComponent<Renderer>().sharedMaterial = mesh.font.material;
            return mesh;
        }
        private static Material Mat(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path);
            }
            material.color = color; material.SetFloat("_Glossiness", .12f);
            EditorUtility.SetDirty(material); return material;
        }
        private static void Set(UnityEngine.Object component, string name, object value)
        {
            var settings = new SerializedObject(component);
            var property = settings.FindProperty(name);
            if (property == null) throw new InvalidOperationException(component.name + " missing field " + name);
            if (value is float f) property.floatValue = f;
            else if (value is int i) property.intValue = i;
            else if (value is Vector3 v) property.vector3Value = v;
            else property.objectReferenceValue = (UnityEngine.Object)value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
