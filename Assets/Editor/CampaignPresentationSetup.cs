using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession.Editor
{
    public static class CampaignPresentationSetup
    {
        [MenuItem("Possession/Level Design/Sync Campaign HUD with Level 1")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before updating campaign HUDs.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        public static void Apply(Scene scene)
        {
            if (!CampaignLevels.SceneNames.Contains(scene.name)) return;
            var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HUDManager>(true)).Single();
            HUDSetup.StyleHUD(hud);
            var canvas = hud.GetComponent<Canvas>();
            var scaler = hud.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
            }
            if (canvas != null) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var so = new SerializedObject(hud);
            if (so.FindProperty("objectiveText").objectReferenceValue is Text objective)
                objective.transform.parent.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
