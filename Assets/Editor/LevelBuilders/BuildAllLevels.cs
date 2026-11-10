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
            WarehouseCampaignSetup.Build();
            Debug.Log("Built all campaign levels, including Warehouse as Level 3.");
        }
    }
}
