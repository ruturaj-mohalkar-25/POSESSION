using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class BuildLevel03
    {
        const string ScenePath = "Assets/Scenes/Level_03.unity";

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

            LevelBuildKit.FloorRect(layout, "Entry_Floor", -3f, 3f, 0f, 14f);
            LevelBuildKit.FloorRect(layout, "Fork_Floor", -12f, 12f, 14f, 22f);
            LevelBuildKit.FloorRect(layout, "Left_Floor", -12f, -4f, 22f, 40f);
            LevelBuildKit.FloorRect(layout, "Top_Floor", -12f, 12f, 34f, 40f);
            LevelBuildKit.FloorRect(layout, "Right_Floor", 4f, 12f, 6f, 40f);
            LevelBuildKit.FloorRect(layout, "Exit_Floor", 4f, 14f, 0f, 6f);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(layout);

            LevelBuildKit.WallRect(walls, "Entry_South", -3.25f, 3.25f, -0.25f, 0.25f);
            LevelBuildKit.WallRect(walls, "Entry_West", -3.25f, -2.75f, 0f, 14f);
            LevelBuildKit.WallRect(walls, "Entry_East", 2.75f, 3.25f, 0f, 14f);
            LevelBuildKit.WallRect(walls, "Door_FlankLeft", -3.25f, -1.6f, 13.75f, 14.25f);
            LevelBuildKit.WallRect(walls, "Door_FlankRight", 1.6f, 3.25f, 13.75f, 14.25f);

            LevelBuildKit.WallRect(walls, "Fork_West", -12.25f, -11.75f, 14f, 22f);
            LevelBuildKit.WallRect(walls, "Fork_East", 11.75f, 12.25f, 14f, 22f);
            LevelBuildKit.WallRect(walls, "Fork_SouthLeft", -12.25f, -3f, 13.75f, 14.25f);
            LevelBuildKit.WallRect(walls, "Fork_SouthRight", 3f, 4f, 13.75f, 14.25f);
            LevelBuildKit.WallRect(walls, "Fork_North", -4f, 4f, 21.75f, 22.25f);

            LevelBuildKit.WallRect(walls, "Left_West", -12.25f, -11.75f, 22f, 40f);
            LevelBuildKit.WallRect(walls, "Left_East", -4.25f, -3.75f, 22f, 34f);

            LevelBuildKit.WallRect(walls, "Top_North", -12.25f, 12.25f, 39.75f, 40.25f);
            LevelBuildKit.WallRect(walls, "Top_West", -12.25f, -11.75f, 34f, 40f);
            LevelBuildKit.WallRect(walls, "Top_East", 11.75f, 12.25f, 34f, 40f);
            LevelBuildKit.WallRect(walls, "Top_SouthMid", -4f, 4f, 33.75f, 34.25f);

            LevelBuildKit.WallRect(walls, "Right_West", 3.75f, 4.25f, 6f, 34f);
            LevelBuildKit.WallRect(walls, "Right_East", 11.75f, 12.25f, 6f, 40f);
            LevelBuildKit.WallRect(walls, "Wall1_FlankLeft", 4f, 6.4f, 29.75f, 30.25f);
            LevelBuildKit.WallRect(walls, "Wall1_FlankRight", 9.6f, 12.25f, 29.75f, 30.25f);
            LevelBuildKit.WallRect(walls, "Wall2_FlankLeft", 4f, 6.4f, 11.75f, 12.25f);
            LevelBuildKit.WallRect(walls, "Wall2_FlankRight", 9.6f, 12.25f, 11.75f, 12.25f);

            LevelBuildKit.WallRect(walls, "Exit_South", 4f, 14.25f, -0.25f, 0.25f);
            LevelBuildKit.WallRect(walls, "Exit_East", 13.75f, 14.25f, 0f, 6f);
            LevelBuildKit.WallRect(walls, "Exit_West", 3.75f, 4.25f, 0f, 6f);
            LevelBuildKit.WallRect(walls, "Exit_NorthRight", 12f, 14.25f, 5.75f, 6.25f);

            var target = LevelBuildKit.PlaceTarget(layout, "Target_01", 0f, 9f);
            var door = LevelBuildKit.PlaceDoor(layout, "Door", 0f, 14f, new[] { target });
            var wall1 = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall_01", 8f, 30f);
            var wall2 = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall_02", 8f, 12f);
            LevelBuildKit.PlaceExit(layout, "ExitZone", new Vector3(9f, 1f, 3f), door, wall2);

            var spirit = LevelBuildKit.BuildSpirit(new Vector3(0f, LevelBuildKit.SpiritHeight, 3f));

            var spawns = new GameObject("SpawnPoints").transform;
            spawns.SetParent(layout);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_01", 0f, 6f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_02", -8f, 30f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_01", 8f, 34f, 1.3f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_02", 8f, 18f, 1.3f);

            var labels = new GameObject("P4_Labels").transform;
            labels.SetParent(layout);
            LevelBuildKit.FloorSign(labels, "StartInstructions",
                "LEVEL 03\nWASD move   |   SPACE jump   |   E possess / eject\n"
                + "GUARD shoots the target to open the door\n"
                + "go LEFT and up, cross the top, come down the RIGHT\n"
                + "each HEAVY smashes one wall; finish as the SPIRIT",
                new Vector3(0f, 0f, 4f));
            LevelBuildKit.FloorSign(labels, "Sign_Target", "SHOOT THE TARGET", new Vector3(0f, 0f, 7f));
            LevelBuildKit.FloorSign(labels, "Sign_Door", "THE TARGET OPENS THIS DOOR", new Vector3(0f, 0f, 12f));
            LevelBuildKit.FloorSign(labels, "Sign_Fork", "ONLY THE LEFT PATH IS OPEN", new Vector3(-8f, 0f, 20f));
            LevelBuildKit.FloorSign(labels, "Sign_Top", "CROSS TO THE RIGHT CORRIDOR", new Vector3(0f, 0f, 37f));
            LevelBuildKit.FloorSign(labels, "Sign_Heavy_01", "POSSESS A HEAVY, SMASH THIS WALL",
                new Vector3(8f, 0f, 32.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Heavy_02", "SECOND HEAVY SMASHES THE EXIT WALL",
                new Vector3(8f, 0f, 14.5f));
            LevelBuildKit.FloorSign(labels, "Sign_Exit", "EJECT AND REACH THE EXIT AS THE SPIRIT",
                new Vector3(9f, 0f, 4f), 0.045f);

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
