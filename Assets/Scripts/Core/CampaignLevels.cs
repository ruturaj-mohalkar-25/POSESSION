namespace Possession
{
    // Player-facing order; scene asset names remain stable for shared builders and references.
    public static class CampaignLevels
    {
        public const string FirstScene = "Level_03";
        public const string SecondScene = "Level_02";
        public const string ThirdScene = "Level_03_Warehouse";
        public static string[] SceneNames => new[] { FirstScene, SecondScene, ThirdScene };
        public static string[] ScenePaths => new[] {
            "Assets/Scenes/Level_03.unity",
            "Assets/Scenes/Level_02.unity",
            "Assets/Level03Warehouse/Level_03_Warehouse.unity"
        };
        public static int NumberFor(string sceneName) => System.Array.IndexOf(SceneNames, sceneName) + 1;
        public static string NextScene(string sceneName)
        {
            var scenes = SceneNames;
            int index = System.Array.IndexOf(scenes, sceneName);
            return index >= 0 && index + 1 < scenes.Length ? scenes[index + 1] : null;
        }
    }
}
