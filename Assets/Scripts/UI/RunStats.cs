using UnityEngine.SceneManagement;

namespace Possession
{
    // Per-attempt numbers for the end screens. Deaths carry over restarts of the same level.
    public static class RunStats
    {
        public static int Possessions { get; private set; }
        public static int ForcedEjects { get; private set; }
        public static int Deaths { get; private set; }
        public static float Elapsed { get; set; }

        private static string levelName;

        public static void BeginAttempt()
        {
            string current = SceneManager.GetActiveScene().name;
            if (current != levelName)
            {
                levelName = current;
                Deaths = 0;
            }
            Possessions = 0;
            ForcedEjects = 0;
            Elapsed = 0f;
        }

        // Entering a level fresh (menu, Next Level) rather than via Restart
        public static void ResetLevel()
        {
            levelName = null;
        }

        public static void AddPossession() => Possessions++;
        public static void AddForcedEject() => ForcedEjects++;
        public static void AddDeath() => Deaths++;

        public static string Summary()
        {
            int seconds = UnityEngine.Mathf.FloorToInt(Elapsed);
            return $"TIME {seconds / 60}:{seconds % 60:D2}    POSSESSIONS {Possessions}    " +
                   $"FORCED EJECTS {ForcedEjects}    DEATHS {Deaths}";
        }
    }
}
