using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Installs at runtime so shared scene, prefab, builder and AI files stay untouched.
    public static class HeavySmashIndicatorInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Subscribe();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // GameManager clears EventBus before restarting/changing scenes.
            Subscribe();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var heavy in root.GetComponentsInChildren<HeavyEnemy>(true))
                    Attach(heavy);
        }

        private static void Subscribe()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Possessed += OnPossessed;
        }

        private static HeavySmashIndicator Attach(HeavyEnemy heavy)
        {
            if (heavy.GetComponent<SmashAbility>() == null) return null;
            var indicator = heavy.GetComponent<HeavySmashIndicator>();
            return indicator != null ? indicator : heavy.gameObject.AddComponent<HeavySmashIndicator>();
        }

        private static void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            // Covers enemies spawned after sceneLoaded as well as saved scene instances.
            if (!(enemy is HeavyEnemy heavy)) return;
            var indicator = Attach(heavy);
            if (indicator != null && indicator.isActiveAndEnabled)
                indicator.HandlePossessed(enemy, state);
        }
    }
}
