using UnityEngine;

namespace Possession
{
    // Per-level difficulty knobs, exposed in the Inspector.
    // Level_01 keeps these numbers on its scene components (GameManager.levelTime,
    // PossessionController.possessDuration, GuardEnemy.detectionRange). This asset is the one
    // place to edit them; the P4 level builders write them into the scene while building it.
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Possession/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelName = "Level_01";

        [Header("Timers")]
        [Tooltip("Seconds the Spirit may stay inside a possessed enemy before it is forced out.")]
        [SerializeField, Min(1f)] private float possessionTimer = 12f;
        [Tooltip("Seconds on the level clock before the run fails.")]
        [SerializeField, Min(1f)] private float levelTime = 300f;

        [Header("Enemies")]
        [Tooltip("Metres at which a Guard notices the Spirit. Rises per level, so later levels " +
                 "give the player less room to sneak.")]
        [SerializeField, Min(0f)] private float guardDetectionRange = 10f;

        [Header("Par times (seconds)")]
        // Starting points only — retune these once the levels have been played through.
        // Rule of thumb used here: gold ~1/3 of the level clock, silver ~1/2, bronze ~3/4.
        [Tooltip("Finish at or under this time for gold.")]
        [SerializeField, Min(1f)] private float goldParTime = 100f;
        [Tooltip("Finish at or under this time for silver.")]
        [SerializeField, Min(1f)] private float silverParTime = 150f;
        [Tooltip("Finish at or under this time for bronze. Slower than this still completes the level.")]
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
