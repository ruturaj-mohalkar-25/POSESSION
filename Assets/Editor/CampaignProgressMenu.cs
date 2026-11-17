using UnityEditor;

namespace Possession
{
    public static class CampaignProgressMenu
    {
        [MenuItem("Possession/Campaign/Unlock All Levels")]
        private static void UnlockAll() => CampaignProgress.Set(CampaignLevels.SceneNames.Length);

        [MenuItem("Possession/Campaign/Reset Progress")]
        private static void ResetAll() => CampaignProgress.Reset();
    }
}
