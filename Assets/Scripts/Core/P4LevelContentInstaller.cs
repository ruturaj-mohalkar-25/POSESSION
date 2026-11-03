using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // P4 owns Level_01 and How to Play. Installing the two authored shards here keeps
    // shipped scenes correct even when an editor-only scene rebuild was not run.
    public static class P4LevelContentInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= Install;
            SceneManager.sceneLoaded += Install;
        }

        private static void Install(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Level_01" && scene.name != TutorialScene.Name) return;

            var spirit = Object.FindFirstObjectByType<SpiritController>();
            if (spirit != null && spirit.GetComponent<PossessionCharge>() == null)
                spirit.gameObject.AddComponent<PossessionCharge>();

            var existing = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None);
            if (existing.Length >= 2)
            {
                ChargeHUDInstaller.Install();
                return;
            }
            var rootObject = GameObject.Find("ChargePickups");
            var root = rootObject != null ? rootObject.transform : new GameObject("ChargePickups").transform;
            float firstZ = scene.name == TutorialScene.Name ? 10f : 15f;
            if (existing.Length == 0)
                PossessionChargePickup.Create(root, "Pickup_01", new Vector3(-3f, 0.1f, firstZ));
            PossessionChargePickup.Create(root, "Pickup_02", new Vector3(3f, 0.1f, 27f));
            ChargeHUDInstaller.Install();
        }
    }
}
