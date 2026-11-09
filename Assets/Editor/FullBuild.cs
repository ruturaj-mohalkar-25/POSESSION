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

            Debug.Log("Step 1/10: Creating level configs...");
            LevelConfigAssets.CreateAll();

            Debug.Log("Step 2/10: Building Level_01 skeleton...");
            SceneBuilder.BuildLevel01();
            Level01Designer.RefineLevel01();

            Debug.Log("Step 3/10: Building Level_02 charge maze...");
            Level02MazeBuilder.Build();

            Debug.Log("Step 4/10: Building Level_03 layout...");
            if (System.IO.File.Exists("Assets/Scenes/Level_03.unity"))
                AssetDatabase.DeleteAsset("Assets/Scenes/Level_03.unity");
            BuildLevel03.Build();

            Debug.Log("Step 5/10: Updating Guard prefab...");
            GuardSetup.PrepareAssets();

            Debug.Log("Step 6/10: Placing Guards in levels...");
            GuardLevelSetup.PrepareLevels();

            Debug.Log("Step 7/10: Updating Heavy prefab + placing Heavies...");
            HeavySetup.PrepareAssets();
            HeavyLevelSetup.PrepareLevels();

            Debug.Log("Step 8/10: Applying visuals to all levels...");
            VisualSetup.ApplyVisuals();

            Debug.Log("Step 9/10: Setting up HUD in all levels...");
            HUDSetup.SetupHUD();
            HUDSetup.ApplyHUDCleanup();

            Debug.Log("Step 10/10: Building Tutorial practice level...");
            TutorialBuilder.Build();

            WarehouseCampaignSetup.Build();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("=== Full Build All Levels: complete ===");
        }
    }
}
