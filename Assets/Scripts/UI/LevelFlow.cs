using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Scene order comes from the build list: index 0 is MainMenu, levels follow.
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
