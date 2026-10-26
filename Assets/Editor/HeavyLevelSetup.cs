using System;
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

        [MenuItem("Possession/P3/W2/Configure Heavies in Current Level")]
        public static void ConfigureCurrentLevel()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before configuring Heavies.");
            ConfigureScene(SceneManager.GetActiveScene());
            Debug.Log("P3: Heavy setup applied. Save the scene to keep the changes.");
        }

        [MenuItem("Possession/P3/W2/Prepare Heavies in Levels 01-03")]
        public static void PrepareLevels()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play mode before preparing levels.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in new[] { "Level_01", "Level_02", "Level_03" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    ConfigureScene(scene);
                    if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("P3 W2: Heavy placements and per-level speeds configured.");
            }
            finally
            {
                if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
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
                spawn1.transform.position,
                spawn1.transform.position + (levelTwo ? new Vector3(1.4f, 0f, 2f) : new Vector3(2f, 0f, -1f)));
            if (!levelTwo)
                ConfigureHeavy(scene, group, prefab, "Heavy_02", spawn2.transform.position, 2.2f,
                    spawn2.transform.position, spawn2.transform.position + new Vector3(2f, 0f, -2f));
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
            var hostile = instance.GetComponent<EnemyContactDamage>();
            if (hostile == null) hostile = Undo.AddComponent<EnemyContactDamage>(instance);
            var hostileSettings = new SerializedObject(hostile);
            hostileSettings.FindProperty("dustMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P5_FXDust.mat");
            hostileSettings.ApplyModifiedProperties();

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
            PrefabUtility.RecordPrefabInstancePropertyModifications(heavy);
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
