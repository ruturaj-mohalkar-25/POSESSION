using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class CampaignDoorSetup
    {
        [MenuItem("Possession/Level Design/Sync Campaign Sliding Doors")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before updating campaign doors.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                Apply(scene);
                CampaignDoorSecurityStyle.Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        public static void Apply(Scene scene)
        {
            if (!CampaignLevels.SceneNames.Contains(scene.name) || scene.name == CampaignLevels.ThirdScene) return;
            foreach (var door in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Door>(true)))
            {
                var so = new SerializedObject(door);
                var required = so.FindProperty("requiredTargets");
                if (required.arraySize == 0) continue;
                var box = door.GetComponent<BoxCollider>();
                if (box == null) throw new System.InvalidOperationException("Door needs a box blocker: " + door.name);
                var previous = door.transform.Find("Sliding door presentation");
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var root = new GameObject("Sliding door presentation").transform;
                root.SetParent(door.transform, false);
                root.localPosition = box.center;
                // A unit presentation box follows the original scaled/rotated blocker exactly.
                root.localScale = box.size;
                var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Level03Warehouse/Materials/Steel.mat");
                var signal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Level03Warehouse/Materials/cargo-entry_Signal.mat");
                var left = Cube(root, "Left sliding leaf", new Vector3(-.25f, 0, 0), new Vector3(.5f, .92f, .6f), steel);
                var right = Cube(root, "Right sliding leaf", new Vector3(.25f, 0, 0), new Vector3(.5f, .92f, .6f), steel);
                Cube(root, "Door jamb left", new Vector3(-.52f, 0, 0), new Vector3(.04f, 1f, 1.3f), steel);
                Cube(root, "Door jamb right", new Vector3(.52f, 0, 0), new Vector3(.04f, 1f, 1.3f), steel);
                Cube(root, "Door header", new Vector3(0, .475f, 0), new Vector3(1.08f, .05f, 1.3f), steel);
                var view = door.GetComponent<SlidingDoorView>() ?? door.gameObject.AddComponent<SlidingDoorView>();
                var settings = new SerializedObject(view);
                settings.FindProperty("width").floatValue = 1f;
                settings.FindProperty("clipToFrame").boolValue = true;
                settings.FindProperty("leftLeaf").objectReferenceValue = left;
                settings.FindProperty("rightLeaf").objectReferenceValue = right;
                settings.FindProperty("groupColor").colorValue = signal.color;
                var targets = settings.FindProperty("targets");
                var lamps = settings.FindProperty("lamps");
                targets.arraySize = lamps.arraySize = required.arraySize;
                for (int i = 0; i < required.arraySize; i++)
                {
                    targets.GetArrayElementAtIndex(i).objectReferenceValue = required.GetArrayElementAtIndex(i).objectReferenceValue;
                    var lamp = Cube(root, "Target lamp " + (i + 1), new Vector3(((i + .5f) / required.arraySize - .5f) * .75f, .425f, -.8f),
                        new Vector3(.25f / root.lossyScale.x, .25f / root.lossyScale.y, .15f / root.lossyScale.z), signal);
                    lamps.GetArrayElementAtIndex(i).objectReferenceValue = lamp.GetComponent<Renderer>();
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
                var oldRenderer = door.GetComponent<Renderer>();
                if (oldRenderer != null)
                {
                    oldRenderer.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(oldRenderer);
                }
                var oldModel = door.transform.Find("P5_Model");
                if (oldModel != null)
                {
                    oldModel.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(oldModel.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static Transform Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj.transform;
        }
    }
}
