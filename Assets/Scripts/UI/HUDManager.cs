using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Text healthText;

        [Header("Timers")]
        [SerializeField] private Text levelTimerText;
        [SerializeField] private Text possessTimerText;

        [Header("State")]
        [SerializeField] private Text currentFormText;
        [SerializeField] private GameObject possessPrompt; // [E] POSSESS

        [Header("End Screens")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;

        private void OnEnable()
        {
            EventBus.SpiritDamaged += UpdateHealth;
            EventBus.LevelTimerUpdated += UpdateLevelTimer;
            EventBus.PossessTimerUpdated += UpdatePossessTimer;
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.LevelCompleted += OnLevelComplete;
            EventBus.LevelFailed += OnLevelFailed;
        }

        private void OnDisable()
        {
            EventBus.SpiritDamaged -= UpdateHealth;
            EventBus.LevelTimerUpdated -= UpdateLevelTimer;
            EventBus.PossessTimerUpdated -= UpdatePossessTimer;
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.LevelCompleted -= OnLevelComplete;
            EventBus.LevelFailed -= OnLevelFailed;
        }

        private void Start()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (possessTimerText != null) possessTimerText.gameObject.SetActive(false);
            SetFormText("Spirit");
        }

        private void UpdateHealth(int current, int max)
        {
            if (healthText != null)
                healthText.text = $"{current}/{max}";
        }

        private void UpdateLevelTimer(float current, float max)
        {
            if (levelTimerText != null)
            {
                int seconds = Mathf.CeilToInt(current);
                int m = seconds / 60;
                int s = seconds % 60;
                levelTimerText.text = $"{m}:{s:D2}";

                // Red when under 30s
                levelTimerText.color = current < 30f ? Color.red : Color.white;
            }
        }

        private void UpdatePossessTimer(float current, float max)
        {
            if (possessTimerText != null)
            {
                possessTimerText.gameObject.SetActive(true);
                possessTimerText.text = $"{Mathf.CeilToInt(current)}s";
            }
        }

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
            if (possessTimerText != null) possessTimerText.gameObject.SetActive(false);
        }

        private void SetFormText(string form)
        {
            if (currentFormText != null)
                currentFormText.text = form;
        }

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

        public void ShowPossessPrompt(bool show)
        {
            if (possessPrompt != null) possessPrompt.SetActive(show);
        }
    }
}
