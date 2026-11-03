using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Possession
{
    public sealed class FirebaseReporter : MonoBehaviour
    {
        private const string ProjectId = "possession-analytics";
        private const string BaseUrl = "https://firestore.googleapis.com/v1/projects/"
            + ProjectId + "/databases/(default)/documents/";

        public void ReportSession(AnalyticsData data)
        {
            string sessionDoc = BuildSessionDocument(data);
            StartCoroutine(Post("sessions", sessionDoc));
        }

        public void ReportTiming(PlaytestTimingReport report)
        {
            string timingDoc = BuildTimingDocument(report);
            StartCoroutine(Post("timings", timingDoc));
        }

        private static System.Collections.IEnumerator Post(string collection, string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(BaseUrl + collection, "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[Firebase] " + collection + ": " + request.error);

            request.Dispose();
        }

        private static string BuildSessionDocument(AnalyticsData data)
        {
            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            StringField(sb, "levelName", data.levelName); sb.Append(',');
            StringField(sb, "timestamp", DateTime.UtcNow.ToString("O")); sb.Append(',');
            IntField(sb, "possessionCount", data.possessionCount); sb.Append(',');
            IntField(sb, "forcedEjectCount", data.forcedEjectCount); sb.Append(',');
            IntField(sb, "deathCount", data.deathCount); sb.Append(',');
            FloatField(sb, "totalPossessionTime", data.totalPossessionTime); sb.Append(',');
            FloatField(sb, "completionTime", data.completionTime); sb.Append(',');
            IntField(sb, "stars", data.stars); sb.Append(',');

            sb.Append("\"possessions\":{\"arrayValue\":{\"values\":[");
            if (data.possessions != null)
            {
                for (int i = 0; i < data.possessions.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var p = data.possessions[i];
                    sb.Append("{\"mapValue\":{\"fields\":{");
                    StringField(sb, "enemyType", p.enemyType); sb.Append(',');
                    FloatField(sb, "duration", p.duration); sb.Append(',');
                    sb.Append("\"forcedEject\":{\"booleanValue\":").Append(p.forcedEject ? "true" : "false").Append('}');
                    sb.Append("}}}");
                }
            }
            sb.Append("]}}");

            sb.Append("}}");
            return sb.ToString();
        }

        private static string BuildTimingDocument(PlaytestTimingReport report)
        {
            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            StringField(sb, "attemptId", report.attemptId); sb.Append(',');
            StringField(sb, "levelName", report.levelName); sb.Append(',');
            StringField(sb, "startedUtc", report.startedUtc); sb.Append(',');
            StringField(sb, "outcome", report.outcome); sb.Append(',');
            FloatField(sb, "firstPossessionSeconds", report.firstPossessionSeconds); sb.Append(',');
            StringField(sb, "firstPossessedForm", report.firstPossessedForm ?? ""); sb.Append(',');
            FloatField(sb, "elapsedSeconds", report.elapsedSeconds); sb.Append(',');

            sb.Append("\"objectives\":{\"arrayValue\":{\"values\":[");
            if (report.objectives != null)
            {
                for (int i = 0; i < report.objectives.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    var o = report.objectives[i];
                    sb.Append("{\"mapValue\":{\"fields\":{");
                    IntField(sb, "step", o.step); sb.Append(',');
                    StringField(sb, "eventType", o.eventType); sb.Append(',');
                    StringField(sb, "objectName", o.objectName); sb.Append(',');
                    FloatField(sb, "durationSeconds", o.durationSeconds); sb.Append(',');
                    sb.Append("\"completed\":{\"booleanValue\":").Append(o.completed ? "true" : "false").Append('}');
                    sb.Append("}}}");
                }
            }
            sb.Append("]}}");

            sb.Append("}}");
            return sb.ToString();
        }

        private static void StringField(StringBuilder sb, string name, string value)
        {
            sb.Append('"').Append(name).Append("\":{\"stringValue\":\"")
              .Append((value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""))
              .Append("\"}");
        }

        private static void IntField(StringBuilder sb, string name, int value)
        {
            sb.Append('"').Append(name).Append("\":{\"integerValue\":\"")
              .Append(value).Append("\"}");
        }

        private static void FloatField(StringBuilder sb, string name, float value)
        {
            sb.Append('"').Append(name).Append("\":{\"doubleValue\":")
              .Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append('}');
        }
    }

    public static class FirebaseReporterInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (UnityEngine.Object.FindFirstObjectByType<FirebaseReporter>() != null) return;
            var go = new GameObject("FirebaseReporter");
            go.AddComponent<FirebaseReporter>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
    }
}
