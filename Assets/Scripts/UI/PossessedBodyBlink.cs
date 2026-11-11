using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public static class PossessedBodyBlinkInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var player in root.GetComponentsInChildren<PossessionController>(true))
                    if (player.GetComponent<PossessedBodyBlink>() == null)
                        player.gameObject.AddComponent<PossessedBodyBlink>();
        }
    }

    // One cue per player also handles hosts spawned after the scene loads.
    // Visibility leaves the white possession material and combat property blocks alone.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PossessionController))]
    public sealed class PossessedBodyBlink : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)] private float orangeThreshold = 0.35f;
        [SerializeField, Min(0.1f)] private float minBlinkRate = 2f;
        [SerializeField, Min(0.1f)] private float maxBlinkRate = 9f;

        private PossessionController player;
        private EnemyBase host;
        private Renderer body;
        private bool originalForceRenderingOff;
        private float fraction = 1f;
        private float phase;
        private bool ended;

        private bool LevelActive => GameManager.Instance == null
            || GameManager.Instance.CurrentState == LevelState.InProgress;

        private void Awake() => player = GetComponent<PossessionController>();

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.PossessTimerUpdated += OnTimer;
            EventBus.LevelCompleted += OnLevelEnd;
            EventBus.LevelFailed += OnLevelEnd;
            EventBus.SpiritDied += OnLevelEnd;
            EventBus.GameComplete += OnLevelEnd;
            if (!ended && LevelActive && player.PossessedEnemy != null)
            {
                OnPossessed(player.PossessedEnemy, player.CurrentState);
                OnTimer(player.PossessTimer, player.PossessDuration);
            }
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.PossessTimerUpdated -= OnTimer;
            EventBus.LevelCompleted -= OnLevelEnd;
            EventBus.LevelFailed -= OnLevelEnd;
            EventBus.SpiritDied -= OnLevelEnd;
            EventBus.GameComplete -= OnLevelEnd;
            Clear();
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            if (!LevelActive || enemy == null || player.PossessedEnemy != enemy) return;
            Clear();
            ended = false;
            host = enemy;
            body = enemy.GetComponent<Renderer>();
            if (body == null) body = enemy.GetComponentInChildren<SkinnedMeshRenderer>();
            if (body == null) body = enemy.GetComponentInChildren<MeshRenderer>();
            if (body != null) originalForceRenderingOff = body.forceRenderingOff;
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            if (enemy == host) Clear();
        }

        private void OnTimer(float current, float max)
        {
            if (host == null) return;
            fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fraction <= 0f || fraction > orangeThreshold)
            {
                phase = 0f;
                RestoreVisibility();
            }
        }

        private void OnLevelEnd()
        {
            ended = true;
            Clear();
        }

        private void LateUpdate()
        {
            if (host == null) return;
            if (!LevelActive || !host.isActiveAndEnabled || host.CurrentHP <= 0
                || player.PossessedEnemy != host)
            {
                Clear();
                return;
            }
            if (body == null || Time.timeScale <= 0f) return;
            if (fraction <= 0f || fraction > orangeThreshold) return;

            float urgency = 1f - fraction / orangeThreshold;
            float rate = Mathf.Lerp(minBlinkRate, maxBlinkRate, urgency);
            // Integrate cycles so changing the rate never jumps the blink phase.
            phase = Mathf.Repeat(phase + Time.deltaTime * rate, 1f);
            body.forceRenderingOff = originalForceRenderingOff || phase >= 0.5f;
        }

        private void RestoreVisibility()
        {
            if (body != null) body.forceRenderingOff = originalForceRenderingOff;
        }

        private void Clear()
        {
            RestoreVisibility();
            host = null;
            body = null;
            fraction = 1f;
            phase = 0f;
        }
    }
}
