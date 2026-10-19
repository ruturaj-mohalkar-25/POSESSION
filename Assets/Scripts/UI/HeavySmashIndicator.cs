using UnityEngine;
using UnityEngine.Rendering;

namespace Possession
{
    // A facing/range guide, not a preview of which objects the smash will hit.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeavyEnemy), typeof(SmashAbility))]
    public sealed class HeavySmashIndicator : MonoBehaviour
    {
        [SerializeField, Range(10f, 180f)] private float arcAngle = 90f;
        [SerializeField, Range(8, 64)] private int arcSegments = 24;
        [SerializeField, Min(0.01f)] private float lineWidth = 0.07f;
        [SerializeField, Min(0.01f)] private float groundOffset = 0.06f;
        [SerializeField] private Color readyColor = new Color(1f, 0.75f, 0.15f, 1f);
        [SerializeField] private Color cooldownColor = new Color(0.4f, 0.45f, 0.5f, 0.65f);

        private HeavyEnemy heavy;
        private SmashAbility smash;
        private CharacterController body;
        private GameObject visual;
        private LineRenderer arc;
        private LineRenderer shaft;
        private LineRenderer arrowhead;
        private Vector3[] arcPoints;
        private bool possessed;

        public bool IsVisible => arc != null && arc.enabled;

        private void Awake()
        {
            heavy = GetComponent<HeavyEnemy>();
            smash = GetComponent<SmashAbility>();
            body = GetComponent<CharacterController>();

            var material = Resources.Load<Material>("P3/HeavySmashIndicator");
            visual = new GameObject("Heavy Smash Guide");
            visual.layer = 2; // Ignore Raycast; the guide has no colliders either.
            visual.transform.SetParent(transform, false);
            arcPoints = new Vector3[Mathf.Clamp(arcSegments, 8, 64) + 1];
            arc = CreateLine("Range arc", arcPoints.Length, material);
            shaft = CreateLine("Facing direction", 2, material);
            arrowhead = CreateLine("Arrowhead", 3, material);
        }

        private void OnEnable()
        {
            EventBus.Possessed += HandlePossessed;
            EventBus.Ejected += OnEjected;
            EventBus.LevelCompleted += Hide;
            EventBus.LevelFailed += Hide;
            EventBus.SpiritDied += Hide;

            // Also supports adding/enabling the component during an existing possession.
            var possession = FindFirstObjectByType<PossessionController>();
            if (possession != null)
                HandlePossessed(possession.PossessedEnemy, possession.CurrentState);
        }

        private void OnDisable()
        {
            EventBus.Possessed -= HandlePossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.LevelCompleted -= Hide;
            EventBus.LevelFailed -= Hide;
            EventBus.SpiritDied -= Hide;
            Hide();
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }

        internal void HandlePossessed(EnemyBase enemy, PlayerState state)
        {
            possessed = enemy == heavy && state == PlayerState.Possessed_Heavy;
            Refresh();
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            if (enemy == heavy) Hide();
        }

        private void Hide()
        {
            possessed = false;
            SetVisible(false);
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            bool show = possessed && heavy != null && heavy.CurrentHP > 0
                && smash != null && smash.isActiveAndEnabled && smash.Range > 0f
                && (GameManager.Instance == null
                    || GameManager.Instance.CurrentState == LevelState.InProgress);
            SetVisible(show);
            if (!show) return;

            // Use world coordinates: the Heavy's 1.3 scale must not stretch a 2m attack.
            Vector3 origin = transform.position;
            origin.y = (body != null && body.enabled ? body.bounds.min.y : origin.y)
                + groundOffset;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float range = smash.Range;
            float halfAngle = Mathf.Clamp(arcAngle, 10f, 180f) * 0.5f;
            for (int i = 0; i < arcPoints.Length; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle,
                    (float)i / (arcPoints.Length - 1)) * Mathf.Deg2Rad;
                arcPoints[i] = origin + (forward * Mathf.Cos(angle)
                    + right * Mathf.Sin(angle)) * range;
            }
            arc.SetPositions(arcPoints);

            Vector3 tip = origin + forward * range;
            float arrowSize = Mathf.Min(0.3f, range * 0.2f);
            // Start beyond the body's footprint so the direction line remains readable.
            float start = body != null && body.enabled
                ? Mathf.Min(range * 0.5f, Mathf.Max(body.bounds.extents.x, body.bounds.extents.z))
                : 0f;
            shaft.SetPosition(0, origin + forward * start);
            shaft.SetPosition(1, tip);
            arrowhead.SetPosition(0, tip - forward * arrowSize - right * arrowSize * 0.6f);
            arrowhead.SetPosition(1, tip);
            arrowhead.SetPosition(2, tip - forward * arrowSize + right * arrowSize * 0.6f);

            Color color = smash.CanUse ? readyColor : cooldownColor;
            SetColor(arc, color);
            SetColor(shaft, color);
            SetColor(arrowhead, color);
        }

        private LineRenderer CreateLine(string label, int count, Material material)
        {
            var child = new GameObject(label);
            child.layer = 2;
            child.transform.SetParent(visual.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = count;
            line.widthMultiplier = lineWidth;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void SetVisible(bool visible)
        {
            if (arc == null) return;
            arc.enabled = shaft.enabled = arrowhead.enabled = visible;
        }

        private static void SetColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }
    }
}
