using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class BuildLevel02
    {
        const string ScenePath = "Assets/Scenes/Level_02.unity";

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

            LevelBuildKit.FloorRect(layout, "Entry_Floor", -3f, 3f, 0f, 12f);
            LevelBuildKit.FloorRect(layout, "Hub_Floor", -3f, 9f, 12f, 20f);
            LevelBuildKit.FloorRect(layout, "Cache_Floor", -9f, -3f, 12f, 19f);
            LevelBuildKit.FloorRect(layout, "North_Floor", -3f, 3f, 20f, 32f);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(layout);

            LevelBuildKit.WallRect(walls, "Entry_South", -3.25f, 3.25f, -0.25f, 0.25f);
            LevelBuildKit.WallRect(walls, "Entry_West", -3.25f, -2.75f, 0f, 12f);
            LevelBuildKit.WallRect(walls, "Entry_East", 2.75f, 3.25f, 0f, 12f);

            LevelBuildKit.WallRect(walls, "Hub_East", 8.75f, 9.25f, 12f, 20f);
            LevelBuildKit.WallRect(walls, "Hub_SouthRight", 3f, 9.25f, 11.75f, 12.25f);
            LevelBuildKit.WallRect(walls, "Hub_West_Lower", -3.25f, -2.75f, 12f, 14f);
            LevelBuildKit.WallRect(walls, "Hub_West_Upper", -3.25f, -2.75f, 17f, 20f);
            LevelBuildKit.WallRect(walls, "Hub_NorthRight", 3f, 9.25f, 19.75f, 20.25f);

            LevelBuildKit.WallRect(walls, "Cache_West", -9.25f, -8.75f, 12f, 19f);
            LevelBuildKit.WallRect(walls, "Cache_South", -9.25f, -3f, 11.75f, 12.25f);
            LevelBuildKit.WallRect(walls, "Cache_North", -9.25f, -3f, 18.75f, 19.25f);

            LevelBuildKit.WallRect(walls, "Door_FlankLeft", -3.25f, -1.6f, 19.75f, 20.25f);
            LevelBuildKit.WallRect(walls, "Door_FlankRight", 1.6f, 3.25f, 19.75f, 20.25f);
            LevelBuildKit.WallRect(walls, "North_West", -3.25f, -2.75f, 20f, 32f);
            LevelBuildKit.WallRect(walls, "North_East", 2.75f, 3.25f, 20f, 32f);
            LevelBuildKit.WallRect(walls, "Wall_FlankLeft", -3.25f, -1.6f, 26.75f, 27.25f);
            LevelBuildKit.WallRect(walls, "Wall_FlankRight", 1.6f, 3.25f, 26.75f, 27.25f);
            LevelBuildKit.WallRect(walls, "North_Boundary", -3.25f, 3.25f, 31.75f, 32.25f);

            var target1 = LevelBuildKit.PlaceTarget(layout, "Target_01", 6f, 16f);
            var target2 = LevelBuildKit.PlaceTarget(layout, "Target_02", -6f, 15.5f);
            var door = LevelBuildKit.PlaceDoor(layout, "Door", 0f, 20f, new[] { target1, target2 });
            var wall = LevelBuildKit.PlaceBreakableWall(layout, "BreakableWall", 0f, 27f);
            LevelBuildKit.PlaceExit(layout, "ExitZone", new Vector3(0f, 1f, 30f), door, wall);

            var spirit = LevelBuildKit.BuildSpirit(new Vector3(0f, LevelBuildKit.SpiritHeight, 3f));

            var spawns = new GameObject("SpawnPoints").transform;
            spawns.SetParent(layout);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_01", 0f, 7f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Guard_02", 6f, 18f);
            LevelBuildKit.SpawnPoint(spawns, "SpawnPoint_Heavy_01", 0f, 23f, 1.3f);

            var enemies = new GameObject("Enemies").transform;
            enemies.SetParent(layout);

            var labels = new GameObject("P4_Labels").transform;
            labels.SetParent(layout);
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
