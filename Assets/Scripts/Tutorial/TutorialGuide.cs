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
        [SerializeField] private Door door;
        [SerializeField] private RectTransform healthBar;
        [SerializeField] private RectTransform healthText;
        [SerializeField] private RectTransform levelClock;
        [SerializeField] private RectTransform possessionClock;
        [SerializeField] private float contextRange = 7f;
        private TutorialOverlay overlay;
        private readonly float[] travel = new float[4];
        private Vector3 lastPosition;
        private bool wasSpirit = true;
        private bool ownsPause;
        private float jumpPressedUntil;
        private TutorialCoach coach;
        private bool guardExplained, heavyExplained, timerExplained;
        private bool spiritWasEnabled, possessionWasEnabled;
        private float resumeScale = 1f;
        private EnemyBase hostileEnemy;
        private readonly TutorialMovement[] directions = { TutorialMovement.Forward, TutorialMovement.Back,
            TutorialMovement.Left, TutorialMovement.Right };

        public TutorialProgress Progress { get; } = new();
        public GuardEnemy Guard => guard;
        public HeavyEnemy Heavy => heavy;
        public PossessionController Possession => possession;
        public float LevelSeconds => manager != null ? manager.LevelTimer : 300f;
        public SpiritController Spirit => spirit;
        public EnemyBase HostileEnemy => hostileEnemy != null ? hostileEnemy : guard;
        public string HostileName => HostileEnemy == heavy ? "WALL BREAKER" : "GUARD";
        public Transform TargetBody => target.transform;
        public Transform WallBody => wall.transform;
        public Transform ExitBody => exitTrigger.transform;
        public TutorialFocus CoachingFocus { get; private set; }
        public bool IsCoaching => CoachingFocus != TutorialFocus.None;

        private void OnEnable()
        {
            EventBus.Possessed += Possessed;
            EventBus.Ejected += Ejected;
            EventBus.TargetHit += TargetHit;
            EventBus.WallBroken += WallBroken;
            EventBus.LevelCompleted += Completed;
            EventBus.SpiritDied += Failed;
            EventBus.LevelFailed += Failed;
            EventBus.SpiritDamaged += Damaged;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= Possessed;
            EventBus.Ejected -= Ejected;
            EventBus.TargetHit -= TargetHit;
            EventBus.WallBroken -= WallBroken;
            EventBus.LevelCompleted -= Completed;
            EventBus.SpiritDied -= Failed;
            EventBus.LevelFailed -= Failed;
            EventBus.SpiritDamaged -= Damaged;
            StopCoaching(true);
            if (ownsPause) Time.timeScale = 1f;
        }

        private void Start()
        {
            lastPosition = spirit.transform.position;
            if (exitTrigger != null) exitTrigger.enabled = true;
            overlay = gameObject.AddComponent<TutorialOverlay>();
            overlay.Initialize(this);
            coach = gameObject.AddComponent<TutorialCoach>();
            coach.Initialize(this);
            ownsPause = true;
            Time.timeScale = 0f;
        }

        public void BeginPractice()
        {
            if (Progress.Started) return;
            Progress.Begin();
            lastPosition = spirit.transform.position;
            ownsPause = false;
            Time.timeScale = 1f;
            ShowCoaching(TutorialFocus.LevelClock);
        }

        private void Update()
        {
            if (!Progress.Started || Progress.Completed || Progress.Failed) return;
            if (spirit.CurrentHP <= 0 || manager != null && manager.CurrentState == LevelState.Failed)
            {
                Failed();
                return;
            }
            if (IsCoaching) { lastPosition = spirit.transform.position; return; }
            bool asSpirit = possession.CurrentState == PlayerState.Spirit;
            Vector3 delta = spirit.transform.position - lastPosition;
            lastPosition = spirit.transform.position;
            if (asSpirit && wasSpirit && Time.timeScale > 0f && Progress.Movement != TutorialMovement.All)
            {
                var next = Progress.NextMovement;
                if (next == TutorialMovement.Forward && Input.GetKey(KeyCode.W)) travel[0] += Mathf.Max(0f, delta.z);
                if (next == TutorialMovement.Back && Input.GetKey(KeyCode.S)) travel[1] += Mathf.Max(0f, -delta.z);
                if (next == TutorialMovement.Left && Input.GetKey(KeyCode.A)) travel[2] += Mathf.Max(0f, -delta.x);
                if (next == TutorialMovement.Right && Input.GetKey(KeyCode.D)) travel[3] += Mathf.Max(0f, delta.x);
                for (int i = 0; i < 4; i++) if (next == directions[i] && travel[i] >= 0.5f) Progress.Move(directions[i]);
                if (next == TutorialMovement.Jump && Input.GetKeyDown(KeyCode.Space)) jumpPressedUntil = Time.time + 0.5f;
                if (Time.time < jumpPressedUntil && spirit.GetComponent<Rigidbody>().linearVelocity.y > 1f)
                    Progress.Move(TutorialMovement.Jump);
            }
            wasSpirit = asSpirit;
            if (Progress.ForcedOut && !Progress.SurvivedHostility && !HostileEnemy.IsHostile)
                Progress.HostilityEnded();
            MaybeExplainTimer();
        }

        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (enemy != guard && enemy != heavy) return;
            Progress.Possessed(state);
            if (enemy == guard && !guardExplained) { guardExplained = true; ShowCoaching(TutorialFocus.GuardAbility); }
            else if (enemy == heavy && !heavyExplained) { heavyExplained = true; ShowCoaching(TutorialFocus.HeavyAbility); }
            else MaybeExplainTimer();
        }
        private void Ejected(EnemyBase enemy, bool forced)
        {
            if (enemy != guard && enemy != heavy) return;
            Progress.Ejected(enemy == guard, forced);
            if (forced)
            {
                hostileEnemy = enemy;
                coach?.Toast("AUTO-EJECTED — MOVE AWAY!", enemy.transform, new Color(1f, 0.3f, 0.25f));
            }
        }
        private void TargetHit(Target hit)
        {
            if (hit != target) return;
            Progress.TargetWasHit();
            coach?.Toast("DOOR UNLOCKED", door != null ? door.transform : null);
        }
        private void WallBroken(BreakableWall broken)
        {
            if (broken != wall) return;
            Progress.WallWasBroken();
            coach?.Toast("WALL SMASHED — FIND THE EXIT", ExitBody);
        }
        private void Completed() { Progress.Finish(); StopCoaching(false); }
        private void Failed() { Progress.Fail(); StopCoaching(false); }
        private void Damaged(int current, int maximum) => coach?.PulseHealth();

        private void MaybeExplainTimer()
        {
            if (!IsCoaching && !timerExplained && Progress.Lesson == TutorialLesson.WaitForTimer)
            { timerExplained = true; ShowCoaching(TutorialFocus.PossessionClock); }
        }

        private void ShowCoaching(TutorialFocus focus)
        {
            if (!IsCoaching)
            {
                resumeScale = Time.timeScale;
                spiritWasEnabled = spirit.enabled;
                possessionWasEnabled = possession.enabled;
                spirit.enabled = false;
                possession.enabled = false;
            }
            CoachingFocus = focus;
            Time.timeScale = 0f;
            if (possession.CurrentState != PlayerState.Spirit)
                EventBus.PossessTimerUpdated?.Invoke(possession.PossessTimer, possession.PossessDuration);
            coach?.Show(focus);
        }

        public void ContinueCoaching()
        {
            if (CoachingFocus == TutorialFocus.LevelClock) { CoachingFocus = TutorialFocus.Health; coach.Show(CoachingFocus); return; }
            StopCoaching(true);
            MaybeExplainTimer();
        }

        private void StopCoaching(bool resume)
        {
            if (!IsCoaching) return;
            CoachingFocus = TutorialFocus.None;
            coach?.Hide();
            if (spirit != null) spirit.enabled = spiritWasEnabled;
            if (possession != null) possession.enabled = possessionWasEnabled;
            if (resume) Time.timeScale = resumeScale;
            lastPosition = spirit != null ? spirit.transform.position : Vector3.zero;
        }

        public RectTransform[] FocusWidgets(TutorialFocus focus) => focus switch
        {
            TutorialFocus.LevelClock => new[] { levelClock },
            TutorialFocus.Health => new[] { healthText, healthBar },
            TutorialFocus.PossessionClock => new[] { possessionClock },
            _ => System.Array.Empty<RectTransform>()
        };

        public string MovementKey => Progress.NextMovement switch
        {
            TutorialMovement.Forward => "W", TutorialMovement.Left => "A", TutorialMovement.Back => "S",
            TutorialMovement.Right => "D", TutorialMovement.Jump => "SPACE", _ => "WASD"
        };
        public string MovementAction => Progress.NextMovement switch
        {
            TutorialMovement.Forward => "MOVE FORWARD", TutorialMovement.Left => "MOVE LEFT",
            TutorialMovement.Back => "MOVE BACK", TutorialMovement.Right => "MOVE RIGHT",
            TutorialMovement.Jump => "JUMP", _ => "MOVE"
        };

        public void Replay() => LevelFlow.Load(TutorialScene.Name);
        public void MainMenu() => LevelFlow.Load("MainMenu");
        public void PlayLevelOne() => LevelFlow.Load("Level_01");

        public string Title => Progress.Lesson switch
        {
            TutorialLesson.Movement => MovementAction,
            TutorialLesson.FindGuard => "MEET THE GUARD",
            TutorialLesson.ShootTarget => "SHOOT TO OPEN THE DOOR",
            TutorialLesson.ManualEject => "LEAVE BEFORE TIME RUNS OUT",
            TutorialLesson.RepossessGuard => "BORROW THE GUARD AGAIN",
            TutorialLesson.WaitForTimer => "WATCH THE POSSESSION TIMER",
            TutorialLesson.EscapeHostile => "FORCED OUT! MOVE AWAY",
            TutorialLesson.FindHeavy => "MEET THE WALL BREAKER",
            TutorialLesson.SmashWall => "WALL BREAKER · LMB TO SMASH",
            TutorialLesson.ReachExit => "FINISH AS THE SPIRIT",
            _ => "HOW TO PLAY"
        };

        public string Instruction
        {
            get
            {
                bool asGuard = possession.CurrentState == PlayerState.Possessed_Guard;
                bool asHeavy = possession.CurrentState == PlayerState.Possessed_Heavy;
                if (Progress.Lesson == TutorialLesson.ShootTarget && asGuard
                    && guard.GetComponent<ShootAbility>().UsesLeft == 0)
                    return "Out of shots? Press E to eject, then E near the Guard to refill. Aim at the target and left-click.";
                if (Progress.Lesson == TutorialLesson.SmashWall && asHeavy
                    && heavy.GetComponent<SmashAbility>().UsesLeft == 0)
                    return "Smash used? E to eject, then E near the Wall Breaker to try again.";
                return Progress.Lesson switch
                {
                    TutorialLesson.Movement => Progress.NextMovement == TutorialMovement.Jump ? "Press SPACE to jump."
                        : "Hold " + MovementKey + " to " + MovementAction.ToLowerInvariant() + ". The next key appears when you have done it.",
                    TutorialLesson.FindGuard => "The Guard can shoot targets. Walk into the next room; when its green E cue appears, press E to possess it.",
                    TutorialLesson.ShootTarget => asGuard
                        ? "Move the mouse to aim. LEFT CLICK shoots. Hit the marked target to unlock the gold door."
                        : "Return to the Guard and press E. The Spirit cannot shoot; use the Guard to hit the target.",
                    TutorialLesson.ManualEject => "Press E to leave this body and continue to the Wall Breaker.",
                    TutorialLesson.RepossessGuard => Progress.EjectedManually
                        ? "Press E near the Guard again. This time, stay inside until the timer empties to see what happens."
                        : "When the Guard is safe again, possess it with E, then press E once more to practise leaving early.",
                    TutorialLesson.WaitForTimer => "At zero you auto-eject. Run from the enemy! Press E to leave early instead.",
                    TutorialLesson.EscapeHostile => "AUTO-EJECTED! Use WASD to move away from the red " + HostileName.ToLowerInvariant() + ".",
                    TutorialLesson.FindHeavy => "Go through the open door. Approach the Wall Breaker and press E.",
                    TutorialLesson.SmashWall => asHeavy
                        ? "Move close. Face the brick wall with WASD. Press LMB to smash it."
                        : "Press E near the Wall Breaker, face the brick wall, then LMB to smash.",
                    TutorialLesson.ReachExit => "Press E to return to the Spirit, then enter the green exit. Finish before the level clock reaches zero!",
                    _ => ""
                };
            }
        }

        public bool Nearby(out Transform body, out string text, out bool danger)
        {
            body = null; text = ""; danger = false;
            if (!Progress.Started || Progress.Failed || Progress.Completed
                || possession.CurrentState != PlayerState.Spirit) return false;
            EnemyBase best = null;
            float closest = contextRange;
            foreach (EnemyBase enemy in new EnemyBase[] { guard, heavy })
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                float distance = Vector3.Distance(spirit.transform.position, enemy.transform.position);
                if (distance > closest) continue;
                if (Physics.Linecast(spirit.transform.position, enemy.transform.position, out RaycastHit hit,
                    ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<EnemyBase>() != enemy
                    && !hit.collider.transform.IsChildOf(spirit.transform)) continue;
                best = enemy; closest = distance;
            }
            if (best == null) return false;
            body = best.transform;
            danger = best.IsHostile;
            text = danger ? "HOSTILE — KEEP YOUR DISTANCE" : best == guard
                ? "GUARD · shoots targets\n[E] POSSESS" : "HEAVY · breaks walls\n[E] POSSESS";
            return true;
        }
    }
}
