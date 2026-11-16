using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class GoldPart1Tools
    {
        public static void Prepare()
        {
            foreach (var spec in new[] { ("DoorFace", new Color(.57f,.37f,.22f)),
                ("DoorFrame", new Color(.17f,.20f,.22f)), ("DoorBrass", new Color(.72f,.57f,.27f)) })
            {
                string path = "Assets/Resources/Gold/" + spec.Item1 + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
                material.color = spec.Item2; material.SetFloat("_Glossiness", .08f); EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
        public static void CaptureBefore() => Capture("Before");
        public static void CaptureAfter() { Prepare(); Capture("After"); }
        public static void BuildWebGL()
        {
            Prepare();
            var compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback, hashes = PlayerSettings.WebGL.nameFilesAsHashes;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true; PlayerSettings.WebGL.nameFilesAsHashes = true;
                var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    "Builds/Gold-Part1", BuildTarget.WebGL, BuildOptions.None);
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new InvalidOperationException("Part 1 browser build failed.");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback; PlayerSettings.WebGL.nameFilesAsHashes = hashes;
            }
        }
        public static void ValidateGeometry()
        {
            Prepare();
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                Func<string> signature = () => string.Join("\n", scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                    .OrderBy(c => c.GetInstanceID()).Select(c => c.GetInstanceID() + "|" + EditorJsonUtility.ToJson(c)
                        + "|" + c.transform.position.ToString("R") + "|" + c.transform.rotation.ToString("R") + "|" + c.transform.lossyScale.ToString("R")));
                string before = signature(); GoldPresentationInstaller.Install(scene);
                if (signature() != before) throw new InvalidOperationException("Original colliders changed: " + scene.name);
                Debug.Log("GOLD_COLLIDERS_UNCHANGED " + scene.name);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static void Capture(string prefix)
        {
            Directory.CreateDirectory("Docs/GoldPart1");
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                GoldPresentationInstaller.Install(scene);
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerIdentityView>();
                player.Refresh();
                var camera = Camera.main;
                camera.cullingMask &= ~(1 << 5);
                Vector3 at = player.transform.position;
                camera.transform.position = at + new Vector3(0, 25, -18);
                camera.transform.LookAt(at);
                Save(camera, $"{prefix}_{scene.name}_room");
                var door = UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None).OrderBy(d => d.name).First();
                at = door.GetComponent<Collider>().bounds.center;
                camera.transform.position = at + door.transform.forward * -11f + Vector3.up * 5f;
                camera.transform.LookAt(at);
                Save(camera, $"{prefix}_{scene.name}_door");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static void Save(Camera camera, string name)
        {
            var render = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var target = camera.targetTexture;
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes("Docs/GoldPart1/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = target; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(render);
            }
        }
        public static void Diagnose()
        {
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                GoldPresentationInstaller.Install(scene);
                var floors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(GoldRoomPresentation.IsFloor).ToArray();
                for (int i = 0; i < floors.Length; i++) for (int j = i + 1; j < floors.Length; j++)
                {
                    Bounds a = floors[i].bounds, b = floors[j].bounds;
                    float width = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
                    float depth = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                    if (width > .01f && depth > .01f && Mathf.Abs(a.max.y - b.max.y) < .002f)
                        Debug.Log($"GOLD_COPLANAR {scene.name}: {floors[i].name} / {floors[j].name}, area {width * depth:F3}, different material {floors[i].sharedMaterial != floors[j].sharedMaterial}");
                }
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
    }
}
