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
            Set(possession, "possessRange", 7f);
            Set(possession, "hostileTime", 5f);
            spirit.transform.position = new Vector3(0f, 1.15f, 3.5f);
            guard.transform.position = new Vector3(0f, 1.1f, 16f);
            Set(guard, "detectionRange", 4f);
            foreach (var waypoint in roots.Where(r => r.name.StartsWith("Guard_WP")))
                waypoint.transform.position = new Vector3(waypoint.name.EndsWith("1") ? -2f : 2f, 1.1f, 16f);
            Set(heavy, "detectionRange", 4f);
            target.transform.position = new Vector3(2.8f, 1.6f, 20.5f);
            // A slightly wider board makes the first shooting lesson forgiving.
            target.transform.localScale = new Vector3(1.6f, 1.4f, 0.6f);

            var hud = Find<HUDManager>(roots);
            var hudSettings = new SerializedObject(hud);
            foreach (string name in new[] { "levelCompletePanel", "gameOverPanel", "gameCompletePanel", "possessPrompt", "messageText" })
            {
                var property = hudSettings.FindProperty(name);
                if (property.objectReferenceValue is GameObject go) go.SetActive(false);
                else if (property.objectReferenceValue is Component component) component.gameObject.SetActive(false);
                property.objectReferenceValue = null;
            }
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
            foreach (var root in roots)
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name == "ObjectiveBar" || transform.name == "ControlsPanel"
                    || transform.name == "CurrentFormText") transform.gameObject.SetActive(false);
            foreach (var label in roots.SelectMany(r => r.GetComponentsInChildren<WorldLabel>(true)))
            {
                var follow = new SerializedObject(label).FindProperty("follow").objectReferenceValue as Transform;
                if (follow != null && follow.GetComponent<EnemyBase>() != null) label.gameObject.SetActive(false);
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
            Set(config, "levelTime", 300f);
            Set(config, "possessionTimer", 12f);
            Set(config, "guardMoveSpeed", 2.6f);
            Set(config, "guardDetectionRange", 4f);
            return config;
        }

        private static T Find<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).First();
        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (value is float number) prop.floatValue = number;
            else if (value is string text) prop.stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
