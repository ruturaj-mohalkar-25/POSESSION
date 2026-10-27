using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill; // pivot X = 0 so it shrinks from the right
        [SerializeField] private Color healthHigh = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color healthMid = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color healthLow = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private float damageFlashTime = 0.15f;

        [Header("Level Timer")]
        [SerializeField] private Text levelTimerText;

        [Header("Possession Timer")]
        [SerializeField] private GameObject possessTimerGroup; // bar + text, hidden as Spirit
        [SerializeField] private Text possessTimerText;
        [SerializeField] private Image possessTimerFill; // pivot X = 0
        [SerializeField] private Color possessBarColor = Color.white;
        [SerializeField] private float possessWarningTime = 5f;

        [Header("State")]
        [SerializeField] private Text currentFormText;
        [SerializeField] private GameObject possessPrompt; // [E] POSSESS
        [SerializeField] private float promptHeight = 2.6f; // above the enemy's name tag
        [SerializeField] private Text messageText; // "Forced out!"
        [SerializeField] private float messageTime = 1.5f;
        [SerializeField] private float unlockMessageTime = 2.5f;
        [SerializeField] private Color alertColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color objectiveColor = new Color(1f, 0.78f, 0.18f);

        [Header("Objective")]
        [SerializeField] private Text objectiveText;
        // One step per puzzle event (door opened, wall broken), set per level by the HUD setup tool
        [SerializeField] private string[] objectives =
        {
            "1/3  POSSESS GUARD  •  HIT TARGET",
            "2/3  POSSESS HEAVY  •  BREAK WALL",
            "3/3  EJECT  •  REACH EXIT"
        };

        [Header("Controls / Ammo")]
        [SerializeField] private Text controlsText; // bottom-right, changes with the current form
        [SerializeField] private AmmoDisplay ammoDisplay;

        [Header("End Screens")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject gameCompletePanel; // after Next Level on the last level
        [SerializeField] private Text nextButtonLabel;
        [SerializeField] private string firstLevelScene = "Level_01";

        private float healthPct = 1f;
        private EnemyBase[] enemies;
        private Transform spirit;
        private Camera cam;
        private Coroutine flashRoutine;
        private Coroutine messageRoutine;
        private bool levelEnded;
        private Coroutine objectiveRoutine;
        private Color? objectiveBarColor;
        private int objectiveStep;
        private ResultsView activeResults;
        private int stars;
        private AnalyticsData? analytics;

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
            EventBus.SpiritDied += OnSpiritDied;
            EventBus.DoorOpened += OnDoorOpened;
            EventBus.WallBroken += OnWallBroken;
            EventBus.AmmoUpdated += OnAmmoUpdated;
            EventBus.LevelCompletedWithStats += OnLevelStats;
            EventBus.AnalyticsReady += OnAnalyticsReady;
            EventBus.GameComplete += OnGameComplete;
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
            EventBus.SpiritDied -= OnSpiritDied;
            EventBus.DoorOpened -= OnDoorOpened;
            EventBus.WallBroken -= OnWallBroken;
            EventBus.AmmoUpdated -= OnAmmoUpdated;
            EventBus.LevelCompletedWithStats -= OnLevelStats;
            EventBus.AnalyticsReady -= OnAnalyticsReady;
            EventBus.GameComplete -= OnGameComplete;
        }

        private void Start()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (gameCompletePanel != null) gameCompletePanel.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            if (ammoDisplay != null) ammoDisplay.Hide();
            ApplyCompactLayout();
            HideLegacyOverlays();
            SetPossessTimerVisible(false);
            SetObjectiveStep(0);

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
            SetFormText(form == "Heavy" ? "Heavy (slow)" : form);
            SetControls(form);
            if (possessPrompt != null) possessPrompt.SetActive(false);

            // The ability reports its uses before Possessed is raised, so read them directly
            var ability = enemy != null ? enemy.GetComponent<Ability>() : null;
            if (ability != null && ability.MaxUses > 0) OnAmmoUpdated(ability.UsesLeft, ability.MaxUses);
            else if (ammoDisplay != null) ammoDisplay.Hide();
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            SetFormText("Spirit");
            SetControls("Spirit");
            SetPossessTimerVisible(false);
            if (ammoDisplay != null) ammoDisplay.Hide();

            if (forced)
                ShowMessage("FORCED OUT", messageTime, alertColor);
        }

        private void SetFormText(string form)
        {
            // Form is communicated by the controlled character itself. The old corner label
            // duplicated that information and competed with the objective bar.
            if (currentFormText != null) currentFormText.gameObject.SetActive(false);
        }

        private void SetControls(string form)
        {
            // Contextual prompts now live beside the possessed body; keep the screen HUD lean.
            if (controlsText != null)
            {
                Transform panel = controlsText.transform.parent;
                (panel != null ? panel.gameObject : controlsText.gameObject).SetActive(false);
            }
        }

        private void HideLegacyOverlays()
        {
            SetFormText(string.Empty);
            SetControls(string.Empty);
        }

        private void ApplyCompactLayout()
        {
            PlaceTopLeft(healthText != null ? healthText.rectTransform : null,
                new Vector2(24f, -18f), new Vector2(280f, 36f));
            if (healthText != null) healthText.fontSize = 26;

            RectTransform healthBar = healthFill != null ? healthFill.rectTransform.parent as RectTransform : null;
            PlaceTopLeft(healthBar, new Vector2(24f, -58f), new Vector2(280f, 18f));

            PlaceTopLeft(levelTimerText != null ? levelTimerText.rectTransform : null,
                new Vector2(24f, -84f), new Vector2(280f, 36f));
            if (levelTimerText != null) levelTimerText.fontSize = 28;

            if (objectiveText != null)
            {
                objectiveText.fontSize = 26;
                objectiveText.color = new Color(0.12f, 0.10f, 0.04f);
                var bar = objectiveText.transform.parent as RectTransform;
                if (bar != null)
                {
                    bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0f);
                    bar.anchoredPosition = new Vector2(0f, 24f);
                    bar.sizeDelta = new Vector2(820f, 52f);
                    var image = bar.GetComponent<Image>();
                    if (image != null) image.color = new Color(1f, 0.78f, 0.18f, 0.92f);
                }
            }

            if (messageText != null)
            {
                RectTransform message = messageText.rectTransform;
                message.anchorMin = message.anchorMax = message.pivot = new Vector2(0.5f, 0.5f);
                message.anchoredPosition = new Vector2(0f, 90f);
                message.sizeDelta = new Vector2(900f, 70f);
                messageText.fontSize = 44;
            }

            if (possessPrompt != null)
            {
                var promptText = possessPrompt.GetComponent<Text>();
                if (promptText != null) promptText.color = successColor;
            }
        }

        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void OnAmmoUpdated(int left, int max)
        {
            if (ammoDisplay != null && !levelEnded) ammoDisplay.Show(left, max);
        }

        private void OnPossessPrompt(bool show)
        {
            if (possessPrompt != null) possessPrompt.SetActive(show);
        }

        // ---------------- Objective ----------------

        private void OnDoorOpened(Door door)
        {
            SetObjectiveStep(objectiveStep + 1);
            ShowMessage("DOOR OPEN", unlockMessageTime, successColor);
        }

        private void OnWallBroken(BreakableWall wall)
        {
            SetObjectiveStep(objectiveStep + 1);
            ShowMessage("WALL BROKEN", messageTime, successColor);
        }

        // Every objective change flashes the bar so it's noticed even while watching the character
        private void SetObjectiveStep(int step)
        {
            if (objectiveText == null || objectives == null || objectives.Length == 0) return;
            objectiveStep = Mathf.Min(step, objectives.Length - 1);
            objectiveText.text = CompactObjective(objectives[objectiveStep]);
            if (objectiveRoutine != null) StopCoroutine(objectiveRoutine);
            objectiveRoutine = StartCoroutine(FlashObjective());
        }

        private static string CompactObjective(string objective)
        {
            if (string.IsNullOrWhiteSpace(objective)) return objective;
            string upper = objective.ToUpperInvariant();
            int split = objective.IndexOf("  ", System.StringComparison.Ordinal);
            string prefix = split >= 0 ? objective.Substring(0, split + 2) : string.Empty;

            if (upper.Contains("BOTH TARGET") || upper.Contains("2 TARGET"))
                return prefix + "POSSESS GUARD  •  HIT 2 TARGETS";
            if (upper.Contains("TARGET"))
                return prefix + "POSSESS GUARD  •  HIT TARGET";
            if (upper.Contains("FIRST") || upper.Contains("WALL 1"))
                return prefix + "POSSESS HEAVY  •  BREAK WALL 1";
            if (upper.Contains("SECOND") || upper.Contains("WALL 2"))
                return prefix + "POSSESS HEAVY  •  BREAK WALL 2";
            if (upper.Contains("WALL"))
                return prefix + "POSSESS HEAVY  •  BREAK WALL";
            if (upper.Contains("EXIT"))
                return prefix + "EJECT  •  REACH EXIT";
            return objective;
        }

        private IEnumerator FlashObjective()
        {
            var bar = objectiveText.transform.parent;
            var barImage = bar != null ? bar.GetComponent<Image>() : null;
            if (barImage != null && !objectiveBarColor.HasValue) objectiveBarColor = barImage.color;
            Color normal = objectiveBarColor ?? Color.clear;
            Color bright = new Color(objectiveColor.r, objectiveColor.g, objectiveColor.b, 0.95f);

            const float duration = 1.2f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / duration * Mathf.PI); // 0 -> 1 -> 0
                if (barImage != null) barImage.color = Color.Lerp(normal, bright, k);
                if (bar != null) bar.localScale = Vector3.one * (1f + 0.08f * k);
                yield return null;
            }
            if (barImage != null) barImage.color = normal;
            if (bar != null) bar.localScale = Vector3.one;
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

        private void ShowMessage(string text, float duration, Color color)
        {
            if (messageText == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(text, duration, color));
        }

        private IEnumerator MessageRoutine(string text, float duration, Color color)
        {
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            RectTransform rect = messageText.rectTransform;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - Mathf.Clamp01((t - duration * 0.6f) / (duration * 0.4f));
                float pulse = 1f + 0.08f * Mathf.Sin(t * 12f);
                messageText.color = new Color(color.r, color.g, color.b, color.a * fade);
                rect.localScale = Vector3.one * pulse;
                yield return null;
            }
            rect.localScale = Vector3.one;
            messageText.gameObject.SetActive(false);
        }

        // ---------------- End screens ----------------

        private void OnLevelComplete()
        {
            if (levelEnded) return;
            levelEnded = true;

            ShowEndPanel(levelCompletePanel);
            // GameManager.LoadNextLevel raises GameComplete instead of loading when this is the last level
            if (nextButtonLabel != null)
            {
                bool last = SceneManager.GetActiveScene().buildIndex >= SceneManager.sceneCountInBuildSettings - 1;
                nextButtonLabel.text = last ? "FINISH" : "NEXT LEVEL";
            }
        }

        // GameManager's rating and AnalyticsTracker's summary can arrive before or after LevelCompleted
        private void OnLevelStats(int rating, float elapsed)
        {
            stars = rating;
            OnLevelComplete();
            ShowResults();
        }

        private void OnAnalyticsReady(AnalyticsData data)
        {
            analytics = data;
            ShowResults();
        }

        private void OnGameComplete()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            ShowEndPanel(gameCompletePanel);
        }

        private void ShowResults()
        {
            if (activeResults == null) return;
            if (stars > 0) activeResults.ShowRating(stars);
            if (analytics.HasValue)
            {
                activeResults.ShowStats(Summary(analytics.Value));
                if (analytics.Value.possessions != null)
                    activeResults.ShowChart(analytics.Value.possessions);
            }
        }

        private static string Summary(AnalyticsData data)
        {
            int seconds = Mathf.FloorToInt(data.completionTime);
            return $"TIME {seconds / 60}:{seconds % 60:D2}    POSSESSIONS {data.possessionCount}    " +
                   $"FORCED EJECTS {data.forcedEjectCount}    DEATHS {data.deathCount}";
        }

        private void OnSpiritDied()
        {
            OnLevelFailed();
        }

        // Several sources can report failure (time-out, death, LevelFailed); only the first one counts
        private void OnLevelFailed()
        {
            if (levelEnded) return;
            levelEnded = true;
            ShowEndPanel(gameOverPanel);
        }

        private void ShowEndPanel(GameObject panel)
        {
            if (possessPrompt != null) possessPrompt.SetActive(false);
            if (ammoDisplay != null) ammoDisplay.Hide();
            SetPossessTimerVisible(false);
            if (panel == null) return;

            panel.SetActive(true);
            activeResults = panel.GetComponent<ResultsView>();
            ShowResults();
        }

        // Button callbacks
        public void OnRestartButton()
        {
            GameManager.Instance?.RestartLevel();
        }

        public void OnNextLevelButton()
        {
            GameManager.Instance?.LoadNextLevel();
        }

        public void OnPlayAgainButton()
        {
            LevelFlow.Load(firstLevelScene);
        }

        public void OnQuitButton()
        {
            GameManager.Instance?.QuitToMenu();
        }
    }
}
