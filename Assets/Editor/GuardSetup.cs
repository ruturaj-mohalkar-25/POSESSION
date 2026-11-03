using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class GuardSetup
    {
        [MenuItem("Possession/P1/Update Guard Prefab and Level Component")]
        public static void PrepareAssets()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            const string prefabPath = "Assets/Prefabs/Enemies/Guard.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Configure(root.GetComponent<GuardEnemy>(), true);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity");
            foreach (var guard in Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None))
                Configure(guard, false);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P1: Guard prefab and Level_01 component configured.");
        }

        private static void Configure(GuardEnemy guard, bool prefab)
        {
            var capsule = guard.GetComponent<CapsuleCollider>();
            if (capsule != null)
                Object.DestroyImmediate(capsule);

            var controller = guard.GetComponent<CharacterController>();
            if (controller == null)
                controller = guard.gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;

            var serialized = new SerializedObject(guard);
            serialized.FindProperty("moveSpeed").floatValue = 3f;
            serialized.FindProperty("maxHP").intValue = 3;
            serialized.FindProperty("detectionRange").floatValue = 10f;
            serialized.FindProperty("waypointWaitTime").floatValue = 0.75f;
            serialized.FindProperty("waypointArrivalDistance").floatValue = 0.3f;
            serialized.FindProperty("chaseStopDistance").floatValue = 1.5f;
            // Prefab routes are assigned per level; keep scene waypoint references intact.
            if (prefab)
                serialized.FindProperty("waypoints").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
