using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    // Shared graybox helpers for the P4 level builders.
    // Mirrors the Level_01 conventions set by SceneBuilder and Level01Designer:
    // 12 x 12 rooms running along +Z, 6 m walls, a 3.2 m opening in every divider,
    // layer 6 = Enemy, layer 7 = Puzzle, targets at the Guard's FirePoint height.
    public static class LevelBuildKit
    {
        public const int EnemyLayer = 6;
        public const int PuzzleLayer = 7;

        public const float RoomSize = 12f;        // square room, matches Room1..Room3
        public const float FloorThickness = 0.2f;
        public const float WallHeight = 6f;       // tall enough that the Spirit cannot jump a puzzle
        public const float WallY = 3f;            // wall centre for a 6 m wall standing on y = 0
        public const float WallThickness = 0.5f;
        public const float Opening = 3.2f;        // Heavy fits through, Spirit can eject safely
        public const float FirePointHeight = 1.6f; // projectiles fly at the Guard's FirePoint height
        public const float SpiritHeight = 1.15f;
        public const float SpawnHeight = 1.1f;

        public const string SpiritPrefab = "Assets/Prefabs/Spirit.prefab";
        public const string TargetPrefab = "Assets/Prefabs/Puzzle/Target.prefab";
        public const string DoorPrefab = "Assets/Prefabs/Puzzle/Door.prefab";
        public const string BreakableWallPrefab = "Assets/Prefabs/Puzzle/BreakableWall.prefab";

        static readonly Color FloorColor = new Color(0.3f, 0.3f, 0.32f);
        static readonly Color WallColor = new Color(0.25f, 0.25f, 0.27f);
        static readonly Color ExitColor = new Color(0.1f, 0.9f, 0.5f);

        // ======================== SCENE BASICS ========================

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
            camGO.AddComponent<CameraFollow>(); // picks up the Spirit at runtime
        }

        public static GameManager BuildGameManager()
        {
            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        public static GameObject BuildSpirit(Vector3 position)
        {
            var spirit = Instantiate(SpiritPrefab, "Spirit", position, Vector3.one);
            var possession = spirit.GetComponent<PossessionController>();
            if (possession != null)
            {
                var so = new SerializedObject(possession);
                so.FindProperty("enemyLayer").intValue = 1 << EnemyLayer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return spirit;
        }

        // ======================== GEOMETRY ========================

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

        // A divider across the level with a centred gap of `Opening` metres.
        // The gap is where a Door or BreakableWall is dropped in.
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

        // ======================== PUZZLE PIECES ========================

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
            // Sized and raised like Level_01's target so a Guard's shot can actually land on it.
            var go = Instantiate(TargetPrefab, name, new Vector3(x, FirePointHeight, z),
                new Vector3(1.2f, 1.2f, 0.6f), parent);
            go.layer = PuzzleLayer;
            return go.GetComponent<Target>();
        }

        public static Door PlaceDoor(Transform parent, string name, float z, IEnumerable<Target> requiredTargets)
        {
            var go = Instantiate(DoorPrefab, name, new Vector3(0f, WallY, z),
                new Vector3(Opening, WallHeight, WallThickness), parent);
            go.layer = PuzzleLayer;

            var door = go.GetComponent<Door>();
            var so = new SerializedObject(door);
            var targets = so.FindProperty("requiredTargets");
            var list = new List<Target>(requiredTargets);
            targets.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                targets.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.FindProperty("isExit").boolValue = false; // LevelExit owns completion
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        public static BreakableWall PlaceBreakableWall(Transform parent, string name, float z)
        {
            var go = Instantiate(BreakableWallPrefab, name, new Vector3(0f, WallY, z),
                new Vector3(Opening, WallHeight, WallThickness), parent);
            go.layer = PuzzleLayer;
            return go.GetComponent<BreakableWall>();
        }

        // Exit trigger, built the way Level_01's ExitZone ends up after the P4 refine pass.
        public static LevelExit PlaceExit(Transform parent, string name, Vector3 position,
            Door puzzleDoor, BreakableWall breakableWall)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            var exit = go.AddComponent<LevelExit>(); // RequireComponent adds the BoxCollider
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

        // Empty marker for P1/P3 to drop a Guard or Heavy onto.
        public static GameObject SpawnPoint(Transform parent, string name, float x, float z, float y = SpawnHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, z);
            go.layer = EnemyLayer;
            return go;
        }

        // ======================== LABELS ========================

        // Flat sign painted on the floor, same style as Level_01's StartInstructions.
        public static GameObject FloorSign(Transform parent, string name, string text, Vector3 position,
            float characterSize = 0.055f)
        {
            var sign = new GameObject(name);
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(position.x, 0.13f, position.z);
            sign.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var label = sign.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = characterSize;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            return sign;
        }

        // Billboard tag that follows an object and flips to `doneText` when it is solved,
        // reusing the existing WorldLabel component rather than a new one.
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

        // ======================== DIFFICULTY + BUILD SETTINGS ========================

        // Writes the config's values onto the scene components that own them in Level_01.
        public static void ApplyConfig(LevelConfig config, GameManager manager, GameObject spirit)
        {
            if (config == null) return;

            if (manager != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("levelTime").floatValue = config.LevelTime;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var possession = spirit != null ? spirit.GetComponent<PossessionController>() : null;
            if (possession != null)
            {
                var so = new SerializedObject(possession);
                so.FindProperty("possessDuration").floatValue = config.PossessionTimer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // No Guards exist yet (P1 places them), so this only bites once they are in the scene.
            foreach (var guard in Object.FindObjectsByType<GuardEnemy>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(guard);
                so.FindProperty("detectionRange").floatValue = config.GuardDetectionRange;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Appends the scene to Build Settings, keeping MainMenu and Level_01 ahead of it.
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
