using System.Collections;
using UnityEngine;

namespace Possession
{
    [DisallowMultipleComponent]
    public sealed class TutorialGuide : MonoBehaviour
    {
        [SerializeField] private SpiritController spirit;
        [SerializeField] private PossessionController possession;
        [SerializeField] private GuardEnemy guard;
        [SerializeField] private HeavyEnemy heavy;
        [SerializeField] private Target target;
        [SerializeField] private BreakableWall wall;
        [SerializeField] private BoxCollider exitTrigger;
        [SerializeField] private GameManager manager;
        [SerializeField] private HUDManager hud;
        [SerializeField] private Door door;
        [SerializeField] private RectTransform healthBar;
        [SerializeField] private RectTransform healthText;
        [SerializeField] private RectTransform levelClock;
        [SerializeField] private RectTransform possessionClock;
        [SerializeField] private float contextRange = 7f;

        private bool restartQueued;
        private EnemyBase hostileEnemy;
        private bool introShown, guardPopupShown, heavyPopupShown;
        private float guardRoomZ = 12f;
        private float heavyRoomZ = 24f;

        public enum PopupState { None, SpiritIntro, HudIntro, Guard, Heavy }
        public PopupState ActivePopup { get; private set; } = PopupState.None;

        public TutorialProgress Progress { get; } = new();
        public GuardEnemy Guard => guard;
        public HeavyEnemy Heavy => heavy;
        public PossessionController Possession => possession;
        public SpiritController Spirit => spirit;
        public float LevelSeconds => manager != null ? manager.LevelTimer : 300f;
        public EnemyBase HostileEnemy => hostileEnemy != null ? hostileEnemy : guard;
        public string HostileName => HostileEnemy == heavy ? "HEAVY" : "GUARD";
        public Transform TargetBody => target.transform;
        public Transform WallBody => wall.transform;
        public Transform ExitBody => exitTrigger.transform;
        public TutorialFocus CoachingFocus => TutorialFocus.None;
        public bool IsCoaching => ActivePopup != PopupState.None;
        public int Charges => spirit != null && spirit.TryGetComponent(out PossessionCharge charge) ? charge.Count : 0;
        public string FailureMessage { get; private set; } = "CAUGHT -- WATCH THE RED CONE";

        public int Step
        {
            get
            {
                if (!Progress.HitTarget) return Progress.UsedGuard ? 2 : 1;
                if (!Progress.UsedHeavy) return Charges > 0 ? 4 : 3;
                if (!Progress.BrokeWall) return 5;
                return 6;
            }
        }

        private void OnEnable()
        {
            EventBus.Possessed += Possessed;
            EventBus.Ejected += Ejected;
            EventBus.TargetHit += TargetHit;
            EventBus.WallBroken += WallBroken;
            EventBus.LevelCompleted += Completed;
            EventBus.SpiritDied += SpiritDied;
            EventBus.LevelFailed += LevelFailed;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= Possessed;
            EventBus.Ejected -= Ejected;
            EventBus.TargetHit -= TargetHit;
            EventBus.WallBroken -= WallBroken;
            EventBus.LevelCompleted -= Completed;
            EventBus.SpiritDied -= SpiritDied;
            EventBus.LevelFailed -= LevelFailed;
            if (Time.timeScale == 0f && !Progress.Completed) Time.timeScale = 1f;
        }

        private void Start()
        {
            if (exitTrigger != null) exitTrigger.enabled = true;
            gameObject.AddComponent<TutorialOverlay>().Initialize(this);
            ChargeHUDInstaller.Install(gameObject.scene);
            ShowPopup(PopupState.SpiritIntro);
        }

        public void ShowPopup(PopupState popup)
        {
            ActivePopup = popup;
            if (popup != PopupState.None)
            {
                Time.timeScale = 0f;
                if (spirit != null) spirit.enabled = false;
                if (possession != null) possession.enabled = false;
            }
        }

        public void DismissPopup()
        {
            var previous = ActivePopup;
            ActivePopup = PopupState.None;

            if (previous == PopupState.SpiritIntro)
            {
                ShowPopup(PopupState.HudIntro);
                return;
            }

            Time.timeScale = 1f;
            if (spirit != null) spirit.enabled = true;
            if (possession != null) possession.enabled = true;

            if (!introShown)
            {
                introShown = true;
                if (!Progress.Started) Progress.Begin();
                UpdateObjective();
            }
        }

        public void BeginPractice()
        {
            if (!Progress.Started) Progress.Begin();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (ActivePopup != PopupState.None) return;
            UpdateObjective();
            CheckRoomEntry();
            if (!Progress.Started || Progress.Completed || Progress.Failed) return;
            if (spirit.CurrentHP <= 0 || manager != null && manager.CurrentState == LevelState.Failed)
                QueueFailure(spirit.CurrentHP <= 0 ? "CAUGHT -- WATCH THE RED CONE" : "TIME UP -- MOVE WITH PURPOSE");
        }

        private void CheckRoomEntry()
        {
            if (spirit == null || ActivePopup != PopupState.None
                || Progress.Completed || Progress.Failed
                || possession.CurrentState != PlayerState.Spirit) return;

            float z = spirit.transform.position.z;
            if (!guardPopupShown && z >= guardRoomZ)
            {
                guardPopupShown = true;
                ShowPopup(PopupState.Guard);
            }
            else if (!heavyPopupShown && z >= heavyRoomZ)
            {
                heavyPopupShown = true;
                ShowPopup(PopupState.Heavy);
            }
        }

        private void UpdateObjective()
        {
            if (hud != null && !Progress.Completed && !Progress.Failed)
                hud.SetLevelObjective(Title);
        }

        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (enemy == guard || enemy == heavy) Progress.Possessed(state);
        }

        private void Ejected(EnemyBase enemy, bool forced)
        {
            if (enemy != guard && enemy != heavy) return;
            hostileEnemy = forced ? enemy : hostileEnemy;
            Progress.Ejected(enemy == guard, forced);
        }

        private void TargetHit(Target hit)
        {
            if (hit == target) Progress.TargetWasHit();
        }

        private void WallBroken(BreakableWall broken)
        {
            if (broken == wall) Progress.WallWasBroken();
        }

        private void Completed() => Progress.Finish();
        private void SpiritDied() => QueueFailure("CAUGHT -- WATCH THE RED CONE");
        private void LevelFailed() => QueueFailure(spirit != null && spirit.CurrentHP <= 0
            ? "CAUGHT -- WATCH THE RED CONE" : "TIME UP -- MOVE WITH PURPOSE");

        private void QueueFailure(string message)
        {
            if (Progress.Completed || restartQueued) return;
            FailureMessage = message;
            Progress.Fail();
            restartQueued = true;
            StartCoroutine(RestartSoon());
        }

        private IEnumerator RestartSoon()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            Replay();
        }

        public string Title
        {
            get
            {
                if (Progress.Completed) return "PRACTICE COMPLETE";
                if (Progress.Failed) return FailureMessage;
                if (!Progress.HitTarget)
                {
                    if (possession.CurrentState == PlayerState.Possessed_Guard) return "LMB - HIT THE TARGET";
                    if (Charges == 0) return "COLLECT WHITE SOUL - +1 CHARGE";
                    return "POSSESS THE GUARD - PRESS E";
                }
                if (possession.CurrentState == PlayerState.Possessed_Guard) return "E - RETURN TO SPIRIT";
                if (!Progress.UsedHeavy)
                {
                    if (Charges == 0) return "COLLECT WHITE SOUL - +1 CHARGE";
                    return "POSSESS THE HEAVY - PRESS E";
                }
                if (!Progress.BrokeWall)
                {
                    if (possession.CurrentState == PlayerState.Possessed_Heavy)
                        return "LMB - SMASH THE CRACKED WALL";
                    if (Charges == 0) return "COLLECT WHITE SOUL - +1 CHARGE";
                    return "POSSESS THE HEAVY - PRESS E";
                }
                return possession.CurrentState == PlayerState.Spirit ? "ENTER THE GREEN EXIT" : "E - RETURN TO SPIRIT";
            }
        }

        public string Instruction => Title;

        public string ControlsLegend
        {
            get
            {
                if (possession.CurrentState == PlayerState.Possessed_Guard)
                    return "WASD  Move\nLMB  Shoot\nE  Eject";
                if (possession.CurrentState == PlayerState.Possessed_Heavy)
                    return "WASD  Move\nLMB  Smash\nE  Eject";
                return "WASD  Move\nE  Possess";
            }
        }

        public bool Nearby(out Transform body, out string text, out bool danger)
        {
            body = null;
            text = "";
            danger = false;
            if (!Progress.Started || Progress.Failed || Progress.Completed
                || possession.CurrentState != PlayerState.Spirit) return false;
            EnemyBase best = null;
            float closest = contextRange;
            foreach (EnemyBase enemy in new EnemyBase[] { guard, heavy })
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                float distance = Vector3.Distance(spirit.transform.position, enemy.transform.position);
                if (distance > closest) continue;
                best = enemy;
                closest = distance;
            }
            if (best == null) return false;
            body = best.transform;
            danger = best.IsHostile;
            text = best == possession.FindPossessableEnemy() ? "E" : "";
            return true;
        }

        public void ContinueCoaching() => DismissPopup();
        public RectTransform[] FocusWidgets(TutorialFocus focus) => focus switch
        {
            TutorialFocus.LevelClock => new[] { levelClock },
            TutorialFocus.Health => new[] { healthText, healthBar },
            TutorialFocus.PossessionClock => new[] { possessionClock },
            _ => System.Array.Empty<RectTransform>()
        };
        public string MovementKey => "WASD";
        public string MovementAction => "MOVE";

        public void Replay() => LevelFlow.Load(TutorialScene.Name);
        public void MainMenu() => LevelFlow.Load("MainMenu");
        public void PlayLevelOne() => LevelFlow.Load(CampaignLevels.FirstScene);
    }
}
