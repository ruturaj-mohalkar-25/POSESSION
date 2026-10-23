using UnityEngine;
using UnityEngine.Rendering;

namespace Possession
{
    // Per-enemy readability cue: a ground ring plus a floating "E" badge.
    // A green ring and the "E" mark the enemy the Spirit can possess right now;
    // a red ring marks a chasing or hostile enemy. EnemyReadabilityController decides
    // the mode each frame; this component only draws it. Visuals are self-contained
    // (its own LineRenderer and TextMesh), so shared scenes, prefabs and enemy AI stay
    // untouched. Mirrors HeavySmashIndicator's runtime-visual approach.
    [DisallowMultipleComponent]
    public sealed class EnemyReadability : MonoBehaviour
    {
        public enum Mode { None, Possessable, Hostile }

        private const int Segments = 40;
        private static readonly Color PossessableColor = new Color(0.25f, 0.95f, 0.4f, 0.9f);
        private static readonly Color HostileColor = new Color(1f, 0.25f, 0.2f, 0.9f);

        private EnemyBase enemy;
        private CharacterController body;
        private LineRenderer ring;
        private Vector3[] ringPoints;
        private float ringRadius = 0.9f;
        private MeshRenderer badgeRenderer;
        private TextMesh badge;
        private Transform badgeTransform;
        private Camera cam;
        private Mode mode;

        public void SetMode(Mode value) => mode = value;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            body = GetComponent<CharacterController>();

            var material = Resources.Load<Material>("P3/HeavySmashIndicator");
            ringPoints = new Vector3[Segments];

            var ringGo = new GameObject("Readability Ring");
            ringGo.layer = 2; // Ignore Raycast; no collider, never blocks aim or possession.
            ringGo.transform.SetParent(transform, false);
            ring = ringGo.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = ringPoints.Length;
            ring.widthMultiplier = 0.08f;
            ring.numCapVertices = 2;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.enabled = false;

            CreateBadge();
            MeasureRadius();
        }

        private void CreateBadge()
        {
            var badgeGo = new GameObject("Possess Badge");
            badgeGo.layer = 2;
            badgeTransform = badgeGo.transform;
            badgeTransform.SetParent(transform, false);
            badge = badgeGo.AddComponent<TextMesh>();
            badge.text = "E";
            badge.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            badge.fontSize = 64;
            badge.characterSize = 0.08f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.fontStyle = FontStyle.Bold;
            badge.color = PossessableColor;

            badgeRenderer = badgeGo.GetComponent<MeshRenderer>();
            badgeRenderer.sharedMaterial = badge.font.material;
            badgeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            badgeRenderer.receiveShadows = false;
            badgeRenderer.enabled = false;
        }

        private void MeasureRadius()
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float r = body != null ? body.radius * scale : 0.5f;
            ringRadius = Mathf.Max(0.6f, r * 1.6f);
        }

        private void LateUpdate()
        {
            bool live = enemy != null && enemy.isActiveAndEnabled && enemy.CurrentHP > 0
                && (GameManager.Instance == null
                    || GameManager.Instance.CurrentState == LevelState.InProgress);
            Mode shown = live ? mode : Mode.None;

            bool showRing = shown != Mode.None;
            ring.enabled = showRing;
            badgeRenderer.enabled = shown == Mode.Possessable;
            if (!showRing) return;

            Color baseColor = shown == Mode.Possessable ? PossessableColor : HostileColor;
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
            Color pulsed = baseColor;
            pulsed.a = baseColor.a * pulse;
            ring.startColor = pulsed;
            ring.endColor = pulsed;

            Vector3 center = transform.position;
            float groundY = (body != null && body.enabled ? body.bounds.min.y : center.y) + 0.05f;
            for (int i = 0; i < ringPoints.Length; i++)
            {
                float angle = (float)i / ringPoints.Length * Mathf.PI * 2f;
                ringPoints[i] = new Vector3(center.x + Mathf.Cos(angle) * ringRadius, groundY,
                    center.z + Mathf.Sin(angle) * ringRadius);
            }
            ring.SetPositions(ringPoints);

            if (!badgeRenderer.enabled) return;
            if (cam == null) cam = Camera.main;
            float top = body != null && body.enabled ? body.bounds.max.y : center.y + 1.5f;
            badgeTransform.position = new Vector3(center.x, top + 0.6f, center.z);
            if (cam != null) badgeTransform.rotation = cam.transform.rotation;
            badge.color = pulsed;
        }
    }
}
