using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class Level01Designer
    {
        private const string ScenePath = "Assets/Scenes/Level_01.unity";

        [MenuItem("Possession/P4/Refine Existing Level_01")]
        public static void RefineLevel01()
        {
            // Refine the integrated scene instead of rebuilding teammates' prefabs or HUD.
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var previous = GameObject.Find("P4_LevelLayout");
            if (previous != null) Object.DestroyImmediate(previous);
            var layout = new GameObject("P4_LevelLayout");

            foreach (string name in new[] { "Wall_Left", "Wall_Right", "Wall_Back", "Wall_Front",
                "Divider1_Left", "Divider1_Right", "Divider2_Left", "Divider2_Right" })
            {
                var item = Require(name);
                var position = item.transform.position;
                position.y = 3f;
                item.transform.position = position;
                var scale = item.transform.localScale;
                scale.y = 6f; // Spirit's jump cannot bypass a puzzle or leave the level.
                item.transform.localScale = scale;
            }

            // A 3.2 m opening gives Heavy room to pass and Spirit room to eject safely.
            Resize("Divider1_Left", new Vector3(-3.8f, 3f, 12f), new Vector3(4.4f, 6f, 0.5f));
            Resize("Divider1_Right", new Vector3(3.8f, 3f, 12f), new Vector3(4.4f, 6f, 0.5f));
            Resize("Divider2_Left", new Vector3(-3.8f, 3f, 24f), new Vector3(4.4f, 6f, 0.5f));
            Resize("Divider2_Right", new Vector3(3.8f, 3f, 24f), new Vector3(4.4f, 6f, 0.5f));
            Resize("Door", new Vector3(0f, 3f, 24f), new Vector3(3.2f, 6f, 0.5f));
            Resize("BreakableWall", new Vector3(0f, 3f, 33f), new Vector3(3.2f, 6f, 0.5f));
            Barrier(layout.transform, "WallPuzzle_Left", -3.8f);
            Barrier(layout.transform, "WallPuzzle_Right", 3.8f);

            Require("Spirit").transform.position = new Vector3(0f, 1.15f, 3.5f);
            // Projectile travels at FirePoint height (1.6 m), so make its target easy to read/hit.
            Resize("Target", new Vector3(3.5f, 1.6f, 21f), new Vector3(1.2f, 1.2f, 0.6f));

            var target = Require("Target").GetComponent<Target>();
            var door = Require("Door").GetComponent<Door>();
            var doorSettings = new SerializedObject(door);
            doorSettings.FindProperty("requiredTargets").arraySize = 1;
            doorSettings.FindProperty("requiredTargets").GetArrayElementAtIndex(0).objectReferenceValue = target;
            doorSettings.ApplyModifiedPropertiesWithoutUndo();

            var exitObject = Require("ExitZone");
            var oldExit = exitObject.GetComponent<Door>();
            if (oldExit != null) Object.DestroyImmediate(oldExit);
            var exit = exitObject.GetComponent<LevelExit>() ?? exitObject.AddComponent<LevelExit>();
            var exitSettings = new SerializedObject(exit);
            exitSettings.FindProperty("puzzleDoor").objectReferenceValue = door;
            exitSettings.FindProperty("breakableWall").objectReferenceValue = Require("BreakableWall").GetComponent<BreakableWall>();
            exitSettings.ApplyModifiedPropertiesWithoutUndo();
            exitObject.GetComponent<BoxCollider>().isTrigger = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("P4 Level_01 refined: three rooms, sealed puzzle passages, spirit-only gated exit.");
        }

        private static GameObject Require(string name)
        {
            var item = GameObject.Find(name);
            if (item == null) throw new System.InvalidOperationException("Level_01 is missing " + name);
            return item;
        }

        private static void Resize(string name, Vector3 position, Vector3 scale)
        {
            var item = Require(name);
            item.transform.position = position;
            item.transform.localScale = scale;
        }

        private static void Barrier(Transform parent, string name, float x)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = new Vector3(x, 3f, 33f);
            wall.transform.localScale = new Vector3(4.4f, 6f, 0.5f);
            wall.isStatic = true;
            wall.GetComponent<Renderer>().sharedMaterial = Require("Wall_Left").GetComponent<Renderer>().sharedMaterial;
        }

    }
}
