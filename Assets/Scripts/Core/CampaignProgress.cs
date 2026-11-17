using UnityEngine;

namespace Possession
{
    // Highest campaign level the player may enter. Level 1 is always open.
    public static class CampaignProgress
    {
        private const string Key = "Possession.LevelsUnlocked";

        public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(Key, 1), 1, CampaignLevels.PlayableSceneNames.Length);
        public static bool IsUnlocked(int index) => index >= 0 && index < Unlocked;

        // Finishing a playable campaign level opens the next one; tutorials and fixtures are ignored.
        public static void OnLevelCompleted(string sceneName)
        {
            int number = CampaignLevels.NumberFor(sceneName);
            if (number > 0) Set(number + 1);
        }

        public static void Set(int count)
        {
            count = Mathf.Clamp(count, 1, CampaignLevels.PlayableSceneNames.Length);
            if (count <= Unlocked) return;
            PlayerPrefs.SetInt(Key, count);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
