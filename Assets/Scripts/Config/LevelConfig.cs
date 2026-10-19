using UnityEngine;

namespace Possession
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Possession/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelName = "Level_01";

        [Header("Timers")]
        [Tooltip("Seconds the Spirit may stay in a possessed enemy.")]
        [SerializeField, Min(1f)] private float possessionTimer = 12f;
        [Tooltip("Seconds on the level clock.")]
        [SerializeField, Min(1f)] private float levelTime = 300f;

        [Header("Enemies")]
        [Tooltip("Metres at which a Guard notices the Spirit.")]
        [SerializeField, Min(0f)] private float guardDetectionRange = 10f;

        [Header("Par times (seconds)")]
        [SerializeField, Min(1f)] private float goldParTime = 100f;
        [SerializeField, Min(1f)] private float silverParTime = 150f;
        [SerializeField, Min(1f)] private float bronzeParTime = 225f;

        public string LevelName => levelName;
        public float PossessionTimer => possessionTimer;
        public float LevelTime => levelTime;
        public float GuardDetectionRange => guardDetectionRange;
        public float GoldParTime => goldParTime;
        public float SilverParTime => silverParTime;
        public float BronzeParTime => bronzeParTime;
    }
}
