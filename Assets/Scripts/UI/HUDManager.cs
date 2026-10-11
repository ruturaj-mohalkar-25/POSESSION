using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill; // pivot X = 0 so it shrinks from the right
        [SerializeField] private Color healthHigh = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color healthMid = new Color(0.95f, 0.8f, 0.2f);
        [SerializeField] private Color healthLow = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private float damageFlashTime = 0.15f;

        [Header("Level Timer")]
        [SerializeField] private Text levelTimerText;

        [Header("Possession Timer")]
        [SerializeField] private GameObject possessTimerGroup; // bar + text, hidden as Spirit
        [SerializeField] private Text possessTimerText;
        [SerializeField] private Image possessTimerFill; // pivot X = 0
        [SerializeField] private Color possessBarColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private float possessWarningTime = 5f;

        [Header("State")]
        [SerializeField] private Text currentFormText;
        [SerializeField] private GameObject possessPrompt; // [E] POSSESS
        [SerializeField] private float promptHeight = 2.6f; // above the enemy's name tag
        [SerializeField] private Text messageText; // "Forced out!"
        [SerializeField] private float messageTime = 1.5f;

        [Header("Objective")]
        [SerializeField] private Text objectiveText;
        private const string objectiveStart = "1/3  Possess the GUARD and shoot the TARGET to unlock the door";
        private const string objectiveDoorOpen = "2/3  Possess the HEAVY and smash the BREAKABLE wall";
        private const string objectiveWallBroken = "3/3  Eject and reach the EXIT as the Spirit";

        [Header("End Screens")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        private float healthPct = 1f;
        private EnemyBase[] enemies;
        private Transform spirit;
        private Camera cam;
        private Coroutine flashRoutine;
        private Coroutine messageRoutine;

        private void OnEnable()
        {
            EventBus.SpiritDamaged += OnSpiritDamaged;
            EventBus.LevelTimerUpdated += UpdateLevelTimer;
            EventBus.PossessTimerUpdated += UpdatePossessTimer;
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.PossessPromptUpdated += OnPossessPrompt;
            EventBus.LevelCompleted += OnLevelComplete;
            EventBus.LevelFailed += OnLevelFailed;
            // GameManager pauses on death / time-out without raising LevelFailed
            EventBus.SpiritDied += OnLevelFailed;
            EventBus.DoorOpened += OnDoorOpened;
            EventBus.WallBroken += OnWallBroken;
        }

        private void OnDisable()
        {
            EventBus.SpiritDamaged -= OnSpiritDamaged;
            EventBus.LevelTimerUpdated -= UpdateLevelTimer;
            EventBus.PossessTimerUpdated -= UpdatePossessTimer;
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.PossessPromptUpdated -= OnPossessPrompt;
            EventBus.LevelCompleted -= OnLevelComplete;
            EventBus.LevelFailed -= OnLevelFailed;
            EventBus.SpiritDied -= OnLevelFailed;
            EventBus.DoorOpened -= OnDoorOpened;
            EventBus.WallBroken -= OnWallBroken;
        }

        private void Start()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            SetPossessTimerVisible(false);
            SetFormText("Spirit");
            SetObjective(objectiveStart);

            // Show full health immediately instead of waiting for the first hit
            var spiritController = FindFirstObjectByType<SpiritController>();
            if (spiritController != null)
            {
                spirit = spiritController.transform;
                UpdateHealth(spiritController.MaxHP, spiritController.MaxHP);
            }
            enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            if (possessPrompt != null && possessPrompt.activeSelf)
                PlacePromptOverEnemy();
        }

        // ---------------- Health ----------------

        private void OnSpiritDamaged(int current, int max)
        {
            UpdateHealth(current, max);

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashHealth());
        }

        private void UpdateHealth(int current, int max)
        {
            current = Mathf.Max(current, 0);
            healthPct = max > 0 ? (float)current / max : 0f;

            if (healthText != null)
                healthText.text = $"HP {current}/{max}";

            if (healthFill != null)
            {
                healthFill.rectTransform.localScale = new Vector3(healthPct, 1f, 1f);
                healthFill.color = HealthColor(healthPct);
            }
        }

        private Color HealthColor(float pct)
        {
            if (pct > 0.6f) return healthHigh;
            if (pct > 0.3f) return healthMid;
            return healthLow;
        }

        private IEnumerator FlashHealth()
        {
            if (healthFill == null) yield break;
            healthFill.color = Color.white;
            // Realtime so the flash still finishes if the game pauses on death
            yield return new WaitForSecondsRealtime(damageFlashTime);
            healthFill.color = HealthColor(healthPct);
        }

        // ---------------- Level timer ----------------

        private void UpdateLevelTimer(float current, float max)
        {
            if (levelTimerText == null) return;

            int seconds = Mathf.CeilToInt(Mathf.Max(current, 0f));
            levelTimerText.text = $"TIME {seconds / 60}:{seconds % 60:D2}";
            levelTimerText.color = current < 30f ? healthLow : Color.white;

            if (current <= 0f) OnLevelFailed();
        }

        // ---------------- Possession timer ----------------

        private void UpdatePossessTimer(float current, float max)
        {
            SetPossessTimerVisible(true);

            float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool warning = current <= possessWarningTime;

            if (possessTimerText != null)
            {
                possessTimerText.text = $"POSSESSED {Mathf.CeilToInt(Mathf.Max(current, 0f))}s";
                possessTimerText.color = warning ? healthLow : Color.white;
            }

            if (possessTimerFill != null)
            {
                possessTimerFill.rectTransform.localScale = new Vector3(pct, 1f, 1f);
                possessTimerFill.color = warning ? healthLow : possessBarColor;
            }
        }

        private void SetPossessTimerVisible(bool visible)
        {
            if (possessTimerGroup != null)
                possessTimerGroup.SetActive(visible);
            else if (possessTimerText != null)
                possessTimerText.gameObject.SetActive(visible);
        }

        // ---------------- Form / possession ----------------

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            string form = state switch
            {
                PlayerState.Possessed_Guard => "Guard",
                PlayerState.Possessed_Heavy => "Heavy",
                PlayerState.Possessed_Drone => "Drone",
                _ => "Spirit"
            };
            SetFormText(form);
            if (possessPrompt != null) possessPrompt.SetActive(false);
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            SetFormText("Spirit");
            SetPossessTimerVisible(false);

            if (forced)
                ShowMessage("FORCED OUT! Enemy is hostile");
        }

        private void SetFormText(string form)
        {
            if (currentFormText != null)
                currentFormText.text = $"Form: {form}";
        }

        private void OnPossessPrompt(bool show)
        {
            if (possessPrompt != null) possessPrompt.SetActive(show);
        }

        // ---------------- Objective ----------------

        private void OnDoorOpened(Door door)
        {
            SetObjective(objectiveDoorOpen);
            ShowMessage("DOOR UNLOCKED!");
        }

        private void OnWallBroken(BreakableWall wall)
        {
            SetObjective(objectiveWallBroken);
            ShowMessage("WALL SMASHED!");
        }

        private void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        // ---------------- Possess prompt ----------------

        // Same pick as PossessionController: nearest possessable enemy the spirit can see
        private void PlacePromptOverEnemy()
        {
            if (spirit == null || enemies == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            EnemyBase best = null;
            float bestDist = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBePossessed) continue;
                float dist = Vector3.Distance(spirit.position, enemy.transform.position);
                if (dist >= bestDist || !CanSee(enemy)) continue;
                bestDist = dist;
                best = enemy;
            }
            if (best == null) return;

            Vector3 above = best.transform.position + Vector3.up * promptHeight * best.transform.lossyScale.y;
            Vector3 screen = cam.WorldToScreenPoint(above);
            if (screen.z > 0f) possessPrompt.transform.position = screen; // overlay canvas uses screen pixels
        }

        private bool CanSee(EnemyBase enemy)
        {
            if (!Physics.Linecast(spirit.position, enemy.transform.position, out RaycastHit hit,
                    ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<EnemyBase>() == enemy || hit.collider.transform.IsChildOf(spirit);
        }

        // ---------------- Messages ----------------

        private void ShowMessage(string text)
        {
            if (messageText == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(text));
        }

        private IEnumerator MessageRoutine(string text)
        {
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(messageTime);
            messageText.gameObject.SetActive(false);
        }

        // ---------------- End screens ----------------

        private void OnLevelComplete()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
        }

        private void OnLevelFailed()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        // Button callbacks
        public void OnRestartButton()
        {
            GameManager.Instance?.RestartLevel();
        }

        public void OnQuitButton()
        {
            GameManager.Instance?.QuitToMenu();
        }
    }
}
