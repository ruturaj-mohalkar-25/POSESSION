using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Possession
{
    public static class TimingReportStore
    {
        public const string LatestKey = "Possession.P5.LatestTiming.v1";
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "P5Timing");

        public static void Save(PlaytestTimingReport report)
        {
            string json = JsonUtility.ToJson(report, true);
            // PlayerPrefs is backed by browser storage in WebGL; no server is required.
            PlayerPrefs.SetString(LatestKey, json);
            PlayerPrefs.Save();
#if !UNITY_WEBGL || UNITY_EDITOR
            WriteFiles(DirectoryPath, report);
#endif
        }

        public static void WriteFiles(string directory, PlaytestTimingReport report)
        {
            Directory.CreateDirectory(directory);
            string stem = Path.Combine(directory, report.attemptId);
            File.WriteAllText(stem + ".json", JsonUtility.ToJson(report, true));
            File.WriteAllText(stem + ".csv", ToCsv(report));
        }

        public static string ToCsv(PlaytestTimingReport report)
        {
            var csv = new StringBuilder("attempt_id,level,outcome,metric,step,event,object,seconds,completed\n");
            if (report.firstPossessionSeconds >= 0f)
                Row(csv, report, "first_possession", "", "Possessed", report.firstPossessedForm,
                    report.firstPossessionSeconds, true);
            foreach (var step in report.objectives)
                Row(csv, report, "objective_step", step.step.ToString(CultureInfo.InvariantCulture),
                    step.eventType, step.objectName, step.durationSeconds, step.completed);
            return csv.ToString();
        }

        private static void Row(StringBuilder csv, PlaytestTimingReport report, string metric,
            string step, string kind, string name, float seconds, bool complete)
        {
            csv.Append(Quote(report.attemptId)).Append(',').Append(Quote(report.levelName)).Append(',')
                .Append(Quote(report.outcome)).Append(',').Append(metric).Append(',').Append(step).Append(',')
                .Append(Quote(kind)).Append(',').Append(Quote(name)).Append(',')
                .Append(seconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                .Append(complete ? "true" : "false").Append('\n');
        }

        private static string Quote(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    }
}
