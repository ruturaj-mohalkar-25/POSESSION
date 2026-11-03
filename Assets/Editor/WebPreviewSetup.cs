using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Possession.Editor
{
    public static class WebPreviewSetup
    {
        [MenuItem("Possession/Prepare Combined Web Preview")]
        public static void Prepare()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true)
            };
            foreach (string name in LevelFlow.CampaignScenes)
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (!File.Exists(path)) throw new System.IO.FileNotFoundException(path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            TutorialBuildRegistration.Ensure();
            HUDSetup.SetupMainMenu();
            AssetDatabase.SaveAssets();
        }
    }
}
