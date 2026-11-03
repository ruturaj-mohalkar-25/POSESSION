using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Possession.Editor
{
    public static class WebGLBuild
    {
        [Serializable]
        private sealed class BuildInfo
        {
            public string sourceCommit;
            public string integrationBase;
            public string collectiblesRevision;
            public string ejectionRevision;
            public string[] campaignOrder;
            public string unityVersion;
            public string builtAtUtc;
            public string[] scenes;
            public string compression;
            public bool decompressionFallback;
        }

        [MenuItem("Possession/Build WebGL for GitHub Pages")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building WebGL.");

            string output = Environment.GetEnvironmentVariable("POSSESSION_WEBGL_OUTPUT");
            if (string.IsNullOrWhiteSpace(output)) output = "WebGLBuild";
            output = Path.GetFullPath(output);
            string project = Path.GetFullPath(".");
            if (output == project || output.StartsWith(Path.Combine(project, "Assets") + Path.DirectorySeparatorChar)
                || output == Path.Combine(project, "Assets"))
                throw new InvalidOperationException("Choose a separate WebGL output folder.");

            WebPreviewSetup.Prepare();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/MainMenu.unity")
                throw new InvalidOperationException("The enabled build scenes must start at MainMenu.");
            foreach (string scene in scenes)
                if (!File.Exists(scene)) throw new FileNotFoundException("Missing build scene", scene);

            // GitHub Pages does not supply Unity compression headers; use the loader fallback.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            AssetDatabase.SaveAssets();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);

            var info = new BuildInfo
            {
                sourceCommit = Environment.GetEnvironmentVariable("POSSESSION_SOURCE_COMMIT") ?? "local",
                integrationBase = "3b0ffaa2a87024293085f750aa92f110efaba32e",
                collectiblesRevision = "90c36f3e7b8070b3b1cb073012ee17ca1a7526c0",
                ejectionRevision = "7364f5dc57c3047a7037f1b988552ba635ff720f",
                campaignOrder = LevelFlow.CampaignScenes.ToArray(),
                unityVersion = Application.unityVersion,
                builtAtUtc = DateTime.UtcNow.ToString("o"),
                scenes = scenes,
                compression = "gzip",
                decompressionFallback = true
            };
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            File.WriteAllText(Path.Combine(output, "build-info.json"), JsonUtility.ToJson(info, true) + "\n");
            Debug.Log($"WebGL build ready: {output}, {scenes.Length} scenes, {report.summary.totalSize} bytes.");
        }
    }
}
