using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class BuildLevel02
    {
        const string ScenePath = "Assets/Scenes/Level_02.unity";
        const float LevelWidth = 12f;

        const float Room1Z = 6f;
        const float Room2Z = 18f;
        const float CorridorZ = 27f;
        const float DividerZ = 12f;
        const float DoorZ = 24f;
        const float WallZ = 27f;
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

            LevelBuildKit.Floor(layout, "Room1_Floor", new Vector3(0f, 0f, Room1Z), LevelWidth, 12f);
            LevelBuildKit.Floor(layout, "Room2_Floor", new Vector3(0f, 0f, Room2Z), LevelWidth, 12f);
            LevelBuildKit.Floor(layout, "Corridor_Floor", new Vector3(0f, 0f, CorridorZ), LevelWidth, 6f);
            LevelBuildKit.Perimeter(layout, LevelWidth, 0f, 30f);
            LevelBuildKit.DividerWithGap(layout, "Divider1", DividerZ, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "Divider2", DoorZ, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "WallPuzzle", WallZ, LevelWidth);

            var target1 = LevelBuildKit.PlaceTarget(layout, "Target_01", -4f, 9f);
            var target2 = LevelBuildKit.PlaceTarget(layout, "Target_02", 4f, 21f);
            var door = LevelBuildKit.PlaceDoor(layout, "Door", DoorZ, new[] { target1, target2 });
            var wall = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall", WallZ);
            LevelBuildKit.PlaceExit(layout, "ExitZone", new Vector3(0f, 1f, ExitZ), door, wall);

            var spirit = LevelBuildKit.BuildSpirit(new Vector3(0f, LevelBuildKit.SpiritHeight, 3f));

            var spawns = new GameObject("SpawnPoints").transform;
            spawns.SetParent(layout);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_01", -2f, 8f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_02", 2f, 16f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_01", -3f, 20f, 1.3f);

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

            LevelBuildKit.ApplyConfig(LevelConfigAssets.Load("Level_02"), manager, spirit);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            LevelBuildKit.RegisterScene(ScenePath);

            Debug.Log("P4: Level_02 built at " + ScenePath);
        }
    }
}
