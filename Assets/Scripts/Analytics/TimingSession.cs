using System;
using System.Collections.Generic;
using UnityEngine;

namespace Possession
{
    [Serializable]
    public sealed class ObjectiveTiming
    {
        public int step;
        public string eventType;
        public string objectName;
        public float durationSeconds;
        public float elapsedSeconds;
        public bool completed;
    }

    [Serializable]
    public sealed class PlaytestTimingReport
    {
        public int schemaVersion = 1;
        public string attemptId;
        public string levelName;
        public string startedUtc;
        public string captureContext;
        public string outcome = "InProgress";
        public float firstPossessionSeconds = -1f;
        public string firstPossessedForm;
        public float elapsedSeconds;
        public ObjectiveTiming[] objectives = Array.Empty<ObjectiveTiming>();
    }

    // Times are gameplay seconds supplied by the tracker, so pauses do not inflate a step.
    public sealed class TimingSession
    {
        private readonly PlaytestTimingReport report;
        private readonly List<ObjectiveTiming> steps = new();
        private float stepStartedAt;
        public bool Finished => report.outcome != "InProgress";

        public TimingSession(string levelName, string context)
        {
            report = new PlaytestTimingReport
            {
                attemptId = Guid.NewGuid().ToString("N"),
                levelName = levelName,
                startedUtc = DateTime.UtcNow.ToString("O"),
                captureContext = context
            };
        }

        public void Possessed(float elapsed, string form)
        {
            if (Finished || report.firstPossessionSeconds >= 0f) return;
            report.firstPossessionSeconds = Mathf.Max(0f, elapsed);
            report.firstPossessedForm = form;
        }

        public void Objective(string eventType, string name, float elapsed)
        {
            if (!Finished) AddStep(eventType, name, elapsed, true);
        }

        public bool Finish(string outcome, float elapsed)
        {
            if (Finished) return false;
            bool complete = outcome == "Completed";
            AddStep(complete ? "Exit" : "Incomplete", complete ? "Exit" : "Unfinished objective",
                elapsed, complete);
            report.outcome = outcome;
            return true;
        }

        private void AddStep(string kind, string name, float elapsed, bool completed)
        {
            elapsed = Mathf.Max(stepStartedAt, elapsed);
            steps.Add(new ObjectiveTiming
            {
                step = steps.Count + 1,
                eventType = kind,
                objectName = name,
                durationSeconds = elapsed - stepStartedAt,
                elapsedSeconds = elapsed,
                completed = completed
            });
            stepStartedAt = elapsed;
            report.elapsedSeconds = elapsed;
        }

        public PlaytestTimingReport Snapshot(float elapsed)
        {
            report.objectives = steps.ToArray();
            if (!Finished) report.elapsedSeconds = Mathf.Max(stepStartedAt, elapsed);
            // Consumers can inspect or export a snapshot without mutating the active attempt.
            return JsonUtility.FromJson<PlaytestTimingReport>(JsonUtility.ToJson(report));
        }
    }
}
