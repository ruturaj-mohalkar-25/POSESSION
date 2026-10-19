using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class ValidateLevels
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/Level_02.unity",
            "Assets/Scenes/Level_03.unity",
        };

        [MenuItem("Possession/P4/Validate Levels")]
        public static void Validate()
        {
            int failed = 0;
            foreach (var path in Scenes)
                failed += ValidateScene(path) ? 0 : 1;

            if (failed == 0)
                Debug.Log("P4: all levels passed structural validation.");
            else
                Debug.LogError($"P4: {failed} level(s) failed validation. See the errors above.");
        }

        static bool ValidateScene(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogError($"P4 validate: scene not found at {path}. Build it first.");
                return false;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var issues = new List<string>();

            var targets = FindAll<Target>(roots);
            var doors = FindAll<Door>(roots);
            var walls = FindAll<BreakableWall>(roots);
            var exits = FindAll<LevelExit>(roots);
            var spawns = FindAll<SpawnPointMarker>(roots);

            if (targets.Count == 0) issues.Add("no Target placed");
            if (walls.Count == 0) issues.Add("no BreakableWall placed");

            foreach (var door in doors)
            {
                var required = new SerializedObject(door).FindProperty("requiredTargets");
                if (required.arraySize == 0)
                    issues.Add($"Door '{door.name}' has no required targets (it will never gate)");
            }

            if (exits.Count == 0)
            {
                issues.Add("no LevelExit placed");
            }
            else
            {
                foreach (var exit in exits)
                {
                    var so = new SerializedObject(exit);
                    if (so.FindProperty("puzzleDoor").objectReferenceValue == null)
                        issues.Add($"LevelExit '{exit.name}' is not watching a Door");
                    if (so.FindProperty("breakableWall").objectReferenceValue == null)
                        issues.Add($"LevelExit '{exit.name}' is not watching a BreakableWall");
                }
            }

            if (!spawns.Any(s => s.Kind == SpawnPointMarker.SpawnKind.Guard))
                issues.Add("no Guard spawn point");
            if (!spawns.Any(s => s.Kind == SpawnPointMarker.SpawnKind.Heavy))
                issues.Add("no Heavy spawn point");

            string level = System.IO.Path.GetFileNameWithoutExtension(path);
            if (issues.Count == 0)
            {
                Debug.Log($"P4 validate: {level} OK -- {targets.Count} target(s), {doors.Count} door(s), " +
                          $"{walls.Count} wall(s), {exits.Count} exit(s), {spawns.Count} spawn point(s).");
                return true;
            }

            Debug.LogError($"P4 validate: {level} has {issues.Count} issue(s):\n  - " + string.Join("\n  - ", issues));
            return false;
        }

        static List<T> FindAll<T>(GameObject[] roots) where T : Component
        {
            var found = new List<T>();
            foreach (var root in roots)
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }
    }
}
