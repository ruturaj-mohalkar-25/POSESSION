using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    // Adds P1 Guards without rebuilding P3/P4/P5 scene content.
    public static class GuardLevelSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Enemies/Guard.prefab";
        private const string GroupName = "P1_Guards";

        [MenuItem("Possession/P1/W2/Configure Guards in Current Level")]
        public static void ConfigureCurrentLevel()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before configuring Guards.");
            ConfigureScene(SceneManager.GetActiveScene());
            Debug.Log("P1: Guard setup applied. Save the scene to keep the changes.");
        }

        [MenuItem("Possession/P1/W2/Prepare Guards in Levels 02-03")]
        public static void PrepareLevels()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before preparing levels.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in new[] { "Level_02", "Level_03" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    ConfigureScene(scene);
                    if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("P1 W2: Guard placements, patrols and per-level stats configured.");
            }
            finally
            {
                if (!Application.isBatchMode)
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        public static void ConfigureScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Open a level scene before configuring Guards.");
            if (scene.name != "Level_02" && scene.name != "Level_03")
                throw new InvalidOperationException("P1 setup supports Level_02 and Level_03.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<GuardEnemy>() == null)
                throw new InvalidOperationException("P1 setup requires the Guard prefab with GuardEnemy.");

            var config = LevelConfigAssets.Load(scene.name);
            if (config == null)
                throw new InvalidOperationException("Missing LevelConfig for " + scene.name + ".");

            var groupObject = FindObject(scene, GroupName);
            if (groupObject == null)
            {
                groupObject = new GameObject(GroupName);
                SceneManager.MoveGameObjectToScene(groupObject, scene);
                Undo.RegisterCreatedObjectUndo(groupObject, "Create P1 Guard group");
            }

            if (scene.name == "Level_02")
            {
                ConfigureGuard(scene, groupObject.transform, prefab, config, "Guard_01",
                    RequireSpawn(scene, "SpawnPoint_Guard_01"), 0f,
                    new Vector3(0f, LevelBuildKit.SpawnHeight, 4f),
                    new Vector3(0f, LevelBuildKit.SpawnHeight, 10f));
                ConfigureGuard(scene, groupObject.transform, prefab, config, "Guard_02",
                    RequireSpawn(scene, "SpawnPoint_Guard_02"), 0.75f,
                    new Vector3(2f, LevelBuildKit.SpawnHeight, 18f),
                    new Vector3(7f, LevelBuildKit.SpawnHeight, 18f));
            }
            else
            {
                ConfigureGuard(scene, groupObject.transform, prefab, config, "Guard_01",
                    RequireSpawn(scene, "SpawnPoint_Guard_01"), 0f,
                    new Vector3(0f, LevelBuildKit.SpawnHeight, 4f),
                    new Vector3(0f, LevelBuildKit.SpawnHeight, 11f));
                ConfigureGuard(scene, groupObject.transform, prefab, config, "Guard_02",
                    RequireSpawn(scene, "SpawnPoint_Guard_02"), 0.35f,
                    new Vector3(-9f, LevelBuildKit.SpawnHeight, 26f),
                    new Vector3(-7f, LevelBuildKit.SpawnHeight, 32f));
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ConfigureGuard(Scene scene, Transform group, GameObject prefab,
            LevelConfig config, string name, Vector3 spawn, float startDelay,
            Vector3 first, Vector3 second)
        {
            Transform node = group.Find(name);
            GameObject instance;
            if (node == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = name;
                instance.transform.SetParent(group, false);
                Undo.RegisterCreatedObjectUndo(instance, "Place P1 Guard");
            }
            else instance = node.gameObject;

            var guard = instance.GetComponent<GuardEnemy>();
            var controller = instance.GetComponent<CharacterController>();
            if (guard == null || controller == null)
                throw new InvalidOperationException(name + " is missing a required Guard component.");

            float halfHeight = controller.height * Mathf.Abs(instance.transform.lossyScale.y) * 0.5f;
            spawn.y = LevelBuildKit.FloorThickness * 0.5f + halfHeight
                - controller.center.y * instance.transform.lossyScale.y + 0.05f;
            first.y = second.y = spawn.y;

            Undo.RecordObject(instance.transform, "Position P1 Guard");
            instance.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            instance.layer = LevelBuildKit.EnemyLayer;

            Transform route = group.Find(name + "_Route");
            if (route == null)
            {
                route = new GameObject(name + "_Route").transform;
                route.SetParent(group, false);
                Undo.RegisterCreatedObjectUndo(route.gameObject, "Create P1 patrol route");
            }
            Transform firstPoint = Waypoint(route, "WP1", first);
            Transform secondPoint = Waypoint(route, "WP2", second);

            var settings = new SerializedObject(guard);
            settings.FindProperty("moveSpeed").floatValue = config.GuardMoveSpeed;
            settings.FindProperty("maxHP").intValue = config.GuardMaxHP;
            settings.FindProperty("detectionRange").floatValue = config.GuardDetectionRange;
            settings.FindProperty("waypointWaitTime").floatValue = config.GuardWaypointWaitTime;
            settings.FindProperty("chaseStopDistance").floatValue = config.GuardChaseStopDistance;
            settings.FindProperty("patrolStartDelay").floatValue = startDelay;
            var waypoints = settings.FindProperty("waypoints");
            waypoints.arraySize = 2;
            waypoints.GetArrayElementAtIndex(0).objectReferenceValue = firstPoint;
            waypoints.GetArrayElementAtIndex(1).objectReferenceValue = secondPoint;
            settings.ApplyModifiedProperties();

            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(guard);
        }

        private static Transform Waypoint(Transform route, string name, Vector3 position)
        {
            Transform point = route.Find(name);
            if (point == null)
            {
                point = new GameObject(name).transform;
                point.SetParent(route, false);
                Undo.RegisterCreatedObjectUndo(point.gameObject, "Create P1 waypoint");
            }
            Undo.RecordObject(point, "Position P1 waypoint");
            point.position = position;
            return point;
        }

        private static Vector3 RequireSpawn(Scene scene, string name)
        {
            GameObject found = FindObject(scene, name);
            if (found == null)
                throw new InvalidOperationException("Missing " + name + ". Build the P4 level first.");
            return found.transform.position;
        }

        private static GameObject FindObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    if (node.name == name) return node.gameObject;
            return null;
        }
    }
}
