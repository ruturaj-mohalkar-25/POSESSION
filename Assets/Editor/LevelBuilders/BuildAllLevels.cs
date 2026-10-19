using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
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
