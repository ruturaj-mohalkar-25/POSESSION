using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public static class WallOcclusionInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() => Install();

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

        public static WallOcclusionFader Install()
        {
            var player = Object.FindFirstObjectByType<PossessionController>();
            var camera = Camera.main;
            if (player == null || camera == null) return null;
            var existing = camera.GetComponent<WallOcclusionFader>();
            return existing != null ? existing : camera.gameObject.AddComponent<WallOcclusionFader>();
        }
    }
}
