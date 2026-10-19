using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Scene order comes from the build list: index 0 is MainMenu, levels follow.
    public static class LevelFlow
    {
        public const int FirstLevelIndex = 1;

        public static bool IsLastLevel =>
            SceneManager.GetActiveScene().buildIndex >= SceneManager.sceneCountInBuildSettings - 1;

        public static bool IsInBuild(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);

        public static void Load(string sceneName)
        {
            Prepare();
            SceneManager.LoadScene(sceneName);
        }

        public static void LoadIndex(int buildIndex)
        {
            Prepare();
            SceneManager.LoadScene(buildIndex);
        }

        public static void LoadNext()
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings) LoadIndex(next);
        }

        private static void Prepare()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            RunStats.ResetLevel();
        }
    }
}
