using System;

namespace Possession
{
    public static class EventBus
    {
        // Possession
        public static Action<EnemyBase, PlayerState> Possessed;
        public static Action<EnemyBase, bool> Ejected; // enemy, wasForcedEject
        public static Action<float, float> PossessTimerUpdated; // current, max
        public static Action<bool> PossessPromptUpdated; // show/hide

        // Spirit
        public static Action<int, int> SpiritDamaged; // current, max
        public static Action SpiritDied;

        // Puzzle
        public static Action<Target> TargetHit;
        public static Action<Door> DoorOpened;
        public static Action<BreakableWall> WallBroken;

        // Level
        public static Action LevelCompleted;
        public static Action LevelFailed;
        public static Action<float, float> LevelTimerUpdated; // current, max
        public static Action<int, float> LevelCompletedWithStats; // stars, elapsedTime
        public static Action GameComplete;

        // Abilities
        public static Action<int, int> AmmoUpdated; // usesLeft, maxUses

        // Analytics
        public static Action<AnalyticsData> AnalyticsReady;

        public static void Clear()
        {
            Possessed = null;
            Ejected = null;
            PossessTimerUpdated = null;
            PossessPromptUpdated = null;
            SpiritDamaged = null;
            SpiritDied = null;
            TargetHit = null;
            DoorOpened = null;
            WallBroken = null;
            LevelCompleted = null;
            LevelFailed = null;
            LevelTimerUpdated = null;
            LevelCompletedWithStats = null;
            GameComplete = null;
            AmmoUpdated = null;
            AnalyticsReady = null;
        }
    }
}
