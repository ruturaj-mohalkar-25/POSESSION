using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    // Run after P4's builders. Owns only P3_Heavies and Heavy component settings.
    public static class HeavyLevelSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Enemies/Heavy.prefab";
        private const string GroupName = "P3_Heavies";

        [MenuItem("Possession/P3/Testing/Unlock Guard Door (Play Mode)")]
        public static void UnlockGuardDoorForTesting()
        {
            if (!Application.isPlaying) return;
            foreach (var target in UnityEngine.Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
                target.OnHit();
            Debug.Log("P3 focused test: Guard targets hit. Test Heavy movement, smashing and ejection next.");
        }

        [MenuItem("Possession/P3/Testing/Unlock Guard Door (Play Mode)", true)]
        private static bool CanUnlockGuardDoorForTesting() => Application.isPlaying;

        [MenuItem("Possession/P3/Configure Heavies in Current Level")]
        public static void ConfigureCurrentLevel()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before configuring Heavies.");
            ConfigureScene(SceneManager.GetActiveScene());
            Debug.Log("P3: Heavy setup applied. Save the scene to keep the changes.");
        }

        // Does not rebuild existing scenes, change P4 geometry, or add Guards.
        [MenuItem("Possession/P3/Prepare Local Level 02 and 03")]
        public static void PrepareLocalLevels()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before preparing levels.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!File.Exists("Assets/Config/LevelConfig_Level_02.asset")
                || !File.Exists("Assets/Config/LevelConfig_Level_03.asset"))
            {
                // Avoid overwriting another designer's partially configured asset set.
                if (Directory.Exists("Assets/Config")
                    && Directory.GetFiles("Assets/Config", "LevelConfig_*.asset").Length > 0)
                    throw new InvalidOperationException("Create the missing P4 level configs before preparing levels.");
                LevelConfigAssets.CreateAll();
            }

            PrepareLevel("Assets/Scenes/Level_02.unity", BuildLevel02.Build);
            PrepareLevel("Assets/Scenes/Level_03.unity", BuildLevel03.Build);
            AssetDatabase.SaveAssets();
            Debug.Log("P3: Local Level_02 and Level_03 are ready for focused Heavy tests.");
        }

        private static void PrepareLevel(string path, Action build)
        {
            if (!File.Exists(path)) build();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ConfigureScene(scene);
            EditorSceneManager.SaveScene(scene);
            LevelBuildKit.RegisterScene(path);
        }

        public static void ConfigureScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Open a level scene before configuring Heavies.");
            if (scene.name == "Level_01")
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var heavy in root.GetComponentsInChildren<HeavyEnemy>(true))
                        SetStats(heavy, 1.8f);
                return;
            }
            if (scene.name != "Level_02" && scene.name != "Level_03")
                throw new InvalidOperationException("P3 setup supports Level_01, Level_02 and Level_03 only.");

            bool levelTwo = scene.name == "Level_02";
            // Validate dependencies before making any scene changes.
            RequireWall(scene, levelTwo ? "BreakableWall" : "BreakableWall_01");
            if (!levelTwo) RequireWall(scene, "BreakableWall_02");
            var spawn1 = RequireObject(scene, "SpawnPoint_Heavy_01");
            var spawn2 = levelTwo ? null : RequireObject(scene, "SpawnPoint_Heavy_02");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<HeavyEnemy>() == null
                || prefab.GetComponent<SmashAbility>() == null)
                throw new InvalidOperationException("P3 setup requires the Heavy prefab with HeavyEnemy and SmashAbility.");

            var groupObject = FindObject(scene, GroupName);
            if (groupObject == null)
            {
                groupObject = new GameObject(GroupName);
                SceneManager.MoveGameObjectToScene(groupObject, scene);
                Undo.RegisterCreatedObjectUndo(groupObject, "Create P3 Heavy group");
            }
            var group = groupObject.transform;
            ConfigureHeavy(scene, group, prefab, "Heavy_01", spawn1.transform.position,
                levelTwo ? 2f : 2.2f,
                levelTwo ? new Vector3(-3f, 0f, 20f) : new Vector3(-2f, 0f, 19f),
                levelTwo ? new Vector3(0f, 0f, 22f) : new Vector3(2f, 0f, 21f));
            if (!levelTwo)
                ConfigureHeavy(scene, group, prefab, "Heavy_02", spawn2.transform.position, 2.2f,
                    new Vector3(-2f, 0f, 31f), new Vector3(2f, 0f, 33f));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ConfigureHeavy(Scene scene, Transform group, GameObject prefab,
            string name, Vector3 spawn, float speed, Vector3 first, Vector3 second)
        {
            var node = group.Find(name);
            GameObject instance;
            if (node == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = name;
                instance.transform.SetParent(group, false);
                Undo.RegisterCreatedObjectUndo(instance, "Place P3 Heavy");
            }
            else instance = node.gameObject;

            var heavy = instance.GetComponent<HeavyEnemy>();
            var controller = instance.GetComponent<CharacterController>();
            var smash = instance.GetComponent<SmashAbility>();
            if (heavy == null || controller == null || smash == null)
                throw new InvalidOperationException(name + " is missing a required Heavy component.");

            // P4 floors have a 0.1m top; place the capsule above it rather than inside it.
            float halfHeight = controller.height * Mathf.Abs(instance.transform.lossyScale.y) * 0.5f;
            spawn.y = LevelBuildKit.FloorThickness * 0.5f + halfHeight
                - controller.center.y * instance.transform.lossyScale.y + 0.05f;
            Undo.RecordObject(instance.transform, "Position P3 Heavy");
            instance.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            instance.layer = LevelBuildKit.EnemyLayer;
            SetStats(heavy, speed);

            var controllerSettings = new SerializedObject(controller);
            controllerSettings.FindProperty("m_MinMoveDistance").floatValue = 0f;
            controllerSettings.ApplyModifiedProperties();
            var smashSettings = new SerializedObject(smash);
            smashSettings.FindProperty("smashableLayer").intValue = 1 << LevelBuildKit.PuzzleLayer;
            smashSettings.ApplyModifiedProperties();
            if (instance.GetComponent<EnemyContactDamage>() == null)
                Undo.AddComponent<EnemyContactDamage>(instance);

            // These are siblings of the bodies: patrol points never move with an enemy.
            first.y = second.y = spawn.y;
            var route = group.Find(name + "_Route");
            if (route == null)
            {
                var routeObject = new GameObject(name + "_Route");
                route = routeObject.transform;
                route.SetParent(group, false);
                Undo.RegisterCreatedObjectUndo(routeObject, "Create P3 patrol route");
            }
            var firstPoint = Waypoint(route, "WP1", first);
            var secondPoint = Waypoint(route, "WP2", second);
            var settings = new SerializedObject(heavy);
            var waypoints = settings.FindProperty("waypoints");
            waypoints.arraySize = 2;
            waypoints.GetArrayElementAtIndex(0).objectReferenceValue = firstPoint;
            waypoints.GetArrayElementAtIndex(1).objectReferenceValue = secondPoint;
            settings.ApplyModifiedProperties();

            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_WorkerOrange.mat");
            var renderer = instance.GetComponent<Renderer>();
            if (material != null && renderer != null && renderer.sharedMaterial == null)
            {
                Undo.RecordObject(renderer, "Set Heavy material");
                renderer.sharedMaterial = material;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        }

        private static void SetStats(HeavyEnemy heavy, float speed)
        {
            var settings = new SerializedObject(heavy);
            settings.FindProperty("moveSpeed").floatValue = speed;
            settings.FindProperty("maxHP").intValue = 5;
            settings.ApplyModifiedProperties();
        }

        private static Transform Waypoint(Transform route, string name, Vector3 position)
        {
            var point = route.Find(name);
            if (point == null)
            {
                point = new GameObject(name).transform;
                point.SetParent(route, false);
                Undo.RegisterCreatedObjectUndo(point.gameObject, "Create P3 waypoint");
            }
            Undo.RecordObject(point, "Position P3 waypoint");
            point.position = position;
            return point;
        }

        private static void RequireWall(Scene scene, string name)
        {
            var wall = RequireObject(scene, name);
            if (wall.GetComponent<BreakableWall>() == null || wall.GetComponent<Collider>() == null)
                throw new InvalidOperationException(name + " must have BreakableWall and a collider.");
        }

        private static GameObject RequireObject(Scene scene, string name)
        {
            var found = FindObject(scene, name);
            if (found == null)
                throw new InvalidOperationException("Missing " + name + ". Build the P4 level first.");
            return found;
        }

        private static GameObject FindObject(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    if (node.name == name) return node.gameObject;
            return null;
        }
    }
}
