using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // CampaignLevels owns the displayed order; loads reset pause and shared events.
    public static class LevelFlow
    {
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
