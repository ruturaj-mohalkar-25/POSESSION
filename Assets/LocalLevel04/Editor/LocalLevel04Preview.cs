using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.LocalLevel04.Editor
{
    public static class LocalLevel04Preview
    {
        // Runs in an isolated editor copy; no cameras are saved into the playable scene.
        public static void Capture()
        {
            EditorSceneManager.OpenScene(LocalLevel04Builder.ScenePath);
            Camera camera = Camera.main;
            Save(camera, "Logs/local04-start.png", 1600, 1000);
            camera.transform.position = new Vector3(35, 26.45f, 7);
            Save(camera, "Logs/local04-central.png", 1600, 1000);
            camera.transform.position = new Vector3(78, 85, -45);
            camera.transform.LookAt(new Vector3(33, 0, 20));
            Save(camera, "Logs/local04-overview-3d.png", 1800, 1125);
        }
        private static void Save(Camera camera, string path, int width, int height)
        {
            var texture = new RenderTexture(width, height, 24);
            camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(texture);
        }
    }
}
