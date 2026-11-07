using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // C3: keep the white possession identity; pulse an amber warning around the feet
    // only while the existing possession timer is in its orange band.
    public static class PossessedBodyBlinkInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Subscribe();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // GameManager clears EventBus when changing or restarting a level.
            Subscribe();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                    Attach(enemy);
        }

        private static void Subscribe()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Possessed += OnPossessed;
        }

        private static PossessedBodyBlink Attach(EnemyBase enemy)
        {
            var warning = enemy.GetComponent<PossessedBodyBlink>();
            return warning != null ? warning : enemy.gameObject.AddComponent<PossessedBodyBlink>();
        }

        private static void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            // Also covers bodies spawned after sceneLoaded.
            if (enemy == null) return;
            var warning = Attach(enemy);
            if (warning.isActiveAndEnabled) warning.HandlePossessed(enemy, state);
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public sealed class PossessedBodyBlink : MonoBehaviour
    {
        private const int Segments = 48;
        [SerializeField, Range(0f, 1f)] private float orangeThreshold = 0.35f;
        private static readonly Color WarningColor = new Color(1f, 0.5f, 0.08f, 1f);

        private EnemyBase enemy;
        private CharacterController body;
        private LineRenderer halo;
        private readonly Vector3[] points = new Vector3[Segments];
        private bool possessed;
        private float timerPct = 1f;
        private float phase;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            body = GetComponent<CharacterController>();
            var go = new GameObject("Possession Low Time Halo");
            go.layer = 2; // Ignore Raycast; no collider or effect on possession/aiming.
            go.transform.SetParent(transform, false);
            halo = go.AddComponent<LineRenderer>();
            halo.sharedMaterial = Resources.Load<Material>("P3/HeavySmashIndicator");
            halo.useWorldSpace = true;
            halo.loop = true;
            halo.positionCount = Segments;
            halo.numCornerVertices = 2;
            halo.shadowCastingMode = ShadowCastingMode.Off;
            halo.receiveShadows = false;
            halo.enabled = false;
        }

        private void OnEnable()
        {
            EventBus.Possessed += HandlePossessed;
            EventBus.Ejected += OnEjected;
            EventBus.PossessTimerUpdated += OnTimer;
            EventBus.LevelCompleted += Stop;
            EventBus.LevelFailed += Stop;
            EventBus.SpiritDied += Stop;
            EventBus.GameComplete += Stop;

            var possession = FindFirstObjectByType<PossessionController>();
            if (possession != null && possession.PossessedEnemy == enemy)
            {
                HandlePossessed(enemy, possession.CurrentState);
                OnTimer(possession.PossessTimer, possession.PossessDuration);
            }
        }

        private void OnDisable()
        {
            EventBus.Possessed -= HandlePossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.PossessTimerUpdated -= OnTimer;
            EventBus.LevelCompleted -= Stop;
            EventBus.LevelFailed -= Stop;
            EventBus.SpiritDied -= Stop;
            EventBus.GameComplete -= Stop;
            Stop();
        }

        internal void HandlePossessed(EnemyBase target, PlayerState state)
        {
            if (target != enemy) return;
            possessed = true;
            timerPct = 1f;
            phase = 0f;
            halo.enabled = false;
        }

        private void OnEjected(EnemyBase target, bool forced)
        {
            if (target == enemy) Stop();
        }

        private void OnTimer(float current, float max)
        {
            if (possessed) timerPct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void Stop()
        {
            possessed = false;
            phase = 0f;
            if (halo != null) halo.enabled = false;
        }

        private void LateUpdate()
        {
            bool show = possessed && enemy.IsPossessed && enemy.CurrentHP > 0
                && timerPct > 0f && timerPct <= orangeThreshold
                && (GameManager.Instance == null
                    || GameManager.Instance.CurrentState == LevelState.InProgress);
            halo.enabled = show;
            if (!show)
            {
                phase = 0f;
                return;
            }

            // A slow, continuous pulse stays readable; no strobe, body tint or disappearing body.
            // Integrating phase avoids jumps when the remaining time changes the pulse rate.
            float urgency = 1f - timerPct / orangeThreshold;
            phase = Mathf.Repeat(phase + Time.deltaTime * Mathf.Lerp(1.2f, 2f, urgency), 1f);
            float pulse = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            Color color = WarningColor;
            color.a = Mathf.Lerp(0.55f, 1f, pulse);
            halo.startColor = halo.endColor = color;
            halo.widthMultiplier = Mathf.Lerp(0.09f, 0.14f, pulse);

            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float radius = Mathf.Max(0.65f, (body != null ? body.radius * scale : 0.5f) + 0.25f);
            radius *= 1f + 0.08f * pulse;
            Vector3 center = transform.position;
            center.y = (body != null && body.enabled ? body.bounds.min.y : center.y - 1f) + 0.07f;
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * Mathf.PI * 2f;
                points[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            halo.SetPositions(points);
        }
    }
}
