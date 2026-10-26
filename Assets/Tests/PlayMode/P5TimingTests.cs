using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class P5TimingTests
    {
        private Scene scene;
        private P5TimingTracker tracker;

        [SetUp]
        public void Setup()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            scene = SceneManager.CreateScene("P5 timing test");
            tracker = Create("Timing").AddComponent<P5TimingTracker>();
            tracker.PersistReports = false;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.UnloadSceneAsync(scene);
            EventBus.Clear();
        }

        [Test]
        public void FirstPossessionIsMeasuredFromLevelStartAndRecordedOnlyOnce()
        {
            var session = new TimingSession("Level_02", "test");
            session.Possessed(6f, "Guard");
            session.Possessed(20f, "Heavy");
            var report = session.Snapshot(25f);
            Assert.That(report.firstPossessionSeconds, Is.EqualTo(6f));
            Assert.That(report.firstPossessedForm, Is.EqualTo("Guard"));
        }

        [Test]
        public void ZeroSecondPossessionIsDifferentFromNeverPossessing()
        {
            var session = new TimingSession("Level_01", "test");
            Assert.That(session.Snapshot(0f).firstPossessionSeconds, Is.EqualTo(-1f));
            session.Possessed(0f, "Guard");
            session.Possessed(8f, "Heavy");
            Assert.That(session.Snapshot(8f).firstPossessionSeconds, Is.Zero);
        }

        [Test]
        public void ObjectiveDurationsAreIntervalsIncludingTheFinalExitStep()
        {
            var session = new TimingSession("Level_03", "test");
            session.Objective("DoorOpened", "Door", 12f);
            session.Objective("WallBroken", "Wall 1", 20f);
            session.Objective("WallBroken", "Wall 2", 35f);
            session.Finish("Completed", 40f);
            var report = session.Snapshot(50f);
            Assert.That(report.objectives.Select(s => s.durationSeconds), Is.EqualTo(new[] { 12f, 8f, 15f, 5f }));
            Assert.That(report.objectives.All(s => s.completed), Is.True);
            Assert.That(report.objectives.Last().eventType, Is.EqualTo("Exit"));
            Assert.That(report.elapsedSeconds, Is.EqualTo(40f));
            Assert.That(report.objectives.Sum(s => s.durationSeconds), Is.EqualTo(report.elapsedSeconds));
        }

        [TestCase("Failed")]
        [TestCase("Died")]
        [TestCase("Abandoned")]
        public void UnsuccessfulAttemptRetainsTimeSpentOnUnfinishedStep(string outcome)
        {
            var session = new TimingSession("Level_02", "test");
            session.Objective("DoorOpened", "Door", 10f);
            session.Finish(outcome, 24f);
            var report = session.Snapshot(30f);
            Assert.That(report.outcome, Is.EqualTo(outcome));
            Assert.That(report.firstPossessionSeconds, Is.EqualTo(-1f));
            Assert.That(report.objectives.Last().completed, Is.False);
            Assert.That(report.objectives.Last().durationSeconds, Is.EqualTo(14f));
        }

        [Test]
        public void TerminalEventsCannotOverwriteACompletedReport()
        {
            var session = new TimingSession("Level_01", "test");
            Assert.That(session.Finish("Completed", 20f), Is.True);
            Assert.That(session.Finish("Failed", 30f), Is.False);
            session.Objective("WallBroken", "Late event", 31f);
            session.Possessed(32f, "Guard");
            var report = session.Snapshot(40f);
            Assert.That(report.objectives, Has.Length.EqualTo(1));
            Assert.That(report.outcome, Is.EqualTo("Completed"));
            Assert.That(report.firstPossessionSeconds, Is.EqualTo(-1f));
        }

        [Test]
        public void SnapshotsCannotChangeTheRecordedSession()
        {
            var session = new TimingSession("Level_01", "test");
            session.Objective("DoorOpened", "Door", 10f);
            var snapshot = session.Snapshot(12f);
            snapshot.objectives[0].durationSeconds = 999f;
            snapshot.outcome = "Changed";
            Assert.That(session.Snapshot(12f).objectives[0].durationSeconds, Is.EqualTo(10f));
            Assert.That(session.Finished, Is.False);
        }

        [Test]
        public void JsonAndCsvExportPreserveValuesNamesAndInvariantDecimals()
        {
            var session = new TimingSession("Level, \"02\"", "test");
            session.Possessed(1.25f, "Guard");
            session.Objective("DoorOpened", "Door, \"A\"", 4.5f);
            session.Finish("Completed", 8f);
            var report = session.Snapshot(8f);
            string directory = Path.Combine(Path.GetTempPath(), "p5-timing-test-" + Guid.NewGuid().ToString("N"));
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                TimingReportStore.WriteFiles(directory, report);
                var read = JsonUtility.FromJson<PlaytestTimingReport>(File.ReadAllText(Path.Combine(directory, report.attemptId + ".json")));
                Assert.That(read.firstPossessionSeconds, Is.EqualTo(1.25f));
                Assert.That(read.objectives[0].objectName, Is.EqualTo("Door, \"A\""));
                string csv = File.ReadAllText(Path.Combine(directory, report.attemptId + ".csv"));
                StringAssert.Contains(",1.25,true", csv);
                StringAssert.Contains("\"Door, \"\"A\"\"\"", csv);
                Assert.That(csv.Split('\n').Length, Is.EqualTo(5));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator SharedEventsRecordOnceAndIgnoreObjectsFromOtherScenes()
        {
            var guard = Create("Guard").AddComponent<GuardEnemy>();
            guard.enabled = false;
            var door = Create("Door").AddComponent<Door>();
            var wall = Create("Wall").AddComponent<BreakableWall>();
            yield return new WaitForSeconds(0.06f);
            EventBus.Possessed?.Invoke(guard, PlayerState.Possessed_Guard);
            EventBus.DoorOpened?.Invoke(door);
            EventBus.DoorOpened?.Invoke(door);
            EventBus.WallBroken?.Invoke(wall);
            var foreign = new GameObject("Foreign Door").AddComponent<Door>();
            try { EventBus.DoorOpened?.Invoke(foreign); }
            finally { UnityEngine.Object.Destroy(foreign.gameObject); }
            Assert.That(tracker.CurrentReport.firstPossessionSeconds, Is.GreaterThan(0f));
            Assert.That(tracker.CurrentReport.objectives, Has.Length.EqualTo(2));
            EventBus.LevelCompletedWithStats?.Invoke(3, 5f);
            EventBus.LevelCompleted?.Invoke();
            EventBus.LevelFailed?.Invoke();
            Assert.That(tracker.CurrentReport.outcome, Is.EqualTo("Completed"));
            Assert.That(tracker.CurrentReport.objectives, Has.Length.EqualTo(3));
            Assert.That(tracker.CurrentReport.elapsedSeconds, Is.EqualTo(5f));
        }

        [UnityTest]
        public IEnumerator GameplayClockDoesNotCountPausedTime()
        {
            var guard = Create("Guard").AddComponent<GuardEnemy>();
            guard.enabled = false;
            float before = tracker.CurrentReport.elapsedSeconds;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.08f);
            EventBus.Possessed?.Invoke(guard, PlayerState.Possessed_Guard);
            Assert.That(tracker.CurrentReport.firstPossessionSeconds, Is.EqualTo(before).Within(0.01f));
        }

        [Test]
        public void DisablingTrackerClosesAttemptAndUnsubscribes()
        {
            tracker.enabled = false;
            EventBus.LevelCompleted?.Invoke();
            Assert.That(tracker.CurrentReport.outcome, Is.EqualTo("Abandoned"));
            Assert.That(tracker.CurrentReport.objectives.Last().completed, Is.False);
        }

        [UnityTest]
        public IEnumerator TimeoutIsCapturedEvenWhenGameManagerDoesNotRaiseLevelFailed()
        {
            tracker.enabled = false;
            UnityEngine.Object.DestroyImmediate(tracker.gameObject);
            var manager = Create("GameManager").AddComponent<GameManager>();
            typeof(GameManager).GetField("levelTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 0.02f);
            tracker = P5TimingInstaller.Install(scene);
            tracker.PersistReports = false;
            Assert.That(P5TimingInstaller.Install(scene), Is.SameAs(tracker));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(manager.CurrentState, Is.EqualTo(LevelState.Failed));
            Assert.That(tracker.CurrentReport.outcome, Is.EqualTo("Failed"));
            Assert.That(tracker.CurrentReport.objectives.Last().completed, Is.False);
        }

        private GameObject Create(string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }
    }
}
