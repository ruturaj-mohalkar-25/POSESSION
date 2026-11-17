using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class CampaignPressureSetup
    {
        [MenuItem("Possession/Level Design/Add Pressure Guards to Levels 2 and 3")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before updating room patrols.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var hunted = EditorSceneManager.OpenScene(CampaignLevels.ScenePaths[1]);
            Level02MazeBuilder.ApplyPressureGuards(hunted);
            EditorSceneManager.SaveScene(hunted);
            var warehouse = EditorSceneManager.OpenScene(CampaignLevels.ScenePaths[2]);
            WarehouseBuilder.ApplyPressureGuards(warehouse);
            EditorSceneManager.SaveScene(warehouse);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
    }
}
