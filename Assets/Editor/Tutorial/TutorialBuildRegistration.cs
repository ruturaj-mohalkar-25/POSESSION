using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Possession.Editor
{
    [InitializeOnLoad]
    public static class TutorialBuildRegistration
    {
        static TutorialBuildRegistration()
        {
            EditorBuildSettings.sceneListChanged += Ensure;
            EditorApplication.delayCall += Ensure;
        }

        public static void Ensure()
        {
            if (!File.Exists(TutorialScene.Path)) return;
            var current = EditorBuildSettings.scenes;
            var scenes = new List<EditorBuildSettingsScene>(current.Where(s => s.path != TutorialScene.Path));
            int menu = scenes.FindIndex(s => s.path == "Assets/Scenes/MainMenu.unity");
            scenes.Insert(menu + 1, new EditorBuildSettingsScene(TutorialScene.Path, true));
            if (current.Length == scenes.Count && current.Zip(scenes,
                (a, b) => a.path == b.path && a.enabled == b.enabled).All(equal => equal)) return;
            // Tutorial precedes Level_01, so the normal campaign still ends at Level_03.
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
