using UnityEngine;

namespace Possession
{
    // Sells the Heavy's weight while the player drives it: a slower top speed (a bigger
    // gap below the Guard), dust puffs at the feet, and a short stomp bob each stride.
    // Runtime-only, installed by EnemyReadabilityInstaller; no shared prefab or AI edits.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeavyEnemy))]
    public sealed class HeavyFeel : MonoBehaviour
    {
        [SerializeField, Range(0.3f, 1f)] private float slowFactor = 0.65f;
        [SerializeField, Min(0.2f)] private float strideDistance = 1.1f; // metres per footstep
        [SerializeField, Min(0f)] private float bobHeight = 0.08f;
        [SerializeField, Min(0.01f)] private float bobTime = 0.18f;

        private HeavyEnemy heavy;
        private CharacterController body;
        private Transform mesh;      // visual child that bobs; never the collider root
        private Vector3 meshHome;
        private static Material dustMaterial;

        private bool possessed;
        private Vector3 lastFootPosition;
        private float sinceStride;
        private float bobAge = Mathf.Infinity;

        private void Awake()
        {
            heavy = GetComponent<HeavyEnemy>();
            body = GetComponent<CharacterController>();

            // Widen the speed gap vs the Guard so the Heavy reads as heavy and slow.
            heavy.MoveSpeed *= slowFactor;

            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.transform != transform)
            {
                mesh = meshRenderer.transform;
                meshHome = mesh.localPosition;
            }

            EnsureDustMaterial();
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;

            // Covers being added or enabled during an existing possession.
            var possession = FindFirstObjectByType<PossessionController>();
            if (possession != null) OnPossessed(possession.PossessedEnemy, possession.CurrentState);
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            RestoreMesh();
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            possessed = enemy == (EnemyBase)heavy && state == PlayerState.Possessed_Heavy;
            lastFootPosition = transform.position;
            sinceStride = 0f;
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            if (enemy != (EnemyBase)heavy) return;
            possessed = false;
            RestoreMesh();
        }

        private void Update()
        {
            bool running = possessed && (GameManager.Instance == null
                || GameManager.Instance.CurrentState == LevelState.InProgress);
            if (running)
            {
                Vector3 step = transform.position - lastFootPosition;
                step.y = 0f;
                sinceStride += step.magnitude;
                lastFootPosition = transform.position;
                if (sinceStride >= strideDistance)
                {
                    sinceStride = 0f;
                    Stomp();
                }
            }
            AnimateBob();
        }

        private void Stomp()
        {
            bobAge = 0f;
            if (dustMaterial == null) return;

            Vector3 feet = transform.position;
            if (body != null && body.enabled) feet.y = body.bounds.min.y + 0.05f;
            SimpleFX.Burst(feet, dustMaterial, 0.25f, 1.1f, 0.35f);
        }

        private void AnimateBob()
        {
            if (mesh == null) return;
            if (bobAge >= bobTime)
            {
                if (mesh.localPosition != meshHome) mesh.localPosition = meshHome;
                return;
            }
            bobAge += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(bobAge / bobTime) * Mathf.PI);
            mesh.localPosition = meshHome - Vector3.up * bobHeight * k;
        }

        private void RestoreMesh()
        {
            if (mesh != null) mesh.localPosition = meshHome;
            bobAge = Mathf.Infinity;
        }

        private static void EnsureDustMaterial()
        {
            if (dustMaterial != null) return;
            // Flat unlit puff: ignores vertex colour, so a plain primitive shows reliably.
            var shader = Resources.Load<Shader>("P3/ReadabilityDust");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            dustMaterial = new Material(shader) { color = new Color(0.82f, 0.79f, 0.72f, 0.55f) };
        }
    }
}
