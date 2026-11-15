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
        private Text context, roomTitle, cipherNote;
        private Camera cameraView;
        private CameraFollow follow;
        private Quaternion cameraRotation;
        private bool surveying, failed;
        public bool EngineerLost => failed;

        private void Start()
        {
            player = FindFirstObjectByType<PossessionController>();
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
                hud.SetLevelObjective("NO ENGINEER LEFT • RETRY THE LEVEL");
                if (failureTitle != null) failureTitle.text = "NO ENGINEER LEFT";
                EventBus.LevelFailed?.Invoke();
                return;
            }
            if (player == null) return;
            Transform body = player.PossessedEnemy != null ? player.PossessedEnemy.transform : player.transform;
            Vector3 p = body.position;
            if (roomTitle != null) roomTitle.text = "LEVEL 4 / THE SPLIT VAULT\n" + (p.y > 3.5f ? "UPPER FLOOR" : "LOWER FLOOR");
            bool recovered = cipher != null && cipher.Recovered;
            if (cipherNote != null) cipherNote.text = recovered ? "VAULT CIPHER\n" + Level4Cipher.Hint : "";
            if (hud != null) hud.SetLevelObjective(
                !recovered ? galleryDoor != null && !galleryDoor.IsOpen
                    ? "GUARD • OPEN THE GALLERY TO REACH THE ARCHIVE"
                    : "UPSTAIRS • ENGINEER • UNLOCK THE ARCHIVE"
                : accessGate.IsOpen ? "BRING ENGINEER • OPEN THE VAULT" : "CIPHER RECOVERED • OPEN SERVICE ACCESS");
            if (context == null) return;
            context.text = "";
            var plan = new Vector3(p.x / Level4Layout.GeometryScale, p.y, p.z / Level4Layout.GeometryScale);
            bool overlook = plan.y > 4f && plan.x >= 38f && plan.x <= 49f && plan.z >= 34f && plan.z <= 46f;
            bool nearCipher = cipher != null && !recovered && cipher.RecoveryPoint != null
                && Vector3.Distance(p, cipher.RecoveryPoint.position) < 4f;
            if (nearCipher)
            {
                StopSurvey();
                context.text = cipher.CanRecover(body) ? "F • RECOVER VAULT CIPHER" : "ENGINEER • RECOVER THE CIPHER";
                if (Input.GetKeyDown(KeyCode.F)) cipher.TryRecover(body);
            }
            else if (overlook)
            {
                context.text = "BRIDGE OUT • HOLD F TO SURVEY BELOW";
                if (Input.GetKey(KeyCode.F) && !vault.IsInteracting) Survey(); else StopSurvey();
            }
            else StopSurvey();
            if (securityDoors != null)
                foreach (var gate in securityDoors)
                {
                    if (gate == null || gate.IsOpen) continue;
                    if (gate.CanUnlock(body))
                    {
                        context.text = "F • ENGINEER • OPEN SECURITY DOOR";
                        if (Input.GetKeyDown(KeyCode.F)) gate.TryUnlock(body);
                        break;
                    }
                    if (gate.CanRelease(body))
                    {
                        context.text = "F • OPEN RETURN DOOR";
                        if (Input.GetKeyDown(KeyCode.F)) gate.TryRelease(body);
                        break;
                    }
                    if (gate.EngineerPoint != null && Vector3.Distance(p, gate.EngineerPoint.position) < 3.5f)
                        context.text = "SECURITY DOOR • ENGINEER REQUIRED";
                }
            if (accessGate.CanRelease(body))
            {
                context.text = "F • RELEASE SERVICE ACCESS";
                if (Input.GetKeyDown(KeyCode.F)) accessGate.TryRelease(body);
            }
            if (!vault.IsOpen && vault.OperatorPoint != null && Vector3.Distance(p, vault.OperatorPoint.position) < 4f)
                context.text = !vault.HasCipher ? "VAULT • RECOVER THE ARCHIVE CIPHER FIRST"
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
        private void OnDisable() => StopSurvey();
        private void BuildLabels()
        {
            var root = new GameObject("Level 4 context", typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            roomTitle = Label(root.transform, new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(460f, 66f), 23, TextAnchor.UpperLeft);
            context = Label(root.transform, new Vector2(.5f, 0f), new Vector2(0f, 170f), new Vector2(1100, 46), 25, TextAnchor.MiddleCenter);
            cipherNote = Label(root.transform, new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(450, 78), 20, TextAnchor.UpperRight);
        }
        private static Text Label(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int font, TextAnchor alignment)
        {
            var go = new GameObject("Context", typeof(RectTransform), typeof(Text), typeof(Outline)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = font; text.alignment = alignment;
            text.color = new Color(.9f, .96f, 1f); text.raycastTarget = false; go.GetComponent<Outline>().effectColor = Color.black; return text;
        }
    }
}
