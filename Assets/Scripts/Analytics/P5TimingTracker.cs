using System;
using System.Collections.Generic;
using UnityEngine;

namespace Possession
{
    [DisallowMultipleComponent]
    public sealed class P5TimingTracker : MonoBehaviour
    {
        [SerializeField] private bool persistReports = true;
        private readonly HashSet<UnityEngine.Object> completedObjects = new();
        private TimingSession session;
        private GameManager manager;
        private double startedAt;

        public bool PersistReports { get => persistReports; set => persistReports = value; }
        public PlaytestTimingReport CurrentReport => session?.Snapshot(Elapsed);
        public static PlaytestTimingReport LatestReport { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLatest() => LatestReport = null;

        private float Elapsed => manager != null ? manager.ElapsedTime
            : (float)(Time.timeAsDouble - startedAt);

        private void Awake()
        {
            manager = GetComponent<GameManager>();
            startedAt = Time.timeAsDouble;
            string context = Application.isBatchMode ? "automated" : Application.isEditor ? "editor" : "player";
            session = new TimingSession(gameObject.scene.name, context);
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.DoorOpened += OnDoorOpened;
            EventBus.WallBroken += OnWallBroken;
            EventBus.LevelCompleted += OnCompleted;
            EventBus.LevelCompletedWithStats += OnCompletedWithStats;
            EventBus.LevelFailed += OnFailed;
            EventBus.SpiritDied += OnDied;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.DoorOpened -= OnDoorOpened;
            EventBus.WallBroken -= OnWallBroken;
            EventBus.LevelCompleted -= OnCompleted;
            EventBus.LevelCompletedWithStats -= OnCompletedWithStats;
            EventBus.LevelFailed -= OnFailed;
            EventBus.SpiritDied -= OnDied;
            Finish("Abandoned", Elapsed);
        }

        private void LateUpdate()
        {
            // GameManager's countdown calls its own failure method without raising LevelFailed.
            if (manager == null || session.Finished) return;
            if (manager.CurrentState == LevelState.Failed) Finish("Failed", Elapsed);
            else if (manager.CurrentState == LevelState.Complete) Finish("Completed", Elapsed);
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            if (InThisScene(enemy)) session.Possessed(Elapsed, state.ToString());
        }

        private void OnDoorOpened(Door door) => Objective(door, "DoorOpened");
        private void OnWallBroken(BreakableWall wall) => Objective(wall, "WallBroken");

        private void Objective(Component item, string kind)
        {
            if (InThisScene(item) && completedObjects.Add(item))
                session.Objective(kind, item.name, Elapsed);
        }

        private bool InThisScene(Component item) => item != null && item.gameObject.scene == gameObject.scene;
        private void OnCompleted() => Finish("Completed", Elapsed);
        private void OnCompletedWithStats(int stars, float elapsed) => Finish("Completed", elapsed);
        private void OnFailed() => Finish("Failed", Elapsed);
        private void OnDied() => Finish("Died", Elapsed);

        private void Finish(string outcome, float elapsed)
        {
            if (session == null || !session.Finish(outcome, elapsed)) return;
            LatestReport = session.Snapshot(elapsed);
            // Automated fixtures must not be mixed into real playtest reports.
            if (!persistReports || Application.isBatchMode) return;
            try { TimingReportStore.Save(LatestReport); }
            catch (Exception error) { Debug.LogWarning("Could not save playtest timing: " + error.Message, this); }
            var firebase = FindFirstObjectByType<FirebaseReporter>();
            if (firebase != null) firebase.ReportTiming(LatestReport);
        }
    }
}
