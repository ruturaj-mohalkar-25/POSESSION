using System;

namespace Possession
{
    // UI-facing hooks. Gameplay systems raise these; the HUD only listens.
    public static class HUDEvents
    {
        // Remaining ability uses for the possessed body: left, max
        public static Action<int, int> AmmoChanged;

        // Level rating from the par-time system: 1 = bronze, 2 = silver, 3 = gold
        public static Action<int> LevelRated;
    }
}
