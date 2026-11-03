using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Possession.Editor
{
    public static class P5VisibilityTools
    {
        [MenuItem("Possession/P5/Visibility/Toggle Wall Fade (Play Mode)")]
        public static void Toggle()
        {
            var fader = WallOcclusionInstaller.Install();
            if (fader == null) return;
            fader.enabled = !fader.enabled;
            Debug.Log("Wall visibility fade " + (fader.enabled ? "enabled" : "disabled") + ".");
        }

        [MenuItem("Possession/P5/Visibility/Toggle Wall Fade (Play Mode)", true)]
        private static bool CanToggle() => Application.isPlaying;

        public static void CaptureComparisons()
        {
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "VisibilityPreviews");
            Directory.CreateDirectory(directory);
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in new[] { "Level_01", "Level_02", "Level_03" })
                {
                    EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    var player = Object.FindFirstObjectByType<PossessionController>();
                    var camera = Camera.main;
                    // Sample the camera-facing side of an existing room divider or boundary.
                    Vector3 position = name == "Level_01" ? new Vector3(-3f, 1.15f, 13f)
                        : name == "Level_02" ? new Vector3(-6f, 1.15f, 13f)
                        : new Vector3(-8f, 1.15f, 15f);
                    player.transform.position = position;
                    camera.transform.position = position + new Vector3(0f, 25f, -13f);
                    camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                    Physics.SyncTransforms();
                    var fader = camera.GetComponent<WallOcclusionFader>();
                    if (fader == null) fader = camera.gameObject.AddComponent<WallOcclusionFader>();
                    fader.RestoreAll();
                    Capture(camera, Path.Combine(directory, name + "-before.png"));
                    fader.RefreshVisibility(1f);
                    if (fader.FadedRendererCount == 0)
                        throw new InvalidOperationException(name + " preview did not find an obstructing wall.");
                    Capture(camera, Path.Combine(directory, name + "-after.png"));
                    fader.RestoreAll();
                    Object.DestroyImmediate(fader);
                }
                Shader shader = Resources.Load<Shader>("Visibility/P5WallFade");
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                    throw new InvalidOperationException("Wall visibility shader failed to compile.");
                Debug.Log("P5 visibility comparisons rendered in " + directory);
            }
            finally
            {
                if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1400, 1000, 24);
            var texture = new Texture2D(1400, 1000, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1400, 1000), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
