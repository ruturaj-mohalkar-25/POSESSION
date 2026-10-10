using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class HeavySetup
    {
        [MenuItem("Possession/P3/Update Heavy Prefab and Level Component")]
        public static void PrepareAssets()
        {
            // Batch invocation is also used for reproducible prefab/scene migration.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            const string prefabPath = "Assets/Prefabs/Enemies/Heavy.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Configure(root.GetComponent<HeavyEnemy>(), true);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity");
            foreach (var heavy in Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None))
                Configure(heavy, false);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("P3: Heavy prefab and Level_01 component configured.");
        }

        private static void Configure(HeavyEnemy heavy, bool prefab)
        {
            var capsule = heavy.GetComponent<CapsuleCollider>();
            if (capsule != null)
                Object.DestroyImmediate(capsule);

            var controller = heavy.GetComponent<CharacterController>();
            if (controller == null)
                controller = heavy.gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;

            var serialized = new SerializedObject(heavy);
            serialized.FindProperty("moveSpeed").floatValue = 1.8f;
            serialized.FindProperty("maxHP").intValue = 5;
            // Scene waypoints stay assigned; prefab waypoints must be supplied per level.
            if (prefab)
                serialized.FindProperty("waypoints").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
