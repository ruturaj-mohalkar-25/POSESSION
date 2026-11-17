using UnityEngine;

namespace Possession
{
    // Highest campaign level the player may enter. Level 1 is always open.
    public static class CampaignProgress
    {
        private const string Key = "Possession.LevelsUnlocked";

        public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(Key, 1), 1, CampaignLevels.SceneNames.Length);
        public static bool IsUnlocked(int index) => index < Unlocked;

        // Finishing a campaign level opens the next one. Other scenes (tutorial, Level 4) are ignored.
        public static void OnLevelCompleted(string sceneName)
        {
            int number = CampaignLevels.NumberFor(sceneName);
            if (number > 0) Set(number + 1);
        }

        public static void Set(int count)
        {
            count = Mathf.Clamp(count, 1, CampaignLevels.SceneNames.Length);
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
