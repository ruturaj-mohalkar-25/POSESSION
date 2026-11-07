using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    [InitializeOnLoad]
    public static class WarehouseCampaignSetup
    {
        static WarehouseCampaignSetup()
        {
            EditorBuildSettings.sceneListChanged += EnsureBuildOrder;
            EditorApplication.delayCall += EnsureBuildOrder;
        }
        public static void EnsureBuildOrder()
        {
            var paths = new[] { "Assets/Scenes/MainMenu.unity", TutorialScene.Path }
                .Concat(CampaignLevels.ScenePaths).ToArray();
            if (paths.Any(p => !File.Exists(p))) return;
            var current = EditorBuildSettings.scenes;
            var result = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            // Retain the original Level_01 solely as a shared builder/test fixture, outside the menu/campaign.
            foreach (var entry in current)
                if (!paths.Contains(entry.path)) result.Add(entry);
            if (current.Length == result.Count && current.Zip(result,
                (a,b) => a.path == b.path && a.enabled == b.enabled && a.guid.Equals(b.guid)).All(x => x)) return;
            EditorBuildSettings.scenes = result.ToArray();
        }
        [MenuItem("Possession/Level Design/Build Three-Level Campaign")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before rebuilding the campaign.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            WarehouseBuilder.Build();
            EnsureBuildOrder();
            // Flush obsolete serialized order fields while preserving authored geometry and component references.
            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", TutorialScene.Path }
                .Concat(CampaignLevels.ScenePaths))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                CampaignSpaceSetup.Apply(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Campaign ready: Level 1 = former menu Level 2; Level 2 = Hunted maze; Level 3 = Warehouse.");
        }
    }
}
