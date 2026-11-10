using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Cues that sit on the possessed body instead of the screen corners:
    // the body turns white (possession colour) and its name tag hides,
    // the circular possession timer and a short ability hint sit below it,
    // and a possessed Guard draws an aim line toward the cursor.
    [DefaultExecutionOrder(100)] // Position cues after CameraFollow has moved the camera.
    public class PossessionCues : MonoBehaviour
    {
        [Header("Possessed body")]
        [SerializeField] private Material possessedMaterial; // white, like the Spirit; hats and tools keep their colours

        [Header("Timer below the body")]
        [SerializeField] private RectTransform timerGroup; // HUDManager drives the circular countdown

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
        private CanvasGroup timerVisibility;
        private Material aimMaterial;
        private Canvas groundCanvas;
        private readonly Vector3[] timerCorners = new Vector3[4];

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            aimLine = CreateAimLine();
            if (timerGroup != null)
            {
                timerVisibility = timerGroup.GetComponent<CanvasGroup>();
                if (timerVisibility == null) timerVisibility = timerGroup.gameObject.AddComponent<CanvasGroup>();
                var ground = new GameObject("Possession Ground Ring", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(ground, gameObject.scene);
                ground.layer = 2;
                groundCanvas = ground.GetComponent<Canvas>();
                groundCanvas.renderMode = RenderMode.WorldSpace;
                groundCanvas.worldCamera = Camera.main;
                groundCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(52f, 52f);
                ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                ground.transform.localScale = Vector3.one * 0.035f;
                timerGroup.SetParent(ground.transform, false);
                timerGroup.anchorMin = timerGroup.anchorMax = new Vector2(0.5f, 0.5f);
                timerGroup.anchoredPosition3D = Vector3.zero;
                timerGroup.localRotation = Quaternion.identity;
            }
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
            EventBus.LevelCompleted += Clear;
            EventBus.LevelFailed += Clear;
            EventBus.SpiritDied += Clear;
            EventBus.GameComplete += Clear;
            var hud = GetComponent<HUDManager>();
            var possession = hud != null ? hud.PlayerPossession : null;
            if (possession != null && possession.PossessedEnemy != null
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress))
                OnPossessed(possession.PossessedEnemy, possession.CurrentState);
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
            EventBus.LevelCompleted -= Clear;
            EventBus.LevelFailed -= Clear;
            EventBus.SpiritDied -= Clear;
            EventBus.GameComplete -= Clear;
            Clear();
        }

        private void OnDestroy()
        {
            if (aimLine != null) Destroy(aimLine.gameObject);
            if (aimMaterial != null) Destroy(aimMaterial);
            if (groundCanvas != null) Destroy(groundCanvas.gameObject);
        }

        private void Start()
        {
            if (hintText != null) hintText.gameObject.SetActive(false);
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            Clear();
            possessed = enemy;
            if (timerVisibility != null) timerVisibility.alpha = enemy != null ? 1f : 0f;
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
            if (timerVisibility != null) timerVisibility.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (possessed == null || !possessed.isActiveAndEnabled)
            {
                if (aimLine != null) aimLine.enabled = false;
                if (timerVisibility != null) timerVisibility.alpha = 0f;
                if (hintText != null) { var tint = hintText.color; tint.a = 0f; hintText.color = tint; }
                return;
            }
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            if (groundCanvas != null) groundCanvas.worldCamera = cam;

            Vector3 feet = possessed.transform.position;
            if (bodyRenderer != null) feet.y = bodyRenderer.bounds.min.y;
            Vector3 viewport = cam.WorldToViewportPoint(feet);
            bool visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
                && viewport.y >= 0f && viewport.y <= 1f;
            if (timerVisibility != null) timerVisibility.alpha = visible ? 1f : 0f;

            // The countdown lies on the floor around the host's feet, like the reference.
            if (groundCanvas != null)
            {
                float floor = float.NegativeInfinity;
                foreach (var hit in Physics.RaycastAll(feet + Vector3.up * 0.5f, Vector3.down, 3f,
                    1 << 0, QueryTriggerInteraction.Ignore))
                    if (hit.normal.y > 0.5f && hit.point.y <= feet.y + 0.1f)
                    floor = Mathf.Max(floor, hit.point.y);
                if (float.IsNegativeInfinity(floor)) floor = feet.y;
                groundCanvas.transform.position = new Vector3(feet.x, floor + 0.04f, feet.z);
            }

            if (hintText != null && hintText.gameObject.activeSelf)
            {
                hintTimer -= Time.unscaledDeltaTime;
                float drop = hintDrop;
                if (timerGroup != null)
                {
                    timerGroup.GetWorldCorners(timerCorners);
                    float bottom = float.PositiveInfinity;
                    foreach (var corner in timerCorners) bottom = Mathf.Min(bottom, cam.WorldToScreenPoint(corner).y);
                    float scale = canvas != null ? canvas.scaleFactor : 1f;
                    drop = Mathf.Max(drop, (cam.WorldToScreenPoint(feet).y - bottom) / scale
                        + hintText.rectTransform.rect.height * 0.5f + 8f);
                }
                PlaceAt(hintText.rectTransform, feet, -drop);
                var c = hintText.color;
                c.a = visible ? Mathf.Clamp01(hintTimer / hintFadeTime) : 0f;
                hintText.color = c;
                if (hintTimer <= 0f) hintText.gameObject.SetActive(false);
            }

            UpdateAimLine();
            if (!visible && aimLine != null) aimLine.enabled = false;
        }

        private void PlaceAt(RectTransform rect, Vector3 world, float screenOffsetY)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 point = new Vector2(screen.x, screen.y + screenOffsetY * scale);
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Use canvas coordinates: its world plane follows the camera later in the frame.
                var root = canvas.transform as RectTransform;
                Rect pixels = canvas.pixelRect;
                Vector2 normalized = new Vector2((point.x - pixels.x) / pixels.width,
                    (point.y - pixels.y) / pixels.height);
                rect.position = root.TransformPoint(root.rect.min + Vector2.Scale(normalized, root.rect.size));
            }
            else if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvas.transform as RectTransform, point, canvas.worldCamera, out Vector3 position))
                    rect.position = position;
            }
            else rect.position = new Vector3(point.x, point.y, 0f);
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
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = aimStartWidth;
            line.endWidth = aimEndWidth;
            line.numCapVertices = 4; // rounded ends read as a beam rather than a hairline
            aimMaterial = new Material(Shader.Find("Sprites/Default"));
            line.sharedMaterial = aimMaterial;
            line.startColor = aimColor;
            line.endColor = new Color(aimColor.r, aimColor.g, aimColor.b, aimColor.a * 0.25f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }
    }
}
