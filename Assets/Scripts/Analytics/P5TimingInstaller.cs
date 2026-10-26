using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public static class P5TimingInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() => Install(SceneManager.GetActiveScene());

        public static P5TimingTracker Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var manager = root.GetComponentInChildren<GameManager>();
                if (manager == null || !manager.isActiveAndEnabled) continue;
                return manager.GetComponent<P5TimingTracker>() ?? manager.gameObject.AddComponent<P5TimingTracker>();
            }
            return null;
        }
    }
}
