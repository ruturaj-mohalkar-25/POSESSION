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
        private bool guardPopupShown, heavyPopupShown, possessingHintShown;
        private float guardRoomZ = 12f;
        private float heavyRoomZ = 24f;
        private bool reachedHeavyRoom;

        // The Heavy's room starts just past the gold door; fall back to the old fixed line without one
        // (read once at start, since an opening door can slide away)
        private float heavyRoomStartZ = float.NaN;
        private float HeavyRoomStartZ
        {
            get
            {
                if (float.IsNaN(heavyRoomStartZ))
                    heavyRoomStartZ = door != null ? door.transform.position.z + 0.5f : heavyRoomZ;
                return heavyRoomStartZ;
            }
        }

        // Hints are side cards that never pause the game: each one clears when the player does what
        // it describes, or after its time runs out, whichever comes first.
        public enum PopupState { None, SpiritIntro, Possessing, Guard, Heavy }
        public PopupState ActivePopup { get; private set; } = PopupState.None;
        public float HintShownAt { get; private set; }

        // True while the Spirit is being hurt (or an enemy is hunting it): the bubble swaps to a warning
        private float lastDamagedAt = float.NegativeInfinity;
        public bool UnderAttack => possession != null && possession.CurrentState == PlayerState.Spirit
            && !Progress.Completed && !Progress.Failed
            && (Time.time - lastDamagedAt < 1.5f
                || guard != null && guard.isActiveAndEnabled && guard.IsHostile
                || heavy != null && heavy.isActiveAndEnabled && heavy.IsHostile);
        private void SpiritDamaged(int current, int max) => lastDamagedAt = Time.time;
        public float HintDuration { get; private set; }
        public float HintRemaining01 => ActivePopup == PopupState.None || HintDuration <= 0f ? 0f
            : Mathf.Clamp01(1f - (Time.unscaledTime - HintShownAt) / HintDuration);

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
        public bool IsCoaching => false; // hints never block play
        public int Charges => spirit != null && spirit.TryGetComponent(out PossessionCharge charge) ? charge.Count : 0;
        public string FailureMessage { get; private set; } = "CAUGHT - HEALTH AT ZERO";

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
            EventBus.SpiritDamaged += SpiritDamaged;
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
            EventBus.SpiritDamaged -= SpiritDamaged;
            ChargeHUDInstaller.ShowKeyHints = true; // campaign levels keep their key reminders
        }

        private void Start()
        {
            if (exitTrigger != null) exitTrigger.enabled = true;
            if (!Progress.Started) Progress.Begin(); // play starts straight away; hints run alongside
            MarkLayoutWallsFadeable();
            HideEnemyNameTags();
            BuildCheckeredExit();
            _ = HeavyRoomStartZ; // remember where the door sits before it opens
            ChargeHUDInstaller.ShowKeyHints = false; // the objective bar already names the key
            gameObject.AddComponent<TutorialOverlay>().Initialize(this);
            gameObject.AddComponent<TutorialDoorFader>().Initialize(possession); // doors fade like walls
            ChargeHUDInstaller.Install(gameObject.scene);
            UpdateObjective(); // no opening hint: the YOU arrow and objective bar cover it
        }

        public void ShowPopup(PopupState popup)
        {
            ActivePopup = popup;
            HintShownAt = Time.unscaledTime;
            HintDuration = popup switch
            {
                PopupState.SpiritIntro => 9f,
                PopupState.Possessing => 12f,
                PopupState.Guard => 14f,
                PopupState.Heavy => 14f,
                _ => 0f
            };
        }

        public void DismissPopup() => ShowPopup(PopupState.None);

        // The saved checkered mat and flag (P5_Exit) were left at an older layout's exit, away from the
        // real trigger. Rebuild them on the trigger so the exit looks like the campaign's finish line.
        private void BuildCheckeredExit()
        {
            if (exitTrigger == null) return;
            var old = GameObject.Find("P5_Exit");
            if (old != null) Destroy(old);
            foreach (var r in exitTrigger.GetComponentsInChildren<Renderer>()) r.enabled = false; // no plain green pad

            Bounds zone = exitTrigger.bounds;
            Vector3 centre = zone.center;
            float floorY = Physics.Raycast(centre + Vector3.up * 2f, Vector3.down, out var hit, 10f, ~0,
                QueryTriggerInteraction.Ignore) ? hit.point.y : zone.min.y;
            // The flag goes on the far side of the pad, against the dead-end wall
            bool wallAhead = Physics.Raycast(centre, Vector3.forward, out var ahead, 20f, ~0, QueryTriggerInteraction.Ignore);
            bool wallBehind = Physics.Raycast(centre, Vector3.back, out var behind, 20f, ~0, QueryTriggerInteraction.Ignore);
            float back = wallAhead && (!wallBehind || ahead.distance <= behind.distance) ? 1f : -1f;

            // Same layout as the campaign levels (VisualSetup): mat in front of the exit, flag behind it
            var root = new GameObject("P5_Exit").transform;
            float x = centre.x, z = centre.z;
            var checker = CheckerTexture();
            var mat = RuntimeMaterials.Lit(Color.white);
            mat.mainTexture = checker;
            mat.mainTextureScale = new Vector2(4f, 3f);
            ExitPart(root, PrimitiveType.Cube, "FinishMat", new Vector3(x, floorY + 0.11f, z - 0.8f * back),
                new Vector3(3.1f, 0.02f, 2.5f), mat);

            var flagMat = RuntimeMaterials.Lit(Color.white);
            flagMat.mainTexture = checker;
            flagMat.mainTextureScale = new Vector2(2f, 1.5f);
            float flagZ = z + 0.1f * back;
            ExitPart(root, PrimitiveType.Cylinder, "FlagPole", new Vector3(x - 0.6f, floorY + 1.6f, flagZ),
                new Vector3(0.08f, 1.6f, 0.08f), RuntimeMaterials.Lit(new Color(0.55f, 0.58f, 0.62f)));
            ExitPart(root, PrimitiveType.Sphere, "PoleTop", new Vector3(x - 0.6f, floorY + 3.25f, flagZ),
                Vector3.one * 0.18f, RuntimeMaterials.Lit(new Color(1f, 0.78f, 0.18f)));
            ExitPart(root, PrimitiveType.Cube, "Flag", new Vector3(x - 0.05f, floorY + 2.75f, flagZ),
                new Vector3(1.0f, 0.7f, 0.04f), flagMat);

            // The "EXIT" sign was saved at the old exit; stand it over the mat like in the campaign
            var label = GameObject.Find("Label_ExitZone");
            if (label != null)
                label.transform.position = new Vector3(x, floorY + 3.0f, z - 0.8f * back);
        }

        private static void ExitPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            col.enabled = false; // purely visual: the trigger decides when the level ends
            Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Texture2D CheckerTexture()
        {
            const int size = 64, cells = 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = (x * cells / size + y * cells / size) % 2 == 0
                        ? new Color32(245, 245, 245, 255) : new Color32(20, 20, 24, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        // The tutorial's walls are named by room side (Bay1_South, Exit_West...), not "Wall", so the
        // camera's wall fader skipped them and the Spirit vanished behind the near wall. Tag every
        // standing piece of the layout (anything that isn't a floor) so it fades when it blocks the view.
        private static readonly string[] LayoutRoots = { "CompactTutorialLayout", "P4_LevelLayout" };

        // The arrow and Field Guide already name the bodies, so drop the GUARD / HEAVY tags above them
        private static void HideEnemyNameTags()
        {
            foreach (var label in FindObjectsByType<WorldLabel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (label.Follow != null && label.Follow.GetComponentInParent<EnemyBase>() != null)
                    Destroy(label.gameObject);
        }

        private void MarkLayoutWallsFadeable()
        {
            foreach (var go in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (System.Array.IndexOf(LayoutRoots, go.name) < 0) continue;
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                {
                    if (col.isTrigger || col.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || col.GetComponent<Renderer>() == null || col.bounds.size.y < 1f
                        || col.GetComponent<VisibilityOccluder>() != null) continue;
                    col.gameObject.AddComponent<VisibilityOccluder>();
                }
            }
        }

        public void BeginPractice()
        {
            if (!Progress.Started) Progress.Begin();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (ActivePopup != PopupState.None && Time.unscaledTime - HintShownAt >= HintDuration) DismissPopup();
            TrackHeavyRoom();
            UpdateObjective();
            CheckRoomEntry();
            if (!Progress.Started || Progress.Completed || Progress.Failed) return;
            if (spirit.CurrentHP <= 0 || manager != null && manager.CurrentState == LevelState.Failed)
                QueueFailure(spirit.CurrentHP <= 0 ? "CAUGHT - HEALTH AT ZERO" : "TIME UP -- MOVE WITH PURPOSE");
        }

        // Whichever body the player controls, note when it first reaches the Heavy's room
        private void TrackHeavyRoom()
        {
            if (reachedHeavyRoom || possession == null) return;
            Transform body = possession.PossessedEnemy != null ? possession.PossessedEnemy.transform
                : spirit != null ? spirit.transform : null;
            if (body != null && body.position.z >= HeavyRoomStartZ) reachedHeavyRoom = true;
        }

        private void CheckRoomEntry()
        {
            if (spirit == null || Progress.Completed || Progress.Failed
                || possession.CurrentState != PlayerState.Spirit) return;

            float z = spirit.transform.position.z;
            if (!guardPopupShown && z >= guardRoomZ)
            {
                guardPopupShown = true;
                ShowPopup(PopupState.Guard);
            }
            else if (!heavyPopupShown && z >= HeavyRoomStartZ)
            {
                heavyPopupShown = true;
                ShowPopup(PopupState.Heavy);
            }
        }

        private GameObject objectiveBar;
        private bool objectiveBarSearched;

        // The bubble above the player carries the current step, so the shared bottom objective bar is
        // kept in sync (other systems read it) but hidden in the tutorial to avoid saying it twice.
        private void UpdateObjective()
        {
            if (hud != null && !Progress.Completed && !Progress.Failed)
                hud.SetLevelObjective(Title);
            if (objectiveBar == null && !objectiveBarSearched)
            {
                objectiveBarSearched = true;
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "ObjectiveBar") { objectiveBar = t.gameObject; break; }
            }
            if (objectiveBar != null && objectiveBar.activeSelf) objectiveBar.SetActive(false);
        }

        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (enemy != guard && enemy != heavy) return;
            Progress.Possessed(state);
            if (enemy == guard && !possessingHintShown)
            {
                // First time in a body: explain the shrinking ring and how to shoot
                possessingHintShown = true;
                ShowPopup(PopupState.Possessing);
            }
            else if (ActivePopup == PopupState.SpiritIntro || ActivePopup == PopupState.Guard)
                DismissPopup();
        }

        private void Ejected(EnemyBase enemy, bool forced)
        {
            if (enemy != guard && enemy != heavy) return;
            hostileEnemy = forced ? enemy : hostileEnemy;
            Progress.Ejected(enemy == guard, forced);
        }

        private void TargetHit(Target hit)
        {
            if (hit != target) return;
            Progress.TargetWasHit();
            if (ActivePopup == PopupState.Possessing || ActivePopup == PopupState.Guard) DismissPopup();
        }

        private void WallBroken(BreakableWall broken)
        {
            if (broken != wall) return;
            Progress.WallWasBroken();
            if (ActivePopup == PopupState.Heavy) DismissPopup();
        }

        private void Completed() => Progress.Finish();
        private void SpiritDied() => QueueFailure("CAUGHT - HEALTH AT ZERO");
        private void LevelFailed() => QueueFailure(spirit != null && spirit.CurrentHP <= 0
            ? "CAUGHT - HEALTH AT ZERO" : "TIME UP -- MOVE WITH PURPOSE");

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
                    if (possession.CurrentState == PlayerState.Possessed_Guard) return "LEFT CLICK - HIT THE TARGET";
                    if (possession.CurrentState != PlayerState.Spirit) return "E - RETURN TO SPIRIT";
                    return PossessionInstruction(guard, "GUARD");
                }
                // Door is open: first get to the Heavy's room, then deal with the Heavy
                if (!reachedHeavyRoom) return "DOOR OPEN - GO THROUGH IT";
                if (possession.CurrentState == PlayerState.Possessed_Guard) return "E - LEAVE THE GUARD";
                if (!Progress.UsedHeavy)
                {
                    return PossessionInstruction(heavy, "HEAVY");
                }
                if (!Progress.BrokeWall)
                {
                    if (possession.CurrentState == PlayerState.Possessed_Heavy)
                        return "LEFT CLICK - SMASH THE WALL";
                    return PossessionInstruction(heavy, "HEAVY");
                }
                return possession.CurrentState == PlayerState.Spirit ? "REACH THE EXIT" : "E - RETURN TO SPIRIT";
            }
        }

        public string Instruction => Title;

        private string PossessionInstruction(EnemyBase body, string name)
        {
            var reason = possession.GetPossessionBlockReason(body);
            if (reason == PossessionBlockReason.None && possession.FindPossessableEnemy() != body)
                return "MOVE CLOSER TO THE " + name;
            return reason switch
            {
                PossessionBlockReason.None => "PRESS E - POSSESS " + name,
                PossessionBlockReason.NoCharges => "COLLECT WHITE SOUL - +1 CHARGE",
                PossessionBlockReason.Recovering => "RECOVERING - KEEP MOVING",
                PossessionBlockReason.Hostile => name + " HOSTILE - MOVE AWAY",
                PossessionBlockReason.OutOfRange => "MOVE CLOSER TO THE " + name,
                PossessionBlockReason.Obstructed => "GO AROUND WALL TO " + name,
                PossessionBlockReason.InVisionCone => "AVOID RED CONE - GET BEHIND",
                _ => "FIND ANOTHER BODY"
            };
        }

        public string ControlsLegend
        {
            get
            {
                if (possession.CurrentState == PlayerState.Possessed_Guard)
                    return "WASD  Move\nLeft click  Shoot\nE  Eject";
                if (possession.CurrentState == PlayerState.Possessed_Heavy)
                    return "WASD  Move\nLeft click  Smash\nE  Eject";
                return "WASD  Move\nSpace  Jump\nE  Possess";
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
            text = possession.GetPossessionBlockReason(best) == PossessionBlockReason.None
                && best == possession.FindPossessableEnemy() ? "E" : "";
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
