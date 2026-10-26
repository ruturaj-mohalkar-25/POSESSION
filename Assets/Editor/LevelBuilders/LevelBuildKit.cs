using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    public static class LevelBuildKit
    {
        public const int EnemyLayer = 6;
        public const int PuzzleLayer = 7;

        public const float RoomSize = 12f;
        public const float FloorThickness = 0.2f;
        public const float WallHeight = 6f;
        public const float WallY = 3f;
        public const float WallThickness = 0.5f;
        public const float Opening = 3.2f;
        public const float FirePointHeight = 1.6f;
        public const float SpiritHeight = 1.15f;
        public const float SpawnHeight = 1.1f;

        public const string SpiritPrefab = "Assets/Prefabs/Spirit.prefab";
        public const string GuardPrefab = "Assets/Prefabs/Enemies/Guard.prefab";
        public const string TargetPrefab = "Assets/Prefabs/Puzzle/Target.prefab";
        public const string DoorPrefab = "Assets/Prefabs/Puzzle/Door.prefab";
        public const string BreakableWallPrefab = "Assets/Prefabs/Puzzle/BreakableWall.prefab";

        static readonly Color FloorColor = new Color(0.3f, 0.3f, 0.32f);
        static readonly Color WallColor = new Color(0.25f, 0.25f, 0.27f);
        static readonly Color ExitColor = new Color(0.1f, 0.9f, 0.5f);
        static readonly Color TargetColor = new Color(0.85f, 0.3f, 0.25f);
        static readonly Color DoorColor = new Color(0.45f, 0.45f, 0.55f);
        static readonly Color WallBreakColor = new Color(0.5f, 0.5f, 0.5f);
        static readonly Color SpiritColor = new Color(0.6f, 0.8f, 1f);
        static readonly Color GuardColor = new Color(0.15f, 0.45f, 0.85f);

        public static void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);

            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        public static void BuildCamera()
        {
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            cam.fieldOfView = 40f;
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 25f, -10f);
            camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CameraFollow>();
        }

        public static GameManager BuildGameManager()
        {
            var go = new GameObject("GameManager");
            var manager = go.AddComponent<GameManager>();
            go.AddComponent<AnalyticsTracker>();
            return manager;
        }

        public static GameObject BuildSpirit(Vector3 position)
        {
            var spirit = Instantiate(SpiritPrefab, "Spirit", position, Vector3.one);
            SetColor(spirit, SpiritColor);
            var possession = spirit.GetComponent<PossessionController>();
            if (possession != null)
            {
                var so = new SerializedObject(possession);
                so.FindProperty("enemyLayer").intValue = 1 << EnemyLayer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return spirit;
        }

        public static GameObject Floor(Transform parent, string name, Vector3 centre, float width, float depth)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent);
            floor.transform.position = new Vector3(centre.x, 0f, centre.z);
            floor.transform.localScale = new Vector3(width, FloorThickness, depth);
            floor.isStatic = true;
            SetColor(floor, FloorColor);
            return floor;
        }

        public static GameObject FloorRect(Transform parent, string name, float minX, float maxX, float minZ, float maxZ)
        {
            var centre = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            return Floor(parent, name, centre, maxX - minX, maxZ - minZ);
        }

        public static GameObject WallRect(Transform parent, string name, float minX, float maxX, float minZ, float maxZ)
        {
            var position = new Vector3((minX + maxX) * 0.5f, WallY, (minZ + maxZ) * 0.5f);
            var scale = new Vector3(maxX - minX, WallHeight, maxZ - minZ);
            return Wall(parent, name, position, scale);
        }

        public static GameObject Wall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.isStatic = true;
            SetColor(wall, WallColor);
            return wall;
        }

        public static void DividerWithGap(Transform parent, string namePrefix, float z, float levelWidth)
        {
            float halfWidth = levelWidth * 0.5f;
            float segment = halfWidth - Opening * 0.5f;
            float offset = Opening * 0.5f + segment * 0.5f;

            Wall(parent, namePrefix + "_Left", new Vector3(-offset, WallY, z),
                new Vector3(segment, WallHeight, WallThickness));
            Wall(parent, namePrefix + "_Right", new Vector3(offset, WallY, z),
                new Vector3(segment, WallHeight, WallThickness));
        }

        public static void Perimeter(Transform parent, float levelWidth, float minZ, float maxZ)
        {
            float halfWidth = levelWidth * 0.5f;
            float depth = maxZ - minZ;
            float centreZ = (minZ + maxZ) * 0.5f;

            Wall(parent, "Wall_Left", new Vector3(-halfWidth - 0.5f, WallY, centreZ),
                new Vector3(1f, WallHeight, depth + 1f));
            Wall(parent, "Wall_Right", new Vector3(halfWidth + 0.5f, WallY, centreZ),
                new Vector3(1f, WallHeight, depth + 1f));
            Wall(parent, "Wall_Back", new Vector3(0f, WallY, minZ - 0.5f),
                new Vector3(levelWidth + 2f, WallHeight, 1f));
            Wall(parent, "Wall_Front", new Vector3(0f, WallY, maxZ + 0.5f),
                new Vector3(levelWidth + 2f, WallHeight, 1f));
        }

        public static GameObject Instantiate(string prefabPath, string name, Vector3 position, Vector3 scale,
            Transform parent = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException("Missing prefab: " + prefabPath);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            if (parent != null) instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            return instance;
        }

        public static Target PlaceTarget(Transform parent, string name, float x, float z)
        {
            var go = Instantiate(TargetPrefab, name, new Vector3(x, FirePointHeight, z),
                new Vector3(1.2f, 1.2f, 0.6f), parent);
            go.layer = PuzzleLayer;
            SetColor(go, TargetColor);
            return go.GetComponent<Target>();
        }

        public static Door PlaceDoor(Transform parent, string name, float x, float z, IEnumerable<Target> requiredTargets)
        {
            var go = Instantiate(DoorPrefab, name, new Vector3(x, WallY, z),
                new Vector3(Opening, WallHeight, WallThickness), parent);
            go.layer = PuzzleLayer;
            SetColor(go, DoorColor);

            var door = go.GetComponent<Door>();
            var so = new SerializedObject(door);
            var targets = so.FindProperty("requiredTargets");
            var list = new List<Target>(requiredTargets);
            targets.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                targets.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.FindProperty("isExit").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        public static BreakableWall PlaceBreakableWall(Transform parent, string name, float x, float z)
        {
            var go = Instantiate(BreakableWallPrefab, name, new Vector3(x, WallY, z),
                new Vector3(Opening, WallHeight, WallThickness), parent);
            go.layer = PuzzleLayer;
            SetColor(go, WallBreakColor);
            return go.GetComponent<BreakableWall>();
        }

        public static LevelExit PlaceExit(Transform parent, string name, Vector3 position,
            Door puzzleDoor, BreakableWall breakableWall)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            var exit = go.AddComponent<LevelExit>();
            var collider = go.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 2f, 1f);

            var so = new SerializedObject(exit);
            so.FindProperty("puzzleDoor").objectReferenceValue = puzzleDoor;
            so.FindProperty("breakableWall").objectReferenceValue = breakableWall;
            so.ApplyModifiedPropertiesWithoutUndo();

            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "ExitMarker";
            marker.transform.SetParent(go.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(4f, 2f, 1f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            SetColor(marker, ExitColor);

            return exit;
        }

        public static GameObject SpawnPoint(Transform parent, string name, float x, float z, float y = SpawnHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, z);
            go.layer = EnemyLayer;

            var marker = go.AddComponent<SpawnPointMarker>();
            if (name.Contains("Guard")) marker.SetKind(SpawnPointMarker.SpawnKind.Guard);
            else if (name.Contains("Heavy")) marker.SetKind(SpawnPointMarker.SpawnKind.Heavy);
            else marker.SetKind(SpawnPointMarker.SpawnKind.Other);

            return go;
        }

        public static GuardEnemy PlaceGuard(Transform parent, Transform routes, string name,
            Vector3 position, float patrolStartDelay, params Vector3[] waypointPositions)
        {
            var go = Instantiate(GuardPrefab, name, position, Vector3.one, parent);
            go.layer = EnemyLayer;
            SetColor(go, GuardColor);

            var guard = go.GetComponent<GuardEnemy>();
            if (guard == null)
                throw new System.InvalidOperationException("Guard prefab is missing GuardEnemy.");

            var so = new SerializedObject(guard);
            so.FindProperty("patrolStartDelay").floatValue = patrolStartDelay;
            var waypoints = so.FindProperty("waypoints");
            waypoints.arraySize = waypointPositions.Length;
            for (int i = 0; i < waypointPositions.Length; i++)
            {
                var waypoint = new GameObject(name + "_WP" + (i + 1));
                waypoint.transform.SetParent(routes);
                waypoint.transform.position = waypointPositions[i];
                waypoints.GetArrayElementAtIndex(i).objectReferenceValue = waypoint.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return guard;
        }

        public static WorldLabel ObjectLabel(Transform parent, string text, GameObject follow, Vector3 offset,
            Color color, string doneText = "")
        {
            if (follow == null) return null;

            var go = new GameObject("Label_" + follow.name);
            go.transform.SetParent(parent, false);
            go.transform.position = follow.transform.position + offset;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = 0.1f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            var label = go.AddComponent<WorldLabel>();
            label.Init(follow.transform, offset, follow.GetComponent<Target>(), follow.GetComponent<Door>(),
                doneText, null);
            return label;
        }

        public static void ApplyConfig(LevelConfig config, GameManager manager, GameObject spirit)
        {
            if (config == null) return;

            if (manager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("levelTime").floatValue = config.LevelTime;

                var thresholds = so.FindProperty("starThresholds");
                thresholds.arraySize = 3;
                thresholds.GetArrayElementAtIndex(0).floatValue = config.GoldParTime;
                thresholds.GetArrayElementAtIndex(1).floatValue = config.SilverParTime;
                thresholds.GetArrayElementAtIndex(2).floatValue = config.BronzeParTime;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var possession = spirit != null ? spirit.GetComponent<PossessionController>() : null;
            if (possession != null)
            {
                var so = new SerializedObject(possession);
                so.FindProperty("possessDuration").floatValue = config.PossessionTimer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var guard in Object.FindObjectsByType<GuardEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(guard);
                so.FindProperty("moveSpeed").floatValue = config.GuardMoveSpeed;
                so.FindProperty("maxHP").intValue = config.GuardMaxHP;
                so.FindProperty("detectionRange").floatValue = config.GuardDetectionRange;
                so.FindProperty("waypointWaitTime").floatValue = config.GuardWaypointWaitTime;
                so.FindProperty("chaseStopDistance").floatValue = config.GuardChaseStopDistance;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void RegisterScene(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath))
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            scenes.Sort((a, b) => SceneOrder(a.path).CompareTo(SceneOrder(b.path)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static int SceneOrder(string path)
        {
            if (path.EndsWith("MainMenu.unity")) return 0;
            if (path.EndsWith("Level_01.unity")) return 1;
            if (path.EndsWith("Level_02.unity")) return 2;
            if (path.EndsWith("Level_03.unity")) return 3;
            return 4;
        }

        public static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
