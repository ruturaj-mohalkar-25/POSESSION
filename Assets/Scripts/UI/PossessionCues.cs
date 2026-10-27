using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Cues that sit on the possessed body instead of the screen corners:
    // the body turns white (possession colour) and its name tag hides,
    // the possession timer floats above it, a short ability hint shows under it,
    // and a possessed Guard draws an aim line toward the cursor.
    public class PossessionCues : MonoBehaviour
    {
        [Header("Possessed body")]
        [SerializeField] private Material possessedMaterial; // white, like the Spirit; hats and tools keep their colours

        [Header("Timer above the body")]
        [SerializeField] private RectTransform timerGroup; // HUDManager still drives its fill and text
        [SerializeField] private float timerHeight = 2.4f;

        [Header("Ability hint")]
        [SerializeField] private Text hintText;
        [SerializeField] private float hintTime = 3f;
        [SerializeField] private float hintFadeTime = 0.5f;
        [SerializeField] private float hintDrop = 70f; // screen pixels below the body, before canvas scaling

        [Header("Guard aim line")]
        [SerializeField] private Color aimColor = new Color(1f, 0.75f, 0.2f, 0.9f); // same warm gold as the bullet
        [SerializeField] private float aimLength = 6f;
        [SerializeField] private float aimStartWidth = 0.22f;
        [SerializeField] private float aimEndWidth = 0.08f;
        [SerializeField] private float aimHeight = 1f;

        private EnemyBase possessed;
        private ShootAbility shoot;
        private Canvas canvas;
        private Camera cam;
        private LineRenderer aimLine;
        private float hintTimer;
        private Renderer bodyRenderer;
        private Material bodyMaterial;
        private GameObject hiddenLabel;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            aimLine = CreateAimLine();
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.LevelCompleted += Clear;
            EventBus.LevelFailed += Clear;
            EventBus.SpiritDied += Clear;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.LevelCompleted -= Clear;
            EventBus.LevelFailed -= Clear;
            EventBus.SpiritDied -= Clear;
        }

        private void Start()
        {
            if (hintText != null) hintText.gameObject.SetActive(false);
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            Clear();
            possessed = enemy;
            shoot = enemy != null ? enemy.GetComponent<ShootAbility>() : null;
            if (enemy != null)
            {
                // Only the body itself turns white; P5_Model parts (cap, pistol, hat, hammer) are children
                bodyRenderer = enemy.GetComponent<Renderer>();
                if (bodyRenderer != null && possessedMaterial != null)
                {
                    bodyMaterial = bodyRenderer.sharedMaterial;
                    bodyRenderer.sharedMaterial = possessedMaterial;
                }

                // The "GUARD" / "HEAVY" tag is redundant while you are that body
                foreach (var label in FindObjectsByType<WorldLabel>(FindObjectsSortMode.None))
                {
                    if (label.Follow != enemy.transform) continue;
                    hiddenLabel = label.gameObject;
                    hiddenLabel.SetActive(false);
                    break;
                }
            }

            string hint = state switch
            {
                PlayerState.Possessed_Guard => "Shoot",
                PlayerState.Possessed_Heavy => "Smash",
                _ => ""
            };
            if (hintText != null && hint.Length > 0)
            {
                hintText.text = hint;
                hintText.gameObject.SetActive(true);
                hintTimer = hintTime;
            }
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            Clear();
        }

        private void Clear()
        {
            if (bodyRenderer != null && bodyMaterial != null) bodyRenderer.sharedMaterial = bodyMaterial;
            bodyRenderer = null;
            bodyMaterial = null;
            if (hiddenLabel != null) hiddenLabel.SetActive(true);
            hiddenLabel = null;

            possessed = null;
            shoot = null;
            hintTimer = 0f;
            if (hintText != null) hintText.gameObject.SetActive(false);
            if (aimLine != null) aimLine.enabled = false;
        }

        private void LateUpdate()
        {
            if (possessed == null || !possessed.isActiveAndEnabled)
            {
                if (aimLine != null) aimLine.enabled = false;
                return;
            }
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Overlay canvas works in screen pixels
            if (timerGroup != null)
                PlaceAt(timerGroup, possessed.transform.position + Vector3.up * timerHeight, 0f);

            if (hintText != null && hintText.gameObject.activeSelf)
            {
                hintTimer -= Time.unscaledDeltaTime;
                PlaceAt(hintText.rectTransform, possessed.transform.position, -hintDrop);
                var c = hintText.color;
                c.a = Mathf.Clamp01(hintTimer / hintFadeTime);
                hintText.color = c;
                if (hintTimer <= 0f) hintText.gameObject.SetActive(false);
            }

            UpdateAimLine();
        }

        private void PlaceAt(RectTransform rect, Vector3 world, float screenOffsetY)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rect.position = new Vector3(screen.x, screen.y + screenOffsetY * scale, 0f);
        }

        // Same aim as ShootAbility: the point on the ground under the cursor, flattened
        private void UpdateAimLine()
        {
            if (aimLine == null) return;
            bool show = shoot != null && shoot.UsesLeft > 0 && Time.timeScale > 0f;
            aimLine.enabled = show;
            if (!show) return;

            Vector3 start = possessed.transform.position;
            start.y = aimHeight;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!new Plane(Vector3.up, 0f).Raycast(ray, out float distance))
            {
                aimLine.enabled = false;
                return;
            }
            Vector3 dir = ray.GetPoint(distance) - start;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                aimLine.enabled = false;
                return;
            }
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, start + dir.normalized * aimLength);
        }

        private LineRenderer CreateAimLine()
        {
            var go = new GameObject("AimLine");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = aimStartWidth;
            line.endWidth = aimEndWidth;
            line.numCapVertices = 4; // rounded ends read as a beam rather than a hairline
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = aimColor;
            line.endColor = new Color(aimColor.r, aimColor.g, aimColor.b, aimColor.a * 0.25f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }
    }
}
