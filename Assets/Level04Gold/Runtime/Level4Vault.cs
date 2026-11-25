using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Possession.Level04Gold
{
    // The route supplies the challenge; the dial keeps progress within this attempt.
    public sealed class Level4Vault : MonoBehaviour
    {
        [SerializeField] private Transform operatorPoint;
        [SerializeField] private Transform doorLeaf, wheelPointer, firstBolt, secondBolt;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private Level4Cipher cipher;
        [SerializeField] private float firstStop = 60f, secondStop = 300f, tolerance = 12f;
        private PossessionController player;
        private EngineerEnemy engineer;
        private bool engineerWasEnabled;
        private float lastDirection;
        private Canvas overlay;
        private RectTransform dialPointer;
        private Text progressText, feedbackText, hostTime, instructionText;
        private Image boltOne, boltTwo, cipherFirst, cipherSecond;
        private Button rotateLeft, rotateRight, confirmButton, handleButton;
        private static readonly Color ObjectiveGold = new Color(.95f, .75f, .2f);
        private static readonly Color SuccessGreen = new Color(.2f, .85f, .3f);
        private static readonly Color DangerRed = new Color(.9f, .2f, .2f);
        private Quaternion closedDoor;
        public bool IsInteracting { get; private set; }
        public int AlignedSteps { get; private set; }
        public float DialAngle { get; private set; }
        public bool IsOpen { get; private set; }
        public Transform OperatorPoint => operatorPoint;
        public bool HasCipher => cipher != null && cipher.Recovered;

        private void Awake()
        {
            player = FindFirstObjectByType<PossessionController>();
            if (doorLeaf != null) closedDoor = doorLeaf.localRotation;
        }

        private void OnEnable()
        {
            EventBus.LevelCompleted += EndInteraction;
            EventBus.LevelFailed += EndInteraction;
            EventBus.SpiritDied += EndInteraction;
            EventBus.GameComplete += EndInteraction;
            EventBus.Ejected += OnEjected;
        }

        public bool CanOperate()
        {
            if (IsOpen || !HasCipher || operatorPoint == null || Time.timeScale <= 0f || player == null) return false;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            var host = player.PossessedEnemy as EngineerEnemy;
            if (host == null || host.CurrentHP <= 0 || !host.gameObject.activeInHierarchy) return false;
            if (Vector3.Distance(host.transform.position, operatorPoint.position) > 3f
                || Mathf.Abs(host.transform.position.y - operatorPoint.position.y) > 2f) return false;
            foreach (var hit in Physics.RaycastAll(host.transform.position, operatorPoint.position - host.transform.position,
                         Vector3.Distance(host.transform.position, operatorPoint.position), ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(host.transform) || hit.transform.IsChildOf(operatorPoint)) continue;
                return false;
            }
            return true;
        }

        public bool TryBeginInteraction()
        {
            if (IsInteracting || !CanOperate()) return false;
            engineer = (EngineerEnemy)player.PossessedEnemy;
            engineerWasEnabled = engineer.enabled;
            engineer.enabled = false; // A/D turns the dial while the shared possession timer continues.
            IsInteracting = true;
            if (overlay == null) BuildOverlay();
            overlay.gameObject.SetActive(true);
            RefreshOverlay();
            return true;
        }

        public void EndInteraction()
        {
            IsInteracting = false;
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (engineer != null) engineer.enabled = engineerWasEnabled;
            engineer = null;
        }

        public void RotateDial(float degrees)
        {
            if (!IsInteracting || !CanOperate() || AlignedSteps >= 2 || Mathf.Abs(degrees) < .001f) return;
            DialAngle = Mathf.Repeat(DialAngle + degrees, 360f);
            lastDirection = Mathf.Sign(degrees);
            if (feedbackText != null) feedbackText.text = "";
            RefreshOverlay();
        }

        public bool TryConfirmAlignment()
        {
            if (!IsInteracting || !CanOperate() || AlignedSteps >= 2) return false;
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > .01f)
            {
                ShowFeedback("RELEASE TO CONFIRM", ObjectiveGold);
                return false;
            }
            float target = AlignedSteps == 0 ? firstStop : secondStop;
            float direction = AlignedSteps == 0 ? 1f : -1f;
            if (Mathf.Abs(Mathf.DeltaAngle(DialAngle, target)) > tolerance)
            {
                ShowFeedback(AlignedSteps == 0 ? "ALIGN POINTER WITH TRIANGLE" : "ALIGN POINTER WITH SQUARE", DangerRed);
                return false;
            }
            if (lastDirection != direction)
            {
                ShowFeedback(AlignedSteps == 0 ? "TURN RIGHT / CLOCKWISE, THEN CONFIRM" : "TURN LEFT / COUNTERCLOCKWISE, THEN CONFIRM", DangerRed);
                return false;
            }
            DialAngle = target;
            AlignedSteps++;
            lastDirection = 0f;
            ShowFeedback(AlignedSteps == 2 ? "BOLTS RELEASED • TURN HANDLE" : "FIRST BOLT RELEASED", SuccessGreen);
            if (firstBolt != null && AlignedSteps >= 1) firstBolt.localScale = new Vector3(.18f, .28f, .18f);
            if (secondBolt != null && AlignedSteps >= 2) secondBolt.localScale = new Vector3(.18f, .28f, .18f);
            RefreshOverlay();
            return true;
        }

        public bool TryOpenHandle()
        {
            if (IsOpen || !IsInteracting || AlignedSteps != 2 || !CanOperate()) return false;
            IsOpen = true;
            EndInteraction();
            var blocker = GetComponent<Collider>();
            if (blocker != null) blocker.enabled = false;
            CanvasGroup end = null;
            if (completionPanel != null)
            {
                end = completionPanel.GetComponent<CanvasGroup>();
                if (end == null) end = completionPanel.AddComponent<CanvasGroup>();
                end.alpha = 0f;
            }
            // Win is accepted now, before expiry can eject the successful operator.
            EventBus.LevelCompleted?.Invoke();
            StartCoroutine(OpenAnimation(end));
            return true;
        }

        private IEnumerator OpenAnimation(CanvasGroup end)
        {
            for (float t = 0f; t < 1.1f; t += Time.unscaledDeltaTime)
            {
                if (doorLeaf != null) doorLeaf.localRotation = closedDoor * Quaternion.Euler(0f, -105f * Mathf.SmoothStep(0, 1, t / .8f), 0f);
                if (end != null) end.alpha = Mathf.Clamp01((t - .65f) / .4f);
                yield return null;
            }
            if (end != null) end.alpha = 1f;
        }

        private void Update()
        {
            if (player == null) player = FindFirstObjectByType<PossessionController>();
            if (Time.timeScale <= 0f) return;
            if (IsInteracting)
            {
                if (!CanOperate() || player.PossessedEnemy != engineer) { EndInteraction(); return; }
                float input = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(input) > .01f) RotateDial(input * 90f * Time.deltaTime);
                else if (AlignedSteps < 2)
                {
                    float stop = AlignedSteps == 0 ? firstStop : secondStop;
                    if (Mathf.Abs(Mathf.DeltaAngle(DialAngle, stop)) <= tolerance) { DialAngle = stop; RefreshOverlay(); }
                }
                if (Input.GetKeyDown(KeyCode.Return)) { if (AlignedSteps < 2) TryConfirmAlignment(); else TryOpenHandle(); }
                if (Input.GetKeyDown(KeyCode.F)) EndInteraction();
                RefreshHostTime();
            }
            else if (Input.GetKeyDown(KeyCode.F) && CanOperate()) TryBeginInteraction();
        }

        private void LateUpdate()
        {
            // The shared pause menu owns Escape/P and must stay above the vault controls.
            if (overlay != null) overlay.gameObject.SetActive(IsInteracting && Time.timeScale > 0f);
        }

        private void OnEjected(EnemyBase host, bool forced)
        {
            if (IsInteracting && host == engineer) EndInteraction();
        }

        private void OnDisable()
        {
            EventBus.LevelCompleted -= EndInteraction;
            EventBus.LevelFailed -= EndInteraction;
            EventBus.SpiritDied -= EndInteraction;
            EventBus.GameComplete -= EndInteraction;
            EventBus.Ejected -= OnEjected;
            EndInteraction();
        }
        private void OnDestroy() { if (overlay != null) Destroy(overlay.gameObject); }

        private void RefreshOverlay()
        {
            if (wheelPointer != null) wheelPointer.localRotation = Quaternion.Euler(0f, 0f, -DialAngle);
            if (dialPointer != null) dialPointer.localRotation = Quaternion.Euler(0f, 0f, -DialAngle);
            if (progressText != null) progressText.text = $"BOLTS {AlignedSteps} / 2";
            if (instructionText != null) instructionText.text = AlignedSteps == 0
                ? "1/3  HOLD D / RIGHT: CLOCKWISE TO TRIANGLE\nRELEASE, THEN PRESS ENTER / CONFIRM"
                : AlignedSteps == 1
                    ? "2/3  HOLD A / LEFT: COUNTERCLOCKWISE TO SQUARE\nRELEASE, THEN PRESS ENTER / CONFIRM"
                    : "BOTH BOLTS RELEASED\n3/3  CLICK TURN HANDLE OR PRESS ENTER";
            if (boltOne != null) boltOne.color = AlignedSteps >= 1 ? SuccessGreen : GoldTheme.Secondary;
            if (boltTwo != null) boltTwo.color = AlignedSteps >= 2 ? SuccessGreen : GoldTheme.Secondary;
            if (cipherFirst != null) cipherFirst.color = AlignedSteps == 0 ? GoldTheme.Teal : GoldTheme.Secondary;
            if (cipherSecond != null) cipherSecond.color = AlignedSteps == 1 ? GoldTheme.Teal : GoldTheme.Secondary;
            if (rotateLeft != null) rotateLeft.interactable = AlignedSteps < 2;
            if (rotateRight != null) rotateRight.interactable = AlignedSteps < 2;
            if (confirmButton != null) confirmButton.interactable = AlignedSteps < 2;
            if (handleButton != null) handleButton.interactable = AlignedSteps == 2;
            RefreshHostTime();
        }

        private void RefreshHostTime()
        {
            if (hostTime == null || player == null) return;
            hostTime.text = $"HOST {Mathf.Max(0f, player.PossessTimer):0.0}s";
            float fraction = player.PossessDuration > 0f ? player.PossessTimer / player.PossessDuration : 0f;
            hostTime.color = fraction <= .2f ? DangerRed : fraction <= .35f ? ObjectiveGold : GoldTheme.TextColor;
        }

        private void ShowFeedback(string value, Color color)
        {
            if (feedbackText == null) return;
            feedbackText.text = value;
            feedbackText.color = color;
        }

        private void BuildOverlay()
        {
            var root = new GameObject("Vault dial", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay = root.GetComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceOverlay; overlay.sortingOrder = 60;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var panel = ImageNode(root.transform, "Mechanical lock", new Vector2(0, 30), new Vector2(720, 820), GoldTheme.Panel);
            panel.raycastTarget = true;
            GoldTheme.Box(panel.transform, "Accent", new Vector2(0, .994f), Vector2.one, GoldTheme.Teal);
            var title = Label(panel.transform, "VAULT • ENGINEER", new Vector2(-110, 361), new Vector2(410, 42), 30, GoldTheme.TextColor, "VaultTitle");
            title.alignment = TextAnchor.MiddleLeft;
            hostTime = Label(panel.transform, "", new Vector2(230, 361), new Vector2(170, 38), 24, GoldTheme.TextColor, "HostTime");
            hostTime.alignment = TextAnchor.MiddleRight;

            cipherFirst = ImageNode(panel.transform, "CipherStepOne", new Vector2(-160, 303), new Vector2(300, 40), GoldTheme.Teal);
            cipherSecond = ImageNode(panel.transform, "CipherStepTwo", new Vector2(160, 303), new Vector2(300, 40), GoldTheme.Secondary);
            Label(cipherFirst.transform, "1  CW • TRIANGLE", Vector2.zero, new Vector2(276, 34), 21, GoldTheme.TextColor);
            Label(cipherSecond.transform, "2  CCW • SQUARE", Vector2.zero, new Vector2(276, 34), 21, GoldTheme.TextColor);

            instructionText = Label(panel.transform, "", new Vector2(0, 243), new Vector2(660, 58), 20, GoldTheme.TextColor, "VaultInstruction");
            var dial = ImageNode(panel.transform, "Dial plate", new Vector2(0, 50), new Vector2(300, 300), GoldTheme.Secondary);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                var tick = ImageNode(dial.transform, "Tick", new Vector2(Mathf.Sin(angle) * 135, Mathf.Cos(angle) * 135), new Vector2(3, 14), GoldTheme.TextColor);
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 30);
            }
            Mark(dial.transform, "TRIANGLE", firstStop); Mark(dial.transform, "SQUARE", secondStop);
            Mark(dial.transform, "CIRCLE", 150); Mark(dial.transform, "DIAMOND", 210);
            dialPointer = new GameObject("Dial pointer", typeof(RectTransform)).GetComponent<RectTransform>();
            dialPointer.SetParent(dial.transform, false); dialPointer.sizeDelta = Vector2.zero;
            ImageNode(dialPointer, "Pointer", new Vector2(0, 53), new Vector2(6, 106), ObjectiveGold);
            ImageNode(dialPointer, "Hub", Vector2.zero, new Vector2(21, 21), GoldTheme.TextColor);
            progressText = Label(panel.transform, "", new Vector2(0, -130), new Vector2(250, 34), 24, GoldTheme.TextColor, "BoltProgress");
            boltOne = ImageNode(panel.transform, "Bolt one", new Vector2(-148, -130), new Vector2(22, 22), GoldTheme.Secondary);
            boltTwo = ImageNode(panel.transform, "Bolt two", new Vector2(148, -130), new Vector2(22, 22), GoldTheme.Secondary);
            rotateLeft = Button(panel.transform, "RotateCCW", "< CCW", new Vector2(-220, -180), new Vector2(164, 48), () => RotateDial(-30));
            confirmButton = Button(panel.transform, "ConfirmAlignment", "CONFIRM", new Vector2(0, -180), new Vector2(224, 48), () => TryConfirmAlignment(), true);
            rotateRight = Button(panel.transform, "RotateCW", "CW >", new Vector2(220, -180), new Vector2(164, 48), () => RotateDial(30));
            feedbackText = Label(panel.transform, "", new Vector2(0, -228), new Vector2(640, 32), 21, GoldTheme.TextColor, "VaultFeedback");
            handleButton = Button(panel.transform, "TurnHandle", "TURN HANDLE", new Vector2(-125, -280), new Vector2(280, 48), () => TryOpenHandle(), true);
            Button(panel.transform, "LeaveVault", "LEAVE [F]", new Vector2(165, -280), new Vector2(240, 48), EndInteraction);
            Label(panel.transform, "A / D ROTATE   •   ENTER CONFIRM   •   E EJECT", new Vector2(0, -324), new Vector2(650, 26), 18, GoldTheme.TextColor, "VaultControls");
        }

        private static void Mark(Transform parent, string text, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            Label(parent, text, new Vector2(Mathf.Sin(a) * 103, Mathf.Cos(a) * 103), new Vector2(100, 26), 15, GoldTheme.TextColor, "Mark_" + text);
        }
        private static Image ImageNode(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var image = node.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int font, Color color, string name = "Label")
        {
            var label = GoldTheme.Label(parent, name, text, font, Vector2.one * .5f, Vector2.one * .5f);
            var rect = label.rectTransform; rect.pivot = Vector2.one * .5f; rect.anchoredPosition = pos; rect.sizeDelta = size;
            label.color = color;
            return label;
        }
        private static Button Button(Transform parent, string name, string text, Vector2 pos, Vector2 size,
            UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var button = GoldTheme.Button(parent, name, text, Vector2.one * .5f, Vector2.one * .5f, primary);
            var rect = button.GetComponent<RectTransform>(); rect.pivot = Vector2.one * .5f; rect.anchoredPosition = pos; rect.sizeDelta = size;
            GoldTheme.Style(button.GetComponentInChildren<Text>(), 24, TextAnchor.MiddleCenter);
            button.onClick.AddListener(action);
            return button;
        }
    }
}
