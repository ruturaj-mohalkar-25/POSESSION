using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    // P4 — Level_02 graybox.
    //
    // Two rooms plus a short exit corridor, laid out for this puzzle:
    //   shoot Target_01 (Room 1) and Target_02 (Room 2) -> Door opens ->
    //   smash BreakableWall in the corridor -> reach ExitZone as the Spirit.
    //
    // Enemies are NOT placed here, only named spawn points: P1 drops the Guards in and
    // gives them waypoints, P3 does the same for the Heavy.
    public static class BuildLevel02
    {
        const string ScenePath = "Assets/Scenes/Level_02.unity";
        const float LevelWidth = 12f;

        // Room 1: z 0..12, Room 2: z 12..24, exit corridor: z 24..30.
        const float Room1Z = 6f;
        const float Room2Z = 18f;
        const float CorridorZ = 27f;
        const float DividerZ = 12f;   // open passage between the two rooms
        const float DoorZ = 24f;      // gated by both targets
        const float WallZ = 27f;      // breakable wall inside the corridor
        const float ExitZ = 29f;

        [MenuItem("Possession/P4/Build Level_02")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            LevelBuildKit.BuildLighting();
            LevelBuildKit.BuildCamera();
            var manager = LevelBuildKit.BuildGameManager();

            var layout = new GameObject("P4_LevelLayout").transform;

            // ---- Geometry ----
            LevelBuildKit.Floor(layout, "Room1_Floor", new Vector3(0f, 0f, Room1Z), LevelWidth, 12f);
            LevelBuildKit.Floor(layout, "Room2_Floor", new Vector3(0f, 0f, Room2Z), LevelWidth, 12f);
            LevelBuildKit.Floor(layout, "Corridor_Floor", new Vector3(0f, 0f, CorridorZ), LevelWidth, 6f);
            LevelBuildKit.Perimeter(layout, LevelWidth, 0f, 30f);
            LevelBuildKit.DividerWithGap(layout, "Divider1", DividerZ, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "Divider2", DoorZ, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "WallPuzzle", WallZ, LevelWidth);

            // ---- Puzzle pieces (real prefabs) ----
            var target1 = LevelBuildKit.PlaceTarget(layout, "Target_01", -4f, 9f);   // Room 1
            var target2 = LevelBuildKit.PlaceTarget(layout, "Target_02", 4f, 21f);   // Room 2
            var door = LevelBuildKit.PlaceDoor(layout, "Door", DoorZ, new[] { target1, target2 });
            var wall = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall", WallZ);
            LevelBuildKit.PlaceExit(layout, "ExitZone", new Vector3(0f, 1f, ExitZ), door, wall);

            // ---- Player ----
            var spirit = LevelBuildKit.BuildSpirit(new Vector3(0f, LevelBuildKit.SpiritHeight, 3f));

            // ---- Spawn points for P1 (Guards) and P3 (Heavy) ----
            var spawns = new GameObject("SpawnPoints").transform;
            spawns.SetParent(layout);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_01", -2f, 8f);          // Room 1, covers Target_01
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_02", 2f, 16f);          // Room 2, covers Target_02
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_01", -3f, 20f, 1.3f);   // Room 2, walks to the wall

            // ---- Labels ----
            var labels = new GameObject("P4_Labels").transform;
            labels.SetParent(layout);
            LevelBuildKit.FloorSign(labels, "StartInstructions",
                "LEVEL 02\nWASD move   |   SPACE jump   |   E possess / eject\n"
                + "Possess a GUARD and shoot BOTH targets\n"
                + "Then possess the HEAVY and smash the wall",
                new Vector3(0f, 0f, 3.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Target_01", "SHOOT THIS TARGET", new Vector3(-4f, 0f, 7f));
            LevelBuildKit.FloorSign(labels, "Sign_Target_02", "SHOOT THIS TARGET TOO", new Vector3(4f, 0f, 19f));
            LevelBuildKit.FloorSign(labels, "Sign_Door", "DOOR OPENS WHEN BOTH TARGETS ARE HIT",
                new Vector3(0f, 0f, 22f));
            LevelBuildKit.FloorSign(labels, "Sign_Wall", "SMASH THE WALL AS THE HEAVY", new Vector3(0f, 0f, 25.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Exit", "EJECT AND REACH THE EXIT AS THE SPIRIT",
                new Vector3(0f, 0f, 28.2f), 0.045f);

            LevelBuildKit.ObjectLabel(labels, "SHOOT ME", target1.gameObject, new Vector3(0f, 1.3f, 0f),
                new Color(1f, 0.35f, 0.3f), "HIT!");
            LevelBuildKit.ObjectLabel(labels, "SHOOT ME", target2.gameObject, new Vector3(0f, 1.3f, 0f),
                new Color(1f, 0.35f, 0.3f), "HIT!");
            LevelBuildKit.ObjectLabel(labels, "LOCKED", door.gameObject, new Vector3(0f, 3.4f, -0.6f),
                new Color(0.85f, 0.62f, 0.15f), "OPEN");
            LevelBuildKit.ObjectLabel(labels, "BREAKABLE", wall.gameObject, new Vector3(0f, 3.4f, -0.6f), Color.white);

            // ---- Difficulty ----
            LevelBuildKit.ApplyConfig(LevelConfigAssets.Load("Level_02"), manager, spirit);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            LevelBuildKit.RegisterScene(ScenePath);

            Debug.Log("P4: Level_02 built at " + ScenePath
                + ". Enemies are spawn points only — P1 and P3 place the Guards and Heavy.");
        }
    }
}
