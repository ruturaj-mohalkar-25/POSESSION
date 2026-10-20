using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    public static class FullBuild
    {
        [MenuItem("Possession/Full Build Level_01 %#b")]
        public static void BuildLevel01()
        {
            Debug.Log("=== Full Build Level_01: starting ===");

            Debug.Log("Step 1/5: Building skeleton scene...");
            SceneBuilder.BuildLevel01();

            Debug.Log("Step 2/5: Refining level layout (P4)...");
            Level01Designer.RefineLevel01();

            Debug.Log("Step 3/5: Updating Guard component (P1)...");
            GuardSetup.PrepareAssets();

            Debug.Log("Step 4/5: Updating Heavy component (P3)...");
            HeavySetup.PrepareAssets();

            Debug.Log("Step 5/5: Applying visuals and HUD (P5)...");
            VisualSetup.ApplyVisuals();
            HUDSetup.SetupHUD();

            Debug.Log("=== Full Build Level_01: complete ===");
        }

        [MenuItem("Possession/Full Build All Levels")]
        public static void BuildAllLevels()
        {
            Debug.Log("=== Full Build All Levels: starting ===");

            Debug.Log("Step 1/6: Building Level_01 skeleton...");
            SceneBuilder.BuildLevel01();
            Level01Designer.RefineLevel01();

            Debug.Log("Step 2/6: Building Level_02 and Level_03 layouts (P4)...");
            LevelConfigAssets.CreateAll();
            foreach (string path in new[] { "Assets/Scenes/Level_02.unity", "Assets/Scenes/Level_03.unity" })
                if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
            BuildLevel02.Build();
            BuildLevel03.Build();

            Debug.Log("Step 3/6: Updating Guard prefab (P1)...");
            GuardSetup.PrepareAssets();

            Debug.Log("Step 4/6: Updating Heavy prefab + placing Heavies (P3)...");
            HeavySetup.PrepareAssets();
            HeavyLevelSetup.PrepareLevels();

            Debug.Log("Step 5/6: Applying visuals to all levels (P5)...");
            VisualSetup.ApplyVisuals();

            Debug.Log("Step 6/6: Setting up HUD in all levels (P5)...");
            HUDSetup.SetupHUD();

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("=== Full Build All Levels: complete ===");
        }
    }
}
