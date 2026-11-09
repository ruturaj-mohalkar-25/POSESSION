using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class CampaignSpaceSetup
    {
        public const float HorizontalScale = 2f;
        private readonly struct Original
        {
            public readonly Vector3 Position, Scale;
            public Original(Transform t) { Position = t.position; Scale = t.localScale; }
        }

        [MenuItem("Possession/Level Design/Expand Campaign Levels 1 and 2")]
        public static void ExpandLevels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before expanding levels.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in CampaignLevels.ScenePaths.Take(2))
            {
                var scene = EditorSceneManager.OpenScene(path);
                Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Campaign Levels 1 and 2 expanded: twice the room/passages width and depth; shared gameplay unchanged.");
        }

        public static void Apply(Scene scene)
        {
            if (scene.name != CampaignLevels.FirstScene && scene.name != CampaignLevels.SecondScene) return;
            var roots = scene.GetRootGameObjects();
            var marker = roots.Select(r => r.GetComponent<CampaignLayoutScale>()).FirstOrDefault(m => m != null);
            if (marker != null && Mathf.Approximately(marker.HorizontalScale, HorizontalScale)) return;
            float previous = marker != null ? marker.HorizontalScale : 1f;
            float ratio = HorizontalScale / previous;
            Vector3 Map(Vector3 p) => new Vector3(p.x * ratio, p.y, p.z * ratio);
            var originals = new Dictionary<Transform, Original>();
            foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) originals[t] = new Original(t);
            var spirit = roots.SelectMany(r => r.GetComponentsInChildren<SpiritController>(true)).Single();
            var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
            var cameraOffsets = cameras.Select(c => c.transform.position - spirit.transform.position).ToArray();

            void Expand(Transform t)
            {
                if (t.GetComponent<Canvas>() != null || t.GetComponent<Camera>() != null || t.GetComponent<Light>() != null
                    || t.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return;
                Original old = originals[t];
                t.position = Map(old.Position);
                bool actor = t.GetComponent<EnemyBase>() != null || t.GetComponent<SpiritController>() != null;
                bool pickup = t.GetComponent<PossessionChargePickup>() != null;
                bool smallProp = t.GetComponent<Target>() != null || t.GetComponent<TextMesh>() != null
                    || t.name.StartsWith("Cover_") || t.name == "Dust footprint";
                if (actor || pickup || smallProp)
                {
                    if (t.GetComponent<EnemyBase>() is EnemyBase enemy)
                    {
                        var so = new SerializedObject(enemy);var bounds = so.FindProperty("homeBounds");
                        if (bounds != null)
                        {
                            Bounds home = bounds.boundsValue;home.center = Map(home.center);
                            home.size = Vector3.Scale(home.size, new Vector3(ratio, 1f, ratio));
                            bounds.boundsValue = home;so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    return;
                }
                // Resize geometry and trigger volumes, not the character/ability subtrees above.
                bool shape = t.GetComponent<Renderer>() != null || t.GetComponent<Collider>() != null;
                if (shape)
                {
                    t.localScale = Vector3.Scale(old.Scale, new Vector3(ratio, 1f, ratio));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    return;
                }
                foreach (Transform child in t) Expand(child);
            }
            foreach (var root in roots) Expand(root.transform);
            for (int i = 0; i < cameras.Length; i++)
                cameras[i].transform.position = spirit.transform.position + cameraOffsets[i];
            foreach (var director in roots.SelectMany(r => r.GetComponentsInChildren<Level02Director>(true)))
            {
                var so = new SerializedObject(director);var courtyard = so.FindProperty("courtyard");
                courtyard.vector3Value = Map(courtyard.vector3Value);so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (marker == null)
            {
                var node = new GameObject("Campaign_Layout_Spacing");SceneManager.MoveGameObjectToScene(node, scene);
                marker = node.AddComponent<CampaignLayoutScale>();
            }
            marker.RecordAppliedScale(HorizontalScale);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
