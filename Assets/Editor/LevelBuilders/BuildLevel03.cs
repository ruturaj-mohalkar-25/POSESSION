using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class BuildLevel03
    {
        const string ScenePath = "Assets/Scenes/Level_03.unity";
        const float LevelWidth = 12f;

        const float Room1Z = 6f;
        const float Room2Z = 18f;
        const float Room3Z = 30f;
        const float AlcoveZ = 39f;
        const float DoorZ = 12f;
        const float Wall1Z = 24f;
        const float Wall2Z = 36f;
        const float ExitZ = 41f;

        [MenuItem("Possession/P4/Build Level_03")]
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
            LevelBuildKit.Floor(layout, "Room3_Floor", new Vector3(0f, 0f, Room3Z), LevelWidth, 12f);
            LevelBuildKit.Floor(layout, "Alcove_Floor", new Vector3(0f, 0f, AlcoveZ), LevelWidth, 6f);
            LevelBuildKit.Perimeter(layout, LevelWidth, 0f, 42f);
            LevelBuildKit.DividerWithGap(layout, "Divider1", DoorZ, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "Divider2", Wall1Z, LevelWidth);
            LevelBuildKit.DividerWithGap(layout, "Divider3", Wall2Z, LevelWidth);

            var target = LevelBuildKit.PlaceTarget(layout, "Target_01", 4f, 9f);
            var door = LevelBuildKit.PlaceDoor(layout, "Door", DoorZ, new[] { target });
            var wall1 = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall_01", Wall1Z);
            var wall2 = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall_02", Wall2Z);
            LevelBuildKit.PlaceExit(layout, "ExitZone", new Vector3(0f, 1f, ExitZ), door, wall2);

            var spirit = LevelBuildKit.BuildSpirit(new Vector3(0f, LevelBuildKit.SpiritHeight, 3f));

            var spawns = new GameObject("SpawnPoints").transform;
            spawns.SetParent(layout);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_01", -2f, 7f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_02", 3f, 31f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_01", 0f, 20f, 1.3f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_02", 0f, 32f, 1.3f);

            var labels = new GameObject("P4_Labels").transform;
            labels.SetParent(layout);
            LevelBuildKit.FloorSign(labels, "StartInstructions",
                "LEVEL 03\nWASD move   |   SPACE jump   |   E possess / eject\n"
                + "GUARD shoots the target, then each HEAVY smashes one wall\n"
                + "Finish the run as the SPIRIT",
                new Vector3(0f, 0f, 3.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Target", "SHOOT THE TARGET", new Vector3(4f, 0f, 7f));
            LevelBuildKit.FloorSign(labels, "Sign_Door", "THE TARGET OPENS THIS DOOR", new Vector3(0f, 0f, 10.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Heavy_01", "POSSESS THE HEAVY, SMASH THE WALL",
                new Vector3(0f, 0f, 22f));
            LevelBuildKit.FloorSign(labels, "Sign_Heavy_02", "SECOND HEAVY SMASHES THE EXIT WALL",
                new Vector3(0f, 0f, 34f));
            LevelBuildKit.FloorSign(labels, "Sign_Exit", "EJECT AND REACH THE EXIT AS THE SPIRIT",
                new Vector3(0f, 0f, 39.5f), 0.045f);

            LevelBuildKit.ObjectLabel(labels, "SHOOT ME", target.gameObject, new Vector3(0f, 1.3f, 0f),
                new Color(1f, 0.35f, 0.3f), "HIT!");
            LevelBuildKit.ObjectLabel(labels, "LOCKED", door.gameObject, new Vector3(0f, 3.4f, -0.6f),
                new Color(0.85f, 0.62f, 0.15f), "OPEN");
            LevelBuildKit.ObjectLabel(labels, "BREAKABLE", wall1.gameObject, new Vector3(0f, 3.4f, -0.6f), Color.white);
            LevelBuildKit.ObjectLabel(labels, "EXIT WALL", wall2.gameObject, new Vector3(0f, 3.4f, -0.6f), Color.white);

            LevelBuildKit.ApplyConfig(LevelConfigAssets.Load("Level_03"), manager, spirit);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            LevelBuildKit.RegisterScene(ScenePath);

            Debug.Log("P4: Level_03 built at " + ScenePath);
        }
    }
}
