using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Possession.Editor
{
    public static class PlaytestFeedbackBuild
    {
        [MenuItem("Possession/Build Playtest Feedback WebGL")]
        public static void WebGL()
        {
            string path = Environment.GetEnvironmentVariable("POSSESSION_FEEDBACK_BUILD") ?? "Builds/PlaytestFeedback";
            var compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            bool hashes = PlayerSettings.WebGL.nameFilesAsHashes;
            bool caching = PlayerSettings.WebGL.dataCaching;
            int width = PlayerSettings.defaultWebScreenWidth, height = PlayerSettings.defaultWebScreenHeight;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.dataCaching = false;
                PlayerSettings.defaultWebScreenWidth = 1280;
                PlayerSettings.defaultWebScreenHeight = 720;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = path,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Playtest build failed: " + report.summary.result);
                Debug.Log("PLAYTEST_FEEDBACK_BUILD_SUCCEEDED " + path);
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.WebGL.nameFilesAsHashes = hashes;
                PlayerSettings.WebGL.dataCaching = caching;
                PlayerSettings.defaultWebScreenWidth = width;
                PlayerSettings.defaultWebScreenHeight = height;
            }
        }
    }
}
