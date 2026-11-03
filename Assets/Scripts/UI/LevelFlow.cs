using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Player-facing order differs from the legacy scene filenames.
    public static class LevelFlow
    {
        public static System.Collections.Generic.IReadOnlyList<string> CampaignScenes { get; } =
            System.Array.AsReadOnly(new[] { "Level_01", "Level_03", "Level_02" });

        public static bool TryGetNextLevel(string current, out string next)
        {
            next = null;
            for (int i = 0; i < CampaignScenes.Count - 1; i++)
                if (CampaignScenes[i] == current) { next = CampaignScenes[i + 1]; return true; }
            return false;
        }

        public static bool IsLastCampaignLevel(string current) =>
            current == CampaignScenes[CampaignScenes.Count - 1];

        public static bool IsInBuild(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);

        public static void Load(string sceneName)
        {
            Prepare();
            SceneManager.LoadScene(sceneName);
        }

        private static void Prepare()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
        }
    }
}
