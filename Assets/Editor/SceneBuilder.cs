using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace Possession.Editor
{
    public static class SceneBuilder
    {
        private const int EnemyLayer = 6;
        private const int PuzzleLayer = 7;

        private static readonly Color FloorColor = new Color(0.3f, 0.3f, 0.32f);
        private static readonly Color WallColor = new Color(0.25f, 0.25f, 0.27f);
        private static readonly Color SpiritColor = new Color(0.9f, 0.95f, 1f, 0.8f);
        private static readonly Color GuardColor = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color HeavyColor = new Color(0.2f, 0.3f, 0.9f);
        private static readonly Color TargetColor = new Color(0.95f, 0.85f, 0.1f);
        private static readonly Color DoorColor = new Color(0.1f, 0.8f, 0.2f);
        private static readonly Color WallBreakColor = new Color(0.5f, 0.35f, 0.2f);
        private static readonly Color ExitColor = new Color(0.1f, 0.9f, 0.5f);

        [MenuItem("Possession/Build Level_01 Scene")]
        public static void BuildLevel01()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildFloorAndWalls();

            var spirit = BuildSpirit();
            var guard = BuildGuard();
            var heavy = BuildHeavy();
            var target = BuildTarget();
            var door = BuildDoor(target);
            var wall = BuildBreakableWall();
            var exit = BuildExit();

            WireGuardFields(guard);
            WireHeavyFields(heavy);
            WireSpiritFields(spirit);
            WireDoorFields(door, target);
            WireSmashFields(heavy);

            BuildCamera();
            BuildManagers();
            BuildHUD();

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            string scenePath = scenesDir + "/Level_01.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            SavePrefab(spirit, "Assets/Prefabs", "Spirit.prefab");
            SavePrefab(guard, "Assets/Prefabs/Enemies", "Guard.prefab");
            SavePrefab(heavy, "Assets/Prefabs/Enemies", "Heavy.prefab");
            SavePrefab(target, "Assets/Prefabs/Puzzle", "Target.prefab");
            SavePrefab(door, "Assets/Prefabs/Puzzle", "Door.prefab");
            SavePrefab(wall, "Assets/Prefabs/Puzzle", "BreakableWall.prefab");

            AssetDatabase.SaveAssets();
            Debug.Log("Level_01 built. Prefabs saved. Scene added to Build Settings.");
        }

        [MenuItem("Possession/Build MainMenu Scene")]
        public static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            camGO.tag = "MainCamera";

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "POSSESSION";
            titleText.fontSize = 64;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 80);
            titleRect.sizeDelta = new Vector2(600, 100);

            // Play button
            CreateMenuButton(canvasGO.transform, "Play", new Vector2(0, -20), "Level_01");

            // Quit button
            var quitGO = CreateButton(canvasGO.transform, "Quit", new Vector2(0, -100));

            // Event system
            var eventSysGO = new GameObject("EventSystem");
            eventSysGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSysGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            string scenePath = scenesDir + "/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings (MainMenu first, then Level_01)
            var existing = EditorBuildSettings.scenes;
            var newScenes = new EditorBuildSettingsScene[existing.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < existing.Length; i++)
                newScenes[i + 1] = existing[i];
            EditorBuildSettings.scenes = newScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("MainMenu built. Added to Build Settings (index 0).");
        }

        // ======================== LEVEL GEOMETRY ========================

        static void BuildLighting()
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

        static void BuildFloorAndWalls()
        {
            // Room 1: Spirit start (z: 0 to 12)
            BuildRoom("Room1", new Vector3(0, 0, 6), new Vector3(12, 0.2f, 12));

            // Room 2: Guard puzzle (z: 12 to 24), opening between rooms
            BuildRoom("Room2", new Vector3(0, 0, 18), new Vector3(12, 0.2f, 12));

            // Room 3: Heavy puzzle (z: 24 to 36)
            BuildRoom("Room3", new Vector3(0, 0, 30), new Vector3(12, 0.2f, 12));

            // Walls around perimeter
            BuildWall("Wall_Left", new Vector3(-6.5f, 1.5f, 18), new Vector3(1, 3, 36));
            BuildWall("Wall_Right", new Vector3(6.5f, 1.5f, 18), new Vector3(1, 3, 36));
            BuildWall("Wall_Back", new Vector3(0, 1.5f, -0.5f), new Vector3(14, 3, 1));
            BuildWall("Wall_Front", new Vector3(0, 1.5f, 36.5f), new Vector3(14, 3, 1));

            // Divider between Room 1 and Room 2 (with gap for passage)
            BuildWall("Divider1_Left", new Vector3(-3.5f, 1.5f, 12), new Vector3(5, 3, 0.5f));
            BuildWall("Divider1_Right", new Vector3(3.5f, 1.5f, 12), new Vector3(5, 3, 0.5f));

            // Divider between Room 2 and Room 3 — this is where the Door goes
            BuildWall("Divider2_Left", new Vector3(-3.5f, 1.5f, 24), new Vector3(5, 3, 0.5f));
            BuildWall("Divider2_Right", new Vector3(3.5f, 1.5f, 24), new Vector3(5, 3, 0.5f));
        }

        static void BuildRoom(string name, Vector3 center, Vector3 size)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name + "_Floor";
            floor.transform.position = center;
            floor.transform.localScale = size;
            floor.isStatic = true;
            SetColor(floor, FloorColor);
        }

        static void BuildWall(string name, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.isStatic = true;
            SetColor(wall, WallColor);
        }

        // ======================== GAME OBJECTS ========================

        static GameObject BuildSpirit()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Spirit";
            go.transform.position = new Vector3(0, 1.1f, 3);

            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            go.AddComponent<SpiritController>();
            go.AddComponent<PossessionController>();

            SetColor(go, SpiritColor);
            return go;
        }

        static GameObject BuildGuard()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Guard";
            go.transform.position = new Vector3(2, 1.1f, 18);
            go.layer = EnemyLayer;

            go.AddComponent<GuardEnemy>();
            go.AddComponent<ShootAbility>();

            // Waypoints
            var wp1 = new GameObject("Guard_WP1");
            wp1.transform.position = new Vector3(-3, 1.1f, 16);
            var wp2 = new GameObject("Guard_WP2");
            wp2.transform.position = new Vector3(3, 1.1f, 20);

            // Fire point
            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(go.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.5f, 0.8f);

            SetColor(go, GuardColor);
            return go;
        }

        static GameObject BuildHeavy()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Heavy";
            go.transform.position = new Vector3(-2, 1.3f, 30);
            go.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            go.layer = EnemyLayer;

            go.AddComponent<HeavyEnemy>();
            go.AddComponent<SmashAbility>();

            // Waypoints
            var wp1 = new GameObject("Heavy_WP1");
            wp1.transform.position = new Vector3(-3, 1.3f, 28);
            var wp2 = new GameObject("Heavy_WP2");
            wp2.transform.position = new Vector3(3, 1.3f, 32);

            SetColor(go, HeavyColor);
            return go;
        }

        static GameObject BuildTarget()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Target";
            go.transform.position = new Vector3(4, 1.5f, 21);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            go.layer = PuzzleLayer;

            go.AddComponent<Target>();

            SetColor(go, TargetColor);
            return go;
        }

        static GameObject BuildDoor(GameObject target)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Door";
            go.transform.position = new Vector3(0, 1.5f, 24);
            go.transform.localScale = new Vector3(2, 3, 0.5f);
            go.layer = PuzzleLayer;

            var door = go.AddComponent<Door>();

            SetColor(go, DoorColor);
            return go;
        }

        static GameObject BuildBreakableWall()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BreakableWall";
            go.transform.position = new Vector3(0, 1.5f, 33);
            go.transform.localScale = new Vector3(4, 3, 0.5f);
            go.layer = PuzzleLayer;

            go.AddComponent<BreakableWall>();

            SetColor(go, WallBreakColor);
            return go;
        }

        static GameObject BuildExit()
        {
            var go = new GameObject("ExitZone");
            go.transform.position = new Vector3(0, 1f, 35.5f);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4, 2, 1);

            // Exit door script — marked as exit so it fires LevelCompleted
            var door = go.AddComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("requiredTargets").arraySize = 0; // no targets needed, always open
            so.FindProperty("isExit").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Visual marker
            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "ExitMarker";
            marker.transform.SetParent(go.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(4, 2, 1);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            SetColor(marker, ExitColor);

            return go;
        }

        // ======================== CAMERA ========================

        static void BuildCamera()
        {
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camGO.tag = "MainCamera";

            // Fixed isometric-ish view: 45 degrees down, looking at center of level
            camGO.transform.position = new Vector3(0, 25f, 5f);
            camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            cam.fieldOfView = 40f;

            // Add AudioListener
            camGO.AddComponent<AudioListener>();
        }

        // ======================== MANAGERS ========================

        static void BuildManagers()
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        // ======================== HUD ========================

        static void BuildHUD()
        {
            var canvasGO = new GameObject("HUD_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var hud = canvasGO.AddComponent<HUDManager>();

            // Health text — top left
            var healthGO = CreateUIText(canvasGO.transform, "HealthText", "3/3",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(150, 40));

            // Level timer — top center
            var timerGO = CreateUIText(canvasGO.transform, "LevelTimerText", "5:00",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(150, 40));

            // Possession timer — top right
            var possessGO = CreateUIText(canvasGO.transform, "PossessTimerText", "",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(150, 40));
            possessGO.SetActive(false);

            // Current form — bottom left
            var formGO = CreateUIText(canvasGO.transform, "CurrentFormText", "Spirit",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(200, 40));

            // [E] POSSESS prompt — bottom center
            var promptGO = CreateUIText(canvasGO.transform, "PossessPrompt", "[E] POSSESS",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(250, 50));
            promptGO.GetComponent<Text>().fontSize = 28;
            promptGO.SetActive(false);

            // Level Complete panel
            var completePanel = CreateEndPanel(canvasGO.transform, "LevelCompletePanel", "LEVEL COMPLETE", new Color(0.1f, 0.8f, 0.2f));
            completePanel.SetActive(false);

            // Game Over panel
            var gameOverPanel = CreateEndPanel(canvasGO.transform, "GameOverPanel", "GAME OVER", new Color(0.9f, 0.15f, 0.15f));
            gameOverPanel.SetActive(false);

            // Wire serialized fields via SerializedObject
            var so = new SerializedObject(hud);
            so.FindProperty("healthText").objectReferenceValue = healthGO.GetComponent<Text>();
            so.FindProperty("levelTimerText").objectReferenceValue = timerGO.GetComponent<Text>();
            so.FindProperty("possessTimerText").objectReferenceValue = possessGO.GetComponent<Text>();
            so.FindProperty("currentFormText").objectReferenceValue = formGO.GetComponent<Text>();
            so.FindProperty("possessPrompt").objectReferenceValue = promptGO;
            so.FindProperty("levelCompletePanel").objectReferenceValue = completePanel;
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Event system
            var eventSysGO = new GameObject("EventSystem");
            eventSysGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSysGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static GameObject CreateEndPanel(Transform parent, string name, string title, Color titleColor)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // Title
            var titleGO = CreateUIText(panel.transform, "Title", title,
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(500, 80));
            titleGO.GetComponent<Text>().fontSize = 48;
            titleGO.GetComponent<Text>().color = titleColor;

            // Restart button
            CreateButton(panel.transform, "Restart", new Vector2(0, -40));

            return panel;
        }

        // ======================== WIRING ========================

        static void WireGuardFields(GameObject guard)
        {
            var wp1 = GameObject.Find("Guard_WP1");
            var wp2 = GameObject.Find("Guard_WP2");

            var enemy = guard.GetComponent<GuardEnemy>();
            var so = new SerializedObject(enemy);
            var waypoints = so.FindProperty("waypoints");
            waypoints.arraySize = 2;
            waypoints.GetArrayElementAtIndex(0).objectReferenceValue = wp1.transform;
            waypoints.GetArrayElementAtIndex(1).objectReferenceValue = wp2.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var shoot = guard.GetComponent<ShootAbility>();
            var shootSO = new SerializedObject(shoot);
            var firePoint = guard.transform.Find("FirePoint");
            shootSO.FindProperty("firePoint").objectReferenceValue = firePoint;
            shootSO.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireHeavyFields(GameObject heavy)
        {
            var wp1 = GameObject.Find("Heavy_WP1");
            var wp2 = GameObject.Find("Heavy_WP2");

            var enemy = heavy.GetComponent<HeavyEnemy>();
            var so = new SerializedObject(enemy);
            var waypoints = so.FindProperty("waypoints");
            waypoints.arraySize = 2;
            waypoints.GetArrayElementAtIndex(0).objectReferenceValue = wp1.transform;
            waypoints.GetArrayElementAtIndex(1).objectReferenceValue = wp2.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireSpiritFields(GameObject spirit)
        {
            var possession = spirit.GetComponent<PossessionController>();
            var so = new SerializedObject(possession);
            // Layer 6 = Enemy, set as LayerMask (1 << 6 = 64)
            so.FindProperty("enemyLayer").intValue = 1 << EnemyLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireDoorFields(GameObject door, GameObject target)
        {
            var doorComp = door.GetComponent<Door>();
            var so = new SerializedObject(doorComp);
            var targets = so.FindProperty("requiredTargets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = target.GetComponent<Target>();
            so.FindProperty("isExit").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The ExitZone is the actual exit, not this door
        }

        static void WireSmashFields(GameObject heavy)
        {
            var smash = heavy.GetComponent<SmashAbility>();
            var so = new SerializedObject(smash);
            // Puzzle layer = 7, set as LayerMask (1 << 7 = 128)
            so.FindProperty("smashableLayer").intValue = 1 << PuzzleLayer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ======================== HELPERS ========================

        static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        static GameObject CreateUIText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return go;
        }

        static GameObject CreateButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f);

            var btn = go.AddComponent<Button>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return go;
        }

        static GameObject CreateMenuButton(Transform parent, string label, Vector2 pos, string sceneName)
        {
            var go = CreateButton(parent, label, pos);
            var btn = go.GetComponent<Button>();
            // Scene loading wired in play mode via script
            return go;
        }

        static void SavePrefab(GameObject go, string folder, string filename)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string[] parts = folder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            string path = folder + "/" + filename;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }
    }
}
