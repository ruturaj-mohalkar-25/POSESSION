using System.Collections.Generic;
using UnityEngine;

namespace Possession
{
    public struct PossessionEntry
    {
        public string enemyType;
        public float duration;
        public bool forcedEject;
    }

    public struct AnalyticsData
    {
        public string levelName;
        public int possessionCount;
        public int forcedEjectCount;
        public int deathCount;
        public float totalPossessionTime;
        public float completionTime;
        public int stars;
        public List<PossessionEntry> possessions;
    }

    public class AnalyticsTracker : MonoBehaviour
    {
        private int possessionCount;
        private int forcedEjectCount;
        private int deathCount;
        private float possessionStartTime;
        private float totalPossessionTime;
        private string currentEnemyType;
        private readonly List<PossessionEntry> possessions = new();

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
            currentEnemyType = enemy is HeavyEnemy ? "Heavy" : "Guard";
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            float duration = Time.time - possessionStartTime;
            totalPossessionTime += duration;
            if (forced) forcedEjectCount++;
            possessions.Add(new PossessionEntry
            {
                enemyType = currentEnemyType,
                duration = duration,
                forcedEject = forced
            });
        }

        private void OnSpiritDied()
        {
            deathCount++;
        }

        private void OnLevelComplete(int stars, float elapsedTime)
        {
            if (possessionStartTime > 0f && possessions.Count < possessionCount)
            {
                possessions.Add(new PossessionEntry
                {
                    enemyType = currentEnemyType,
                    duration = Time.time - possessionStartTime,
                    forcedEject = false
                });
            }

            var data = new AnalyticsData
            {
                levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                possessionCount = possessionCount,
                forcedEjectCount = forcedEjectCount,
                deathCount = deathCount,
                totalPossessionTime = totalPossessionTime,
                completionTime = elapsedTime,
                stars = stars,
                possessions = new List<PossessionEntry>(possessions)
            };
            EventBus.AnalyticsReady?.Invoke(data);
            var firebase = FindFirstObjectByType<FirebaseReporter>();
            if (firebase != null) firebase.ReportSession(data);
        }
    }
}
