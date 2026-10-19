using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    public static class LevelConfigAssets
    {
        public const string Folder = "Assets/Config";

        [MenuItem("Possession/P4/Create or Update Level Configs")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "Config");

            Write("LevelConfig_Level_01", "Level_01", 12f, 300f, 10f, 100f, 150f, 225f);
            Write("LevelConfig_Level_02", "Level_02", 10f, 240f, 12f, 80f, 120f, 180f);
            Write("LevelConfig_Level_03", "Level_03", 8f, 180f, 14f, 60f, 90f, 135f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("P4: Level configs written to " + Folder);
        }

        public static LevelConfig Load(string levelName)
        {
            return AssetDatabase.LoadAssetAtPath<LevelConfig>(Folder + "/LevelConfig_" + levelName + ".asset");
        }

        static void Write(string assetName, string levelName, float possessionTimer, float levelTime,
            float guardDetectionRange, float gold, float silver, float bronze)
        {
            string path = Folder + "/" + assetName + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            bool isNew = config == null;
            if (isNew) config = ScriptableObject.CreateInstance<LevelConfig>();

            var so = new SerializedObject(config);
            so.FindProperty("levelName").stringValue = levelName;
            so.FindProperty("possessionTimer").floatValue = possessionTimer;
            so.FindProperty("levelTime").floatValue = levelTime;
            so.FindProperty("guardDetectionRange").floatValue = guardDetectionRange;
            so.FindProperty("goldParTime").floatValue = gold;
            so.FindProperty("silverParTime").floatValue = silver;
            so.FindProperty("bronzeParTime").floatValue = bronze;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew) AssetDatabase.CreateAsset(config, path);
            else EditorUtility.SetDirty(config);
        }
    }
}
