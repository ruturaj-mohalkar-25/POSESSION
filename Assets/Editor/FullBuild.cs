using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    public static class FullBuild
    {
        [MenuItem("Possession/Full Build Level_01 %#b")]
        public static void BuildAll()
        {
            Debug.Log("=== Full Build: starting ===");

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

            Debug.Log("=== Full Build: complete ===");
        }
    }
}
