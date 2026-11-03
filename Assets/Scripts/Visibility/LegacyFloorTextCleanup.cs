using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Covers already-saved scenes without changing their serialized layout or object labels.
    public static class LegacyFloorTextCleanup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() => Apply(SceneManager.GetActiveScene());

        public static int Apply(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return 0;
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
            {
                string name = text.gameObject.name;
                if (name != "StartInstructions" && !name.StartsWith("Sign_", StringComparison.Ordinal)) continue;
                if (text.GetComponent<WorldLabel>() != null) continue;
                var renderer = text.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                renderer.enabled = false;
                count++;
            }
            return count;
        }
    }
}
