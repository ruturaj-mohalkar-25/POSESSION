using UnityEngine;

namespace Possession
{
    public struct AnalyticsData
    {
        public string levelName;
        public int possessionCount;
        public int forcedEjectCount;
        public int deathCount;
        public float totalPossessionTime;
        public float completionTime;
        public int stars;
    }

    public class AnalyticsTracker : MonoBehaviour
    {
        private int possessionCount;
        private int forcedEjectCount;
        private int deathCount;
        private float possessionStartTime;
        private float totalPossessionTime;

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.SpiritDied += OnSpiritDied;
            EventBus.LevelCompletedWithStats += OnLevelComplete;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.SpiritDied -= OnSpiritDied;
            EventBus.LevelCompletedWithStats -= OnLevelComplete;
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            possessionCount++;
            possessionStartTime = Time.time;
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            totalPossessionTime += Time.time - possessionStartTime;
            if (forced) forcedEjectCount++;
        }

        private void OnSpiritDied()
        {
            deathCount++;
        }

        private void OnLevelComplete(int stars, float elapsedTime)
        {
            var data = new AnalyticsData
            {
                levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                possessionCount = possessionCount,
                forcedEjectCount = forcedEjectCount,
                deathCount = deathCount,
                totalPossessionTime = totalPossessionTime,
                completionTime = elapsedTime,
                stars = stars
            };
            EventBus.AnalyticsReady?.Invoke(data);
        }
    }
}
