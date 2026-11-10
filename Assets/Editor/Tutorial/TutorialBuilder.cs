using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class TutorialBuilder
    {
        [MenuItem("Possession/Tutorial/Build or Rebuild Practice Level")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Practice level", "Stop Play Mode before rebuilding the practice level.", "OK");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity", OpenSceneMode.Single);
            if (!File.Exists(TutorialScene.Path)) AssetDatabase.CopyAsset("Assets/Scenes/Level_01.unity", TutorialScene.Path);
            else File.Copy("Assets/Scenes/Level_01.unity", TutorialScene.Path, true);
            AssetDatabase.ImportAsset(TutorialScene.Path, ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(TutorialScene.Path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var spirit = Find<SpiritController>(roots);
            var possession = spirit.GetComponent<PossessionController>();
            var guard = Find<GuardEnemy>(roots);
            var heavy = Find<HeavyEnemy>(roots);
            var target = Find<Target>(roots);
            var wall = Find<BreakableWall>(roots);
            var exit = Find<LevelExit>(roots);
            var manager = Find<GameManager>(roots);
            var config = MakeConfig();
            LevelBuildKit.ApplyConfig(config, manager, spirit.gameObject);
            Set(possession, "possessRange", 5f);
            Set(possession, "hostileTime", 5f);
            BuildCompactLayout(roots, spirit, guard, heavy, target, wall, exit, Find<Door>(roots));

            var hud = Find<HUDManager>(roots);
            var hudSettings = new SerializedObject(hud);
            foreach (string name in new[] { "levelCompletePanel", "gameOverPanel", "gameCompletePanel", "messageText" })
            {
                var property = hudSettings.FindProperty(name);
                if (property.objectReferenceValue is GameObject go) go.SetActive(false);
                else if (property.objectReferenceValue is Component component) component.gameObject.SetActive(false);
                property.objectReferenceValue = null;
            }
            var possessPrompt = Named(roots, "PossessPrompt");
            if (possessPrompt != null)
            {
                possessPrompt.SetActive(false);
                hudSettings.FindProperty("possessPrompt").objectReferenceValue = possessPrompt;
            }
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            foreach (var root in roots.Where(r => r != null))
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name == "ControlsPanel" || transform.name == "CurrentFormText")
                    transform.gameObject.SetActive(false);
            int enabledEnemyLabels = 0;
            foreach (var label in roots.Where(r => r != null).SelectMany(r => r.GetComponentsInChildren<WorldLabel>(true)))
            {
                var follow = new SerializedObject(label).FindProperty("follow").objectReferenceValue as Transform;
                if (follow == null || follow.GetComponent<EnemyBase>() == null) continue;
                label.gameObject.SetActive(true);
                EditorUtility.SetDirty(label.gameObject);
                enabledEnemyLabels++;
            }
            LegacyFloorTextCleanup.Apply(scene);
            var guide = new GameObject("TutorialGuide").AddComponent<TutorialGuide>();
            var so = new SerializedObject(guide);
            so.FindProperty("spirit").objectReferenceValue = spirit;
            so.FindProperty("possession").objectReferenceValue = possession;
            so.FindProperty("guard").objectReferenceValue = guard;
            so.FindProperty("heavy").objectReferenceValue = heavy;
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("wall").objectReferenceValue = wall;
            so.FindProperty("exitTrigger").objectReferenceValue = exit.GetComponent<BoxCollider>();
            so.FindProperty("manager").objectReferenceValue = manager;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("door").objectReferenceValue = Find<Door>(roots);
            so.FindProperty("healthBar").objectReferenceValue = (hudSettings.FindProperty("healthFill").objectReferenceValue as UnityEngine.UI.Image).transform.parent as RectTransform;
            so.FindProperty("healthText").objectReferenceValue = (hudSettings.FindProperty("healthText").objectReferenceValue as UnityEngine.UI.Text).rectTransform;
            so.FindProperty("levelClock").objectReferenceValue = (hudSettings.FindProperty("levelTimerText").objectReferenceValue as UnityEngine.UI.Text).rectTransform;
            so.FindProperty("possessionClock").objectReferenceValue = (hudSettings.FindProperty("possessTimerGroup").objectReferenceValue as GameObject).transform as RectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
            exit.GetComponent<BoxCollider>().enabled = true;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TutorialScene.Path);
            TutorialBuildRegistration.Ensure();
            AssetDatabase.SaveAssets();
            Debug.Log("Tutorial ready: open MainMenu and press Play → HOW TO PLAY, or open Assets/Scenes/Tutorial.unity.");
        }

        [MenuItem("Possession/Tutorial/Open Practice Level")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(TutorialScene.Path);
        }

        private static LevelConfig MakeConfig()
        {
            const string path = "Assets/Config/LevelConfig_Tutorial.asset";
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            Set(config, "levelName", TutorialScene.Name);
            Set(config, "levelTime", 150f);
            Set(config, "possessionTimer", 12f);
            Set(config, "guardMoveSpeed", 2.2f);
            Set(config, "guardDetectionRange", 5.5f);
            return config;
        }

        private static void BuildCompactLayout(GameObject[] roots, SpiritController spirit, GuardEnemy guard,
            HeavyEnemy heavy, Target target, BreakableWall wall, LevelExit exit, Door door)
        {
            Material floor = Named(roots, "Room1_Floor")?.GetComponent<Renderer>()?.sharedMaterial;
            Material walls = Named(roots, "Wall_Left")?.GetComponent<Renderer>()?.sharedMaterial;
            foreach (string name in new[] { "Room1_Floor", "Room2_Floor", "Room3_Floor", "Wall_Back", "Wall_Front",
                "Wall_Left", "Wall_Right", "Divider1_Left", "Divider1_Right", "Divider2_Left", "Divider2_Right",
                "WallPuzzle_Left", "WallPuzzle_Right" })
            {
                var old = Named(roots, name);
                if (old != null) Object.DestroyImmediate(old);
            }

            var environment = new GameObject("CompactTutorialLayout").transform;
            Box(environment, "Bay1_Floor", new Vector3(0f, -0.25f, 4.5f), new Vector3(14f, 0.5f, 9f), floor);
            Box(environment, "Connector_Floor", new Vector3(4f, -0.25f, 9.5f), new Vector3(4f, 0.5f, 1f), floor);
            Box(environment, "Bay2_Floor", new Vector3(4f, -0.25f, 14.5f), new Vector3(14f, 0.5f, 9f), floor);
            Box(environment, "Exit_Floor", new Vector3(4f, -0.25f, 20.2f), new Vector3(4f, 0.5f, 2f), floor);

            Box(environment, "Bay1_South", new Vector3(0f, 1.5f, -0.25f), new Vector3(14.5f, 3f, 0.5f), walls);
            Box(environment, "Bay1_West", new Vector3(-7.25f, 1.5f, 4.5f), new Vector3(0.5f, 3f, 9f), walls);
            Box(environment, "Bay1_East", new Vector3(7.25f, 1.5f, 4.5f), new Vector3(0.5f, 3f, 9f), walls);
            Box(environment, "Bay1_NorthLeft", new Vector3(-2.5f, 1.5f, 9.25f), new Vector3(9.5f, 3f, 0.5f), walls);
            Box(environment, "Bay1_NorthRight", new Vector3(6.75f, 1.5f, 9.25f), new Vector3(1.5f, 3f, 0.5f), walls);

            Box(environment, "Bay2_SouthLeft", new Vector3(-0.5f, 1.5f, 9.75f), new Vector3(5f, 3f, 0.5f), walls);
            Box(environment, "Bay2_SouthRight", new Vector3(8.5f, 1.5f, 9.75f), new Vector3(5f, 3f, 0.5f), walls);
            Box(environment, "Bay2_West", new Vector3(-3.25f, 1.5f, 14.5f), new Vector3(0.5f, 3f, 9f), walls);
            Box(environment, "Bay2_East", new Vector3(11.25f, 1.5f, 14.5f), new Vector3(0.5f, 3f, 9f), walls);
            Box(environment, "Bay2_NorthLeft", new Vector3(-0.35f, 1.5f, 19.25f), new Vector3(5.8f, 3f, 0.5f), walls);
            Box(environment, "Bay2_NorthRight", new Vector3(8.35f, 1.5f, 19.25f), new Vector3(5.8f, 3f, 0.5f), walls);
            Box(environment, "Exit_West", new Vector3(1.75f, 1.5f, 20.2f), new Vector3(0.5f, 3f, 2f), walls);
            Box(environment, "Exit_East", new Vector3(6.25f, 1.5f, 20.2f), new Vector3(0.5f, 3f, 2f), walls);

            spirit.transform.position = new Vector3(0f, 1.15f, 0.4f);
            guard.transform.SetPositionAndRotation(new Vector3(0f, 1.1f, 5.5f), Quaternion.identity);
            guard.MoveSpeed = 2.2f;
            guard.ConfigureHomeBounds(new Bounds(new Vector3(0f, 1f, 4.5f), new Vector3(13f, 4f, 8f)));
            Set(guard, "detectionRange", 5.5f);
            PlaceWaypoints(roots, "Guard_WP", new Vector3(-1.4f, 1.1f, 5.5f), new Vector3(1.4f, 1.1f, 5.5f));

            heavy.transform.SetPositionAndRotation(new Vector3(4f, 1.3f, 14.5f), Quaternion.identity);
            heavy.MoveSpeed = 1.3f;
            heavy.ConfigureHomeBounds(new Bounds(new Vector3(4f, 1f, 14.5f), new Vector3(13f, 4f, 8f)));
            Set(heavy, "detectionRange", 5f);
            PlaceWaypoints(roots, "Heavy_WP", new Vector3(2.8f, 1.3f, 14.5f), new Vector3(5.2f, 1.3f, 14.5f));

            target.transform.position = new Vector3(5.7f, 1.5f, 6.8f);
            target.transform.localScale = new Vector3(1.5f, 1.35f, 0.55f);
            door.transform.position = new Vector3(4f, door.transform.position.y, 9.5f);
            wall.transform.position = new Vector3(4f, wall.transform.position.y, 19f);
            exit.transform.position = new Vector3(4f, exit.transform.position.y, 20.3f);

            var pickups = roots.Where(r => r != null)
                .SelectMany(r => r.GetComponentsInChildren<PossessionChargePickup>(true)).ToArray();
            if (pickups.Length > 0) ConfigurePickup(pickups[0], new Vector3(4f, 0.1f, 11.5f));
            if (pickups.Length > 1) ConfigurePickup(pickups[1], new Vector3(8.2f, 0.1f, 15.5f));
        }

        private static void ConfigurePickup(PossessionChargePickup pickup, Vector3 position)
        {
            pickup.transform.position = position;
            Set(pickup, "reforms", false);
            Set(pickup, "respawnInRoom", false);
        }

        private static void PlaceWaypoints(GameObject[] roots, string prefix, Vector3 first, Vector3 second)
        {
            foreach (var waypoint in roots.Where(r => r != null)
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith(prefix)))
                waypoint.position = waypoint.name.EndsWith("1") ? first : second;
        }

        private static GameObject Named(GameObject[] roots, string name) => roots
            .Where(r => r != null).SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name)?.gameObject;

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static T Find<T>(GameObject[] roots) where T : Component =>
            roots.Where(r => r != null).SelectMany(r => r.GetComponentsInChildren<T>(true)).First();
        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (value is float number) prop.floatValue = number;
            else if (value is string text) prop.stringValue = text;
            else if (value is bool flag) prop.boolValue = flag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
