using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession.Level04Gold.Editor
{
    public static class Level4PresentationSetup
    {
        [MenuItem("Possession/Level 4 Gold/Match Campaign Presentation")]
        public static void MatchCampaign()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before updating Level 4 presentation.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Level4GoldBuilder.ScenePath);
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var hud = roots.SelectMany(r => r.GetComponentsInChildren<HUDManager>(true)).Single();
            Possession.Editor.HUDSetup.StyleHUD(hud);
            var canvas = hud.GetComponent<Canvas>();
            if (canvas != null) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
            }
            var settings = new SerializedObject(hud);
            if (settings.FindProperty("objectiveText").objectReferenceValue is Text objective)
                objective.transform.parent.gameObject.SetActive(true);
            var gameplay = roots.Single(r => r.name == "Level04_Gameplay");
            if (gameplay.GetComponent<Level4WorldPresentation>() == null)
                gameplay.AddComponent<Level4WorldPresentation>();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
