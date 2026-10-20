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
        [Tooltip("Guard movement speed in metres per second.")]
        [SerializeField, Min(0f)] private float guardMoveSpeed = 3f;
        [Tooltip("Guard health for this level.")]
        [SerializeField, Min(1)] private int guardMaxHP = 3;
        [Tooltip("Metres at which a Guard notices the Spirit.")]
        [SerializeField, Min(0f)] private float guardDetectionRange = 10f;
        [Tooltip("Seconds a Guard waits after reaching a waypoint.")]
        [SerializeField, Min(0f)] private float guardWaypointWaitTime = 0.75f;
        [Tooltip("Distance a chasing Guard keeps from the Spirit.")]
        [SerializeField, Min(0f)] private float guardChaseStopDistance = 1.5f;

        [Header("Par times (seconds)")]
        [SerializeField, Min(1f)] private float goldParTime = 100f;
        [SerializeField, Min(1f)] private float silverParTime = 150f;
        [SerializeField, Min(1f)] private float bronzeParTime = 225f;

        public string LevelName => levelName;
        public float PossessionTimer => possessionTimer;
        public float LevelTime => levelTime;
        public float GuardMoveSpeed => guardMoveSpeed;
        public int GuardMaxHP => guardMaxHP;
        public float GuardDetectionRange => guardDetectionRange;
        public float GuardWaypointWaitTime => guardWaypointWaitTime;
        public float GuardChaseStopDistance => guardChaseStopDistance;
        public float GoldParTime => goldParTime;
        public float SilverParTime => silverParTime;
        public float BronzeParTime => bronzeParTime;
    }
}
