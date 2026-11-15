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
        private Text progressText, feedbackText, hostTime;
        private Image boltOne, boltTwo;
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
            RefreshOverlay();
        }

        public bool TryConfirmAlignment()
        {
            if (!IsInteracting || !CanOperate() || AlignedSteps >= 2) return false;
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > .01f)
            {
                if (feedbackText != null) feedbackText.text = "RELEASE DIAL TO CONFIRM";
                return false;
            }
            float target = AlignedSteps == 0 ? firstStop : secondStop;
            float direction = AlignedSteps == 0 ? 1f : -1f;
            if (Mathf.Abs(Mathf.DeltaAngle(DialAngle, target)) > tolerance || lastDirection != direction)
            {
                if (feedbackText != null) feedbackText.text = "BOLT STILL LOCKED";
                return false;
            }
            DialAngle = target;
            AlignedSteps++;
            lastDirection = 0f;
            if (feedbackText != null) feedbackText.text = AlignedSteps == 2 ? "BOLTS RELEASED • TURN HANDLE" : "FIRST BOLT RELEASED";
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
                if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape)) EndInteraction();
                if (hostTime != null) hostTime.text = $"HOST {player.PossessTimer:0.0}s";
            }
            else if (Input.GetKeyDown(KeyCode.F) && CanOperate()) TryBeginInteraction();
        }

        private void OnDisable() => EndInteraction();
        private void OnDestroy() { if (overlay != null) Destroy(overlay.gameObject); }

        private void RefreshOverlay()
        {
            if (wheelPointer != null) wheelPointer.localRotation = Quaternion.Euler(0f, 0f, -DialAngle);
            if (dialPointer != null) dialPointer.localRotation = Quaternion.Euler(0f, 0f, -DialAngle);
            if (progressText != null) progressText.text = $"BOLTS {AlignedSteps}/2";
            if (boltOne != null) boltOne.color = AlignedSteps >= 1 ? new Color(.25f, 1f, .6f) : new Color(.35f, .4f, .46f);
            if (boltTwo != null) boltTwo.color = AlignedSteps >= 2 ? new Color(.25f, 1f, .6f) : new Color(.35f, .4f, .46f);
        }

        private void BuildOverlay()
        {
            var root = new GameObject("Vault dial", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay = root.GetComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceOverlay; overlay.sortingOrder = 60;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var panel = ImageNode(root.transform, "Mechanical lock", new Vector2(0, 30), new Vector2(640, 740), new Color(.035f, .055f, .08f, .98f));
            Label(panel.transform, "VAULT • ENGINEER", new Vector2(0, 315), new Vector2(600, 48), 32, new Color(1f, .8f, .35f));
            hostTime = Label(panel.transform, "", new Vector2(0, 267), new Vector2(500, 38), 24, Color.white);
            var dial = ImageNode(panel.transform, "Dial plate", new Vector2(0, 55), new Vector2(340, 340), new Color(.12f, .18f, .23f));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                var tick = ImageNode(dial.transform, "Tick", new Vector2(Mathf.Sin(angle) * 148, Mathf.Cos(angle) * 148), new Vector2(4, 18), new Color(.45f, .6f, .65f));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 30);
            }
            Mark(dial.transform, "TRIANGLE", firstStop); Mark(dial.transform, "SQUARE", secondStop);
            Mark(dial.transform, "CIRCLE", 150); Mark(dial.transform, "DIAMOND", 210);
            dialPointer = new GameObject("Dial pointer", typeof(RectTransform)).GetComponent<RectTransform>();
            dialPointer.SetParent(dial.transform, false); dialPointer.sizeDelta = Vector2.zero;
            ImageNode(dialPointer, "Pointer", new Vector2(0, 57), new Vector2(7, 115), new Color(1f, .8f, .3f));
            ImageNode(dialPointer, "Hub", Vector2.zero, new Vector2(23, 23), new Color(.9f, .93f, .95f));
            progressText = Label(panel.transform, "BOLTS 0/2", new Vector2(0, -150), new Vector2(300, 36), 25, Color.white);
            boltOne = ImageNode(panel.transform, "Bolt one", new Vector2(-155, -150), new Vector2(24, 24), Color.gray);
            boltTwo = ImageNode(panel.transform, "Bolt two", new Vector2(155, -150), new Vector2(24, 24), Color.gray);
            Button(panel.transform, "< CCW", new Vector2(-205, -215), new Vector2(155, 46), () => RotateDial(-30));
            Button(panel.transform, "CONFIRM", new Vector2(0, -215), new Vector2(205, 46), () => TryConfirmAlignment());
            Button(panel.transform, "CW >", new Vector2(205, -215), new Vector2(155, 46), () => RotateDial(30));
            feedbackText = Label(panel.transform, "", new Vector2(0, -265), new Vector2(600, 34), 21, new Color(.55f, 1f, .75f));
            Button(panel.transform, "TURN HANDLE", new Vector2(-135, -315), new Vector2(245, 46), () => TryOpenHandle());
            Button(panel.transform, "LEAVE [F]", new Vector2(145, -315), new Vector2(215, 46), EndInteraction);
            Label(root.transform, "A / D rotate   •   ENTER confirm   •   E eject", new Vector2(0, -405), new Vector2(1000, 44), 24, Color.white);
        }

        private static void Mark(Transform parent, string text, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            Label(parent, text, new Vector2(Mathf.Sin(a) * 111, Mathf.Cos(a) * 111), new Vector2(105, 28), 15, new Color(.6f, .94f, 1f));
        }
        private static Image ImageNode(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var image = node.GetComponent<Image>(); image.color = color; return image;
        }
        private static Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int font, Color color)
        {
            var node = new GameObject("Label", typeof(RectTransform), typeof(Text)); node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = pos; rect.sizeDelta = size;
            var label = node.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = font; label.alignment = TextAnchor.MiddleCenter; label.text = text; label.color = color; label.raycastTarget = false; return label;
        }
        private static void Button(Transform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var image = ImageNode(parent, text, pos, size, new Color(.15f, .25f, .32f)); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action); Label(image.transform, text, Vector2.zero, size, 22, Color.white);
        }
    }
}
