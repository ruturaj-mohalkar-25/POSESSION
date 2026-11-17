using System.Linq;

namespace Possession
{
    // Player-facing order; scene asset names remain stable for shared builders and references.
    public static class CampaignLevels
    {
        public const string FirstScene = "Level_03";
        public const string SecondScene = "Level_02";
        public const string ThirdScene = "Level_03_Warehouse";
        public const string FourthScene = "Level_04_Gold";
        public const string FourthScenePath = "Assets/Level04Gold/Scenes/Level_04_Gold.unity";
        // Shared single-floor authoring tools retain their existing three-scene scope.
        public static string[] SceneNames => new[] { FirstScene, SecondScene, ThirdScene };
        public static string[] ScenePaths => new[] {
            "Assets/Scenes/Level_03.unity",
            "Assets/Scenes/Level_02.unity",
            "Assets/Level03Warehouse/Level_03_Warehouse.unity"
        };
        // Menu selection and progression include the separately authored two-storey level.
        public static string[] PlayableSceneNames => SceneNames.Concat(new[] { FourthScene }).ToArray();
        public static string[] PlayableScenePaths => ScenePaths.Concat(new[] { FourthScenePath }).ToArray();
        public static int NumberFor(string sceneName) => System.Array.IndexOf(PlayableSceneNames, sceneName) + 1;
        public static string NextScene(string sceneName)
        {
            var scenes = PlayableSceneNames;
            int index = System.Array.IndexOf(scenes, sceneName);
            return index >= 0 && index + 1 < scenes.Length ? scenes[index + 1] : null;
        }
    }
}
