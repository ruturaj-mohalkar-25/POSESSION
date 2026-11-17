using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession.Level04Gold
{
    public sealed class Level4Director : MonoBehaviour
    {
        [SerializeField] private EngineerEnemy[] engineers;
        [SerializeField] private Level4Vault vault;
        [SerializeField] private Level4AccessGate accessGate;
        [SerializeField] private Level4Cipher cipher;
        [SerializeField] private Door galleryDoor;
        [SerializeField] private Level4AccessGate[] securityDoors = new Level4AccessGate[0];
        [SerializeField] private HUDManager hud;
        [SerializeField] private Text failureTitle;
        private PossessionController player;
        private SpiritController playerSpirit;
        private Text context, roomTitle, cipherNote;
        private GameObject overlayRoot, statusPanel, contextPanel, cipherPanel;
        private RectTransform statusRect;
        private Camera cameraView;
        private CameraFollow follow;
        private Quaternion cameraRotation;
        private bool surveying, failed, levelEnded;
        public bool EngineerLost => failed;

        private void Start()
        {
            player = FindFirstObjectByType<PossessionController>();
            playerSpirit = player != null ? player.GetComponent<SpiritController>() : null;
            if (engineers == null || engineers.Length == 0) engineers = FindObjectsByType<EngineerEnemy>(FindObjectsSortMode.None);
            cameraView = Camera.main;
            if (cameraView != null) { follow = cameraView.GetComponent<CameraFollow>(); cameraRotation = cameraView.transform.rotation; }
            BuildLabels();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { LevelFlow.Load(gameObject.scene.name); return; }
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) { StopSurvey(); if (context != null) context.text = ""; return; }
            if (!failed && !vault.IsOpen && engineers != null && engineers.Length > 0
                && !engineers.Any(e => e != null && e.CurrentHP > 0 && e.gameObject.activeInHierarchy))
            {
                failed = true;
                if (hud != null) hud.SetLevelObjective("NO ENGINEER LEFT • RETRY");
                if (failureTitle != null) failureTitle.text = "NO ENGINEER LEFT";
                EventBus.LevelFailed?.Invoke();
                return;
            }
            if (player == null) return;
            Transform body = player.PossessedEnemy != null ? player.PossessedEnemy.transform : player.transform;
            Vector3 p = body.position;
            if (roomTitle != null) roomTitle.text = p.y > 3.5f ? "UPPER FLOOR" : "LOWER FLOOR";
            bool recovered = cipher != null && cipher.Recovered;
            if (cipherNote != null) cipherNote.text = "CW TRIANGLE  •  CCW SQUARE";
            if (cipherPanel != null) cipherPanel.SetActive(recovered);
            if (statusRect != null) statusRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, recovered ? 108f : 44f);
            if (hud != null) hud.SetLevelObjective(
                !recovered ? galleryDoor != null && !galleryDoor.IsOpen
                    ? "1/4  GUARD • OPEN THE GALLERY"
                    : "2/4  ENGINEER • RECOVER THE CIPHER"
                : accessGate.IsOpen ? "4/4  ENGINEER • OPEN THE VAULT" : "3/4  ENGINEER • OPEN SERVICE ACCESS");
            if (context == null) return;
            context.text = "";
            var plan = new Vector3(p.x / Level4Layout.GeometryScale, p.y, p.z / Level4Layout.GeometryScale);
            bool overlook = plan.y > 4f && plan.x >= 38f && plan.x <= 49f && plan.z >= 34f && plan.z <= 46f;
            bool nearCipher = cipher != null && !recovered && cipher.RecoveryPoint != null
                && Vector3.Distance(p, cipher.RecoveryPoint.position) < 4f;
            if (nearCipher)
            {
                StopSurvey();
                context.text = cipher.CanRecover(body) ? "F • COPY CIPHER" : "CIPHER • ENGINEER REQUIRED";
                if (Input.GetKeyDown(KeyCode.F)) cipher.TryRecover(body);
            }
            else if (overlook)
            {
                context.text = "HOLD F • LOOK BELOW";
                if (Input.GetKey(KeyCode.F) && !vault.IsInteracting) Survey(); else StopSurvey();
            }
            else StopSurvey();
            if (securityDoors != null)
                foreach (var gate in securityDoors)
                {
                    if (gate == null || gate.IsOpen) continue;
                    if (gate.CanUnlock(body))
                    {
                        context.text = "F • OPEN SECURITY DOOR";
                        if (Input.GetKeyDown(KeyCode.F)) gate.TryUnlock(body);
                        break;
                    }
                    if (gate.CanRelease(body))
                    {
                        context.text = "F • RELEASE RETURN DOOR";
                        if (Input.GetKeyDown(KeyCode.F)) gate.TryRelease(body);
                        break;
                    }
                    if (gate.EngineerPoint != null && Vector3.Distance(p, gate.EngineerPoint.position) < 3.5f)
                        context.text = "DOOR • ENGINEER REQUIRED";
                }
            if (accessGate.CanRelease(body))
            {
                context.text = "F • OPEN SERVICE ACCESS";
                if (Input.GetKeyDown(KeyCode.F)) accessGate.TryRelease(body);
            }
            if (!vault.IsOpen && vault.OperatorPoint != null && Vector3.Distance(p, vault.OperatorPoint.position) < 4f)
                context.text = !vault.HasCipher ? "VAULT • RECOVER CIPHER FIRST"
                    : vault.CanOperate() ? "F • OPERATE THE VAULT" : "VAULT • ENGINEER REQUIRED";
            if (vault.IsInteracting) context.text = "";
        }

        private void Survey()
        {
            if (cameraView == null) return;
            if (!surveying) { cameraRotation = cameraView.transform.rotation; surveying = true; }
            if (follow != null) follow.enabled = false;
            cameraView.transform.position = Vector3.Lerp(cameraView.transform.position, Level4Layout.Map(new Vector3(29f, 31f, 16f)), Time.deltaTime * 4f);
            cameraView.transform.rotation = Quaternion.Slerp(cameraView.transform.rotation,
                Quaternion.LookRotation(Level4Layout.Map(new Vector3(28f, 0f, 38f)) - cameraView.transform.position), Time.deltaTime * 4f);
        }
        private void StopSurvey()
        {
            if (!surveying) return;
            surveying = false;
            if (cameraView != null) cameraView.transform.rotation = cameraRotation;
            if (follow != null) follow.enabled = true;
        }
        private void LateUpdate()
        {
            bool playing = !levelEnded && !failed && (playerSpirit == null || playerSpirit.CurrentHP > 0)
                && (GameManager.Instance == null
                || GameManager.Instance.CurrentState == LevelState.InProgress);
            bool showing = playing && (vault == null || !vault.IsInteracting);
            if (overlayRoot != null) overlayRoot.SetActive(showing);
            if (statusPanel != null) statusPanel.SetActive(showing);
            if (contextPanel != null) contextPanel.SetActive(showing && context != null && context.text.Length > 0);
        }

        private void OnEnable()
        {
            EventBus.LevelCompleted += HideForLevelEnd;
            EventBus.LevelFailed += HideForLevelEnd;
            EventBus.SpiritDied += HideForLevelEnd;
            EventBus.GameComplete += HideForLevelEnd;
            if (overlayRoot != null) overlayRoot.SetActive(true);
        }

        private void OnDisable()
        {
            EventBus.LevelCompleted -= HideForLevelEnd;
            EventBus.LevelFailed -= HideForLevelEnd;
            EventBus.SpiritDied -= HideForLevelEnd;
            EventBus.GameComplete -= HideForLevelEnd;
            StopSurvey();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private void HideForLevelEnd()
        {
            levelEnded = true;
            StopSurvey();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private void BuildLabels()
        {
            var root = new GameObject("Level 4 context", typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            overlayRoot = root;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var status = Panel(root.transform, "FloorStatus", new Vector2(1f, 1f),
                new Vector2(-24f, -20f), new Vector2(340f, 108f));
            statusPanel = status.gameObject;
            statusRect = status;
            roomTitle = GoldTheme.Label(status, "FloorLabel", "", 20,
                Vector2.up, Vector2.one, TextAnchor.MiddleRight);
            var floorRect = roomTitle.rectTransform;
            floorRect.pivot = new Vector2(.5f, 1f);
            floorRect.anchoredPosition = new Vector2(0f, -4f);
            floorRect.sizeDelta = new Vector2(-32f, 36f);
            var cipherRect = GoldTheme.Rect(status, "RecoveredCipher", Vector2.up, Vector2.one);
            cipherRect.pivot = new Vector2(.5f, 1f);
            cipherRect.anchoredPosition = new Vector2(0f, -40f);
            cipherRect.sizeDelta = new Vector2(-32f, 62f);
            cipherPanel = cipherRect.gameObject;
            var heading = GoldTheme.Label(cipherPanel.transform, "CipherHeading", "CIPHER RECOVERED", 17,
                new Vector2(0, .53f), Vector2.one, TextAnchor.MiddleRight);
            heading.color = GoldTheme.Teal;
            cipherNote = GoldTheme.Label(cipherPanel.transform, "CipherNote", "", 18,
                Vector2.zero, new Vector2(1, .54f), TextAnchor.MiddleRight);
            cipherPanel.SetActive(false);

            var action = Panel(root.transform, "ActionPrompt", new Vector2(.5f, 0f),
                new Vector2(0f, 170f), new Vector2(640f, 44f));
            action.pivot = new Vector2(.5f, .5f);
            contextPanel = action.gameObject;
            context = GoldTheme.Label(action, "ActionText", "", 25,
                new Vector2(.03f, .1f), new Vector2(.97f, .9f));
            contextPanel.SetActive(false);
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = GoldTheme.Rect(parent, name, anchor, anchor);
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = GoldTheme.Panel;
            image.raycastTarget = false;
            GoldTheme.Box(rect, "Accent", Vector2.zero, new Vector2(.006f, 1f), GoldTheme.Teal);
            return rect;
        }
    }
}
