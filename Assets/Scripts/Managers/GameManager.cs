using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public enum LevelState
    {
        InProgress,
        Complete,
        Failed
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private float levelTime = 300f;
        [SerializeField] private float[] starThresholds = { 60f, 90f, 180f };

        private LevelState currentState = LevelState.InProgress;
        private float levelTimer;
        private float elapsedTime;

        public LevelState CurrentState => currentState;
        public float LevelTimer => levelTimer;
        public float ElapsedTime => elapsedTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.LevelCompleted += OnLevelComplete;
            EventBus.LevelFailed += OnLevelFailed;
            EventBus.SpiritDied += OnSpiritDied;
        }

        private void OnDisable()
        {
            EventBus.LevelCompleted -= OnLevelComplete;
            EventBus.LevelFailed -= OnLevelFailed;
            EventBus.SpiritDied -= OnSpiritDied;
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            levelTimer = levelTime;
            currentState = LevelState.InProgress;
        }

        private void Update()
        {
            if (currentState != LevelState.InProgress) return;

            levelTimer -= Time.deltaTime;
            elapsedTime += Time.deltaTime;
            EventBus.LevelTimerUpdated?.Invoke(levelTimer, levelTime);

            if (levelTimer <= 0f)
            {
                levelTimer = 0f;
                OnLevelFailed();
            }
        }

        private void OnLevelComplete()
        {
            if (currentState != LevelState.InProgress) return;
            currentState = LevelState.Complete;
            Time.timeScale = 0f;

            int stars = 1;
            if (starThresholds.Length >= 1 && elapsedTime <= starThresholds[0]) stars = 3;
            else if (starThresholds.Length >= 2 && elapsedTime <= starThresholds[1]) stars = 2;
            EventBus.LevelCompletedWithStats?.Invoke(stars, elapsedTime);
        }

        private void OnLevelFailed()
        {
            if (currentState != LevelState.InProgress) return;
            currentState = LevelState.Failed;
            Time.timeScale = 0f;
        }

        private void OnSpiritDied()
        {
            OnLevelFailed();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
            {
                Time.timeScale = 1f;
                EventBus.Clear();
                SceneManager.LoadScene(next);
            }
            else
            {
                EventBus.GameComplete?.Invoke();
            }
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
