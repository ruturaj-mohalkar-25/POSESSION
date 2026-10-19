using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    /// <summary>
    /// Convenience entry point: writes the level configs and builds both P4 levels in
    /// one click, in the order they depend on each other. Equivalent to running
    /// "Create or Update Level Configs", "Build Level_02" and "Build Level_03" by hand.
    /// </summary>
    public static class BuildAllLevels
    {
        [MenuItem("Possession/P4/Build All Levels")]
        public static void BuildAll()
        {
            LevelConfigAssets.CreateAll();
            BuildLevel02.Build();
            BuildLevel03.Build();
            Debug.Log("P4: built all levels (configs + Level_02 + Level_03).");
        }
    }
}
