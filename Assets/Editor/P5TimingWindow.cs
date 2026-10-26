using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    public sealed class P5TimingWindow : EditorWindow
    {
        private PlaytestTimingReport report;
        private string[] files = Array.Empty<string>();
        private int selection;
        private Vector2 scroll;
        private string error;

        [MenuItem("Possession/P5/Analytics/Open Timing Results")]
        public static void Open() => GetWindow<P5TimingWindow>("P5 Timing Results");

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            error = null;
            try
            {
                files = Directory.Exists(TimingReportStore.DirectoryPath)
                    ? Directory.GetFiles(TimingReportStore.DirectoryPath, "*.json")
                        .OrderByDescending(File.GetLastWriteTimeUtc).ToArray() : Array.Empty<string>();
                selection = -1;
                var live = Application.isPlaying ? FindFirstObjectByType<P5TimingTracker>() : null;
                if (live != null) report = live.CurrentReport;
                else if (files.Length > 0) { selection = 0; LoadFile(); }
                else
                {
                    string json = PlayerPrefs.GetString(TimingReportStore.LatestKey, "");
                    report = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<PlaytestTimingReport>(json);
                }
            }
            catch (Exception exception) { error = exception.Message; }
            Repaint();
        }

        private void LoadFile() => report = JsonUtility.FromJson<PlaytestTimingReport>(File.ReadAllText(files[selection]));

        private void OnGUI()
        {
            GUILayout.Label("Playtest timing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Gameplay seconds exclude pauses. Objective steps end on door unlock, wall break, and exit. "
                + "Stopped or failed attempts keep an unfinished step.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh / current attempt")) Refresh();
                if (GUILayout.Button("Open data folder"))
                {
                    Directory.CreateDirectory(TimingReportStore.DirectoryPath);
                    EditorUtility.RevealInFinder(TimingReportStore.DirectoryPath);
                }
            }
            if (files.Length > 0)
            {
                var options = new[] { "Current / latest view" }
                    .Concat(files.Select(Path.GetFileNameWithoutExtension)).ToArray();
                int next = EditorGUILayout.Popup("Saved attempt", selection + 1, options) - 1;
                if (next != selection)
                {
                    selection = next;
                    try { if (selection < 0) Refresh(); else LoadFile(); error = null; }
                    catch (Exception exception) { error = exception.Message; }
                }
            }
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (report == null)
            {
                EditorGUILayout.HelpBox("No timing report yet. Play a level, possess an enemy, complete an objective, "
                    + "then refresh here. Finishing or stopping Play Mode saves the attempt.", MessageType.None);
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Level", report.levelName);
            EditorGUILayout.LabelField("Outcome", report.outcome);
            EditorGUILayout.LabelField("Capture", report.captureContext);
            EditorGUILayout.LabelField("Started (UTC)", report.startedUtc);
            EditorGUILayout.LabelField("Elapsed", report.elapsedSeconds.ToString("0.0") + " s");
            GUILayout.Space(8f);
            GUILayout.Label("Time to first possession", EditorStyles.boldLabel);
            if (report.firstPossessionSeconds < 0f)
                GUILayout.Label("Not reached — this attempt has no possession event.");
            else
                Bar(report.firstPossessedForm + "   " + report.firstPossessionSeconds.ToString("0.0") + " s",
                    report.firstPossessionSeconds, report.elapsedSeconds, new Color(0.35f, 0.65f, 0.95f));

            GUILayout.Space(10f);
            GUILayout.Label("Time on each objective step", EditorStyles.boldLabel);
            foreach (var step in report.objectives)
            {
                string label = step.step + ". " + step.eventType + " — " + step.objectName + "   "
                    + step.durationSeconds.ToString("0.0") + " s" + (step.completed ? "" : " (unfinished)");
                Bar(label, step.durationSeconds, report.elapsedSeconds,
                    step.completed ? new Color(0.8f, 0.67f, 0.2f) : Color.gray);
            }
            if (report.outcome == "InProgress") GUILayout.Label("The current unfinished step is saved when this attempt ends.");
            GUILayout.Space(12f);
            if (GUILayout.Button("Export displayed report as CSV"))
            {
                string path = EditorUtility.SaveFilePanel("Export timing CSV", TimingReportStore.DirectoryPath,
                    report.attemptId, "csv");
                if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, TimingReportStore.ToCsv(report));
            }
            EditorGUILayout.EndScrollView();
        }

        private static void Bar(string label, float value, float total, Color color)
        {
            EditorGUILayout.LabelField(label, EditorStyles.wordWrappedLabel);
            Rect rect = GUILayoutUtility.GetRect(1f, 15f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            rect.width *= total > 0f ? Mathf.Clamp01(value / total) : 0f;
            EditorGUI.DrawRect(rect, color);
            GUILayout.Space(4f);
        }
    }
}
