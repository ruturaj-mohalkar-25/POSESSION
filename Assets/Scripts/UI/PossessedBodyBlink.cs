using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // C3: while the player drives a body and the possession bar has turned orange
    // (the timer is running low), the possessed body blinks, faster as the timeout nears.
    // The bar's own colours are NOT touched - this only reads the timer the bar already
    // uses. Runtime-installed per enemy, so no shared prefab, scene or AI file is edited.
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
                foreach (var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                    if (enemy.GetComponent<PossessedBodyBlink>() == null)
                        enemy.gameObject.AddComponent<PossessedBodyBlink>();
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public sealed class PossessedBodyBlink : MonoBehaviour
    {
        // HUDManager lerps the possess bar yellow->red below half time; it reads orange at
        // about a third left, so blink from there on.
        [SerializeField, Range(0f, 1f)] private float orangeThreshold = 0.35f;
        [SerializeField, Min(0.1f)] private float minBlinkRate = 2f;   // blinks/sec at the orange edge
        [SerializeField, Min(0.1f)] private float maxBlinkRate = 9f;   // blinks/sec just before timeout

        private EnemyBase enemy;
        private Renderer bodyRenderer;
        private MaterialPropertyBlock block;
        private Color baseColor = Color.white;
        private int colorId;

        private bool possessed;
        private float timerPct = 1f;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            colorId = Shader.PropertyToID("_Color");
            block = new MaterialPropertyBlock();

            // The visual body mesh, never the collider root.
            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.transform != transform)
            {
                bodyRenderer = meshRenderer;
                var mat = meshRenderer.sharedMaterial;
                if (mat != null && mat.HasProperty(colorId)) baseColor = mat.GetColor(colorId);
            }
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.PossessTimerUpdated += OnTimer;

            // Covers being added mid-possession.
            var possession = FindFirstObjectByType<PossessionController>();
            if (possession != null && possession.PossessedEnemy == enemy)
            {
                possessed = true;
                timerPct = possession.PossessDuration > 0f
                    ? Mathf.Clamp01(possession.PossessTimer / possession.PossessDuration) : 1f;
            }
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.PossessTimerUpdated -= OnTimer;
            possessed = false;
            RestoreColor();
        }

        private void OnPossessed(EnemyBase e, PlayerState state)
        {
            if (e != enemy) return;
            possessed = true;
            timerPct = 1f;
        }

        private void OnEjected(EnemyBase e, bool forced)
        {
            if (e != enemy) return;
            possessed = false;
            RestoreColor();
        }

        private void OnTimer(float current, float max)
        {
            if (!possessed) return;
            timerPct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void Update()
        {
            if (bodyRenderer == null) return;

            bool blinking = possessed && timerPct <= orangeThreshold && timerPct > 0f;
            if (!blinking)
            {
                if (possessed) SetBodyColor(baseColor);
                else RestoreColor();
                return;
            }

            // Lower time left -> faster blink. Square wave between base and a bright tint.
            float urgency = 1f - Mathf.Clamp01(timerPct / orangeThreshold);
            float rate = Mathf.Lerp(minBlinkRate, maxBlinkRate, urgency);
            bool on = Mathf.Repeat(Time.time * rate, 1f) < 0.5f;
            SetBodyColor(on ? Color.Lerp(baseColor, Color.white, 0.85f) : baseColor);
        }

        private void SetBodyColor(Color c)
        {
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor(colorId, c);
            bodyRenderer.SetPropertyBlock(block);
        }

        private void RestoreColor()
        {
            if (bodyRenderer == null) return;
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor(colorId, baseColor);
            bodyRenderer.SetPropertyBlock(block);
        }
    }
}
