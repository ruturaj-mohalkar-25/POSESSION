using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Possession
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private static readonly Color Gold = new(1f, 0.78f, 0.22f);
        private static readonly Color Green = new(0.35f, 1f, 0.55f);
        private static readonly Color Red = new(1f, 0.28f, 0.24f);
        private static readonly Color Dark = new(0.02f, 0.035f, 0.06f, 0.96f);
        private static readonly Color DarkOverlay = new(0.015f, 0.025f, 0.045f, 0.88f);
        private static readonly Color Border = new(0.15f, 0.22f, 0.32f, 1f);

        private TutorialGuide guide;

        // Hint card: slides in at the right edge, never pauses, and shrinks a bar as its time runs out
        private const float HintWidth = 460f;
        private const float BubbleY = 78f;
        private static readonly Color BubbleColor = new(0.02f, 0.035f, 0.06f, 0.6f);
        private Image hintBackground;
        private RectTransform hintCard;
        private CanvasGroup hintGroup;
        private Text dialogTitle;
        private Text dialogBody;
        private RectTransform hintTimer;
        private float hintVisible;

        // Field guide: an always-on key at the left edge naming each character and pickup
        private readonly System.Collections.Generic.Dictionary<TutorialIcons.Kind, Image> guideRows = new();
        private RectTransform fieldGuide;
        private Canvas canvas;
        private Bounds levelBounds;
        private bool hasLevelBounds;
        private float guideX = float.NaN;

        // "YOU (SPIRIT)" marker that floats over whatever body the player controls
        private RectTransform youMarker;
        private RectTransform youArrow;
        private Text youLabel;

        private Text controlsText;

        private GameObject result;
        private Text resultTitle;
        private Text resultBody;
        private Button play;
        private Button menu;

        private TutorialGuide.PopupState lastPopup;

        public void Initialize(TutorialGuide tutorial)
        {
            guide = tutorial;
            var root = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            this.canvas = canvas;
            FindLevelBounds();
            BuildYouMarker(root.transform);
            BuildFieldGuide(root.transform);
            BuildHintCard(youMarker);
            BuildResultPanel(root.transform);
            Refresh();
        }

        // Hint bubble: rides above the player's body (over the "YOU" label), see-through so the
        // Guard, cones and room stay visible behind it, and never takes clicks or pauses play
        private void BuildHintCard(Transform marker)
        {
            hintCard = Panel(marker, "HintCard", new Vector2(0.5f, 0f), new Vector2(0f, BubbleY),
                new Vector2(HintWidth, 100f), BubbleColor);
            hintBackground = hintCard.GetComponent<Image>();
            hintCard.pivot = new Vector2(0.5f, 0f);
            hintGroup = hintCard.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;

            dialogTitle = Label(hintCard, "HintTitle", 22, new Vector2(HintWidth - 24f, 30f), Vector2.zero, Gold);
            AddOutline(dialogTitle);
            dialogBody = Label(hintCard, "HintBody", 26, new Vector2(HintWidth - 24f, 60f), Vector2.zero, Color.white);
            dialogBody.fontStyle = FontStyle.Bold;
            dialogBody.alignment = TextAnchor.MiddleCenter;
            dialogBody.lineSpacing = 1.1f;
            AddOutline(dialogBody);

            hintTimer = Panel(hintCard, "HintTimer", new Vector2(0f, 0f), new Vector2(0f, 2f),
                new Vector2(HintWidth, 3f), new Color(Gold.r, Gold.g, Gold.b, 0.8f));
            hintTimer.pivot = new Vector2(0f, 0.5f);
            hintGroup.alpha = 0f;
        }

        private static void AddOutline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void FindLevelBounds()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name != "CompactTutorialLayout" && t.name != "P4_LevelLayout") continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>())
                {
                    if (!hasLevelBounds) { levelBounds = r.bounds; hasLevelBounds = true; }
                    else levelBounds.Encapsulate(r.bounds);
                }
            }
        }

        private void BuildYouMarker(Transform root)
        {
            youMarker = Rect(root, "YouMarker", Vector2.zero, Vector2.zero, new Vector2(200f, 70f));
            youMarker.pivot = new Vector2(0.5f, 0f);
            var group = youMarker.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            youArrow = Rect(youMarker, "Arrow", new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(26f, 22f));
            var arrow = youArrow;
            var arrowImage = arrow.gameObject.AddComponent<Image>();
            arrowImage.sprite = TutorialIcons.ArrowDown;
            arrowImage.color = Gold;
            arrowImage.raycastTarget = false;

            youLabel = Label(youMarker, "YouLabel", 26, new Vector2(200f, 30f), new Vector2(0f, 4f), Gold);
            youLabel.rectTransform.anchorMin = youLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            youLabel.rectTransform.anchoredPosition = new Vector2(0f, 42f);
            var shadow = youLabel.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // Controls sit right above the player instead of in a corner nobody looks at
            controlsText = Label(youMarker, "ControlsLine", 17, new Vector2(HintWidth, 26f), Vector2.zero,
                new Color(0.9f, 0.93f, 0.97f));
            controlsText.rectTransform.anchorMin = controlsText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            controlsText.rectTransform.anchoredPosition = new Vector2(0f, 72f);
            AddOutline(controlsText);
        }

        // Keep the "YOU" arrow over the controlled body, and the Field Guide just left of the level
        private void PlaceWorldAnchoredUI()
        {
            var cam = Camera.main;
            if (cam == null || canvas == null) return;
            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            bool ended = guide.Progress.Completed || guide.Progress.Failed;

            Transform body = null;
            string who = "YOU (SPIRIT)";
            var state = guide.Possession != null ? guide.Possession.CurrentState : PlayerState.Spirit;
            if (state == PlayerState.Possessed_Guard && guide.Guard != null) { body = guide.Guard.transform; who = "YOU (GUARD)"; }
            else if (state == PlayerState.Possessed_Heavy && guide.Heavy != null) { body = guide.Heavy.transform; who = "YOU (HEAVY)"; }
            else if (guide.Spirit != null) body = guide.Spirit.transform;

            bool showMarker = body != null && !ended;
            if (showMarker)
            {
                var col = body.GetComponentInChildren<Collider>();
                float top = col != null && col.enabled ? col.bounds.max.y : body.position.y + 1f;
                Vector3 screen = cam.WorldToScreenPoint(new Vector3(body.position.x, top + 0.4f, body.position.z));
                showMarker = screen.z > 0f;
                float bob = Mathf.Sin(Time.unscaledTime * 5f) * 5f;
                float half = HintWidth * 0.5f + 12f;
                float x = Mathf.Clamp(screen.x / scale, half, Mathf.Max(half, Screen.width / scale - half));
                youMarker.anchoredPosition = new Vector2(x, screen.y / scale);
                youArrow.anchoredPosition = new Vector2(screen.x / scale - x, 12f + bob);
                youLabel.text = who;
            }
            youMarker.gameObject.SetActive(showMarker);

            if (fieldGuide == null) return;
            float width = fieldGuide.sizeDelta.x;
            float targetX = 24f;
            if (hasLevelBounds && body != null)
            {
                // The level's left edge at the player's depth, projected to the screen
                Vector3 edge = cam.WorldToScreenPoint(new Vector3(levelBounds.min.x, levelBounds.max.y, body.position.z));
                if (edge.z > 0f) targetX = edge.x / scale - width - 28f;
            }
            float maxX = Screen.width / scale - width - 24f;
            targetX = Mathf.Clamp(targetX, 16f, Mathf.Max(16f, maxX));
            float follow = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            guideX = float.IsNaN(guideX) ? targetX : Mathf.Lerp(guideX, targetX, follow);
            fieldGuide.anchoredPosition = new Vector2(guideX, -24f);

        }

        private void BuildFieldGuide(Transform root)
        {
            var rows = new (TutorialIcons.Kind kind, string name, string info)[]
            {
                (TutorialIcons.Kind.Spirit, "SPIRIT", "You. Can't attack."),
                (TutorialIcons.Kind.Guard, "GUARD", "Possess to shoot targets"),
                (TutorialIcons.Kind.Heavy, "HEAVY", "Possess to smash walls"),
                (TutorialIcons.Kind.VisionCone, "VISION CONE", "Danger zone: it attacks"),
                (TutorialIcons.Kind.PossessionRing, "POSSESSION", "Ring = time left in a body"),
                (TutorialIcons.Kind.SoulOrb, "SOUL ORB", "+1 possession charge"),
                (TutorialIcons.Kind.HealthOrb, "HEALTH ORB", "Restores HP"),
            };
            const float rowHeight = 68f, width = 360f;
            float height = 54f + rows.Length * rowHeight + 10f;
            // Top-left; its x is moved every frame so it sits just outside the level's left edge
            var panel = Panel(root, "FieldGuide", new Vector2(0f, 1f), new Vector2(24f, -24f),
                new Vector2(width, height), new Color(0.02f, 0.035f, 0.06f, 0.5f));
            panel.pivot = new Vector2(0f, 1f);
            fieldGuide = panel;

            var header = Label(panel, "Header", 26, new Vector2(width - 32f, 36f),
                new Vector2(0f, height * 0.5f - 28f), Gold);
            header.text = "FIELD GUIDE";
            header.alignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < rows.Length; i++)
            {
                float y = height * 0.5f - 54f - rowHeight * (i + 0.5f);
                var row = Panel(panel, "Row_" + rows[i].name, new Vector2(0.5f, 0.5f), new Vector2(0f, y),
                    new Vector2(width - 14f, rowHeight - 4f), new Color(1f, 1f, 1f, 0f));
                guideRows[rows[i].kind] = row.GetComponent<Image>();

                var icon = Rect(row, "Icon", new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(44f, 44f));
                icon.localScale = Vector3.one * 1.25f;
                TutorialIcons.Build(icon, rows[i].kind);

                var name = Label(row, "Name", 22, new Vector2(width - 96f, 28f), new Vector2(34f, 12f), Color.white);
                name.text = rows[i].name;
                name.alignment = TextAnchor.MiddleLeft;
                var info = Label(row, "Info", 18, new Vector2(width - 96f, 24f), new Vector2(34f, -14f),
                    new Color(0.7f, 0.76f, 0.84f));
                info.text = rows[i].info;
                info.fontStyle = FontStyle.Normal;
                info.alignment = TextAnchor.MiddleLeft;
            }
        }

        private void BuildResultPanel(Transform root)
        {
            result = Panel(root, "Result", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(780f, 300f), Dark).gameObject;
            resultTitle = Label(result.transform, "ResultTitle", 44, new Vector2(720f, 70f),
                new Vector2(0f, 72f), Color.white);
            // Shrink long titles to fit instead of cutting them off
            resultTitle.resizeTextForBestFit = true;
            resultTitle.resizeTextMinSize = 24;
            resultTitle.resizeTextMaxSize = 44;
            resultBody = Label(result.transform, "ResultBody", 26, new Vector2(700f, 70f),
                new Vector2(0f, 8f), Color.white);
            play = Button(result.transform, "PlayLevelOne", "PLAY LEVEL 1", new Vector2(-175f, -92f),
                new Vector2(320f, 62f), guide.PlayLevelOne, Gold);
            menu = Button(result.transform, "MainMenu", "MAIN MENU", new Vector2(175f, -92f),
                new Vector2(260f, 62f), guide.MainMenu, new Color(0.18f, 0.24f, 0.32f));
            result.SetActive(false);
        }

        private void LateUpdate()
        {
            if (guide != null) Refresh();
        }

        private void Refresh()
        {
            RefreshDialog();
            PlaceWorldAnchoredUI();
            RefreshControls();
            RefreshResult();
            RefreshGains();
        }

        // ---- Pickup feedback: a small "+1 SOUL" / "+2 HP" that floats up from the player and fades,
        // and the matching Field Guide row flashes. No pop-up, no pause, gone in about a second. ----
        private const float GainSeconds = 1.3f;
        private int lastCharges = -1, lastHP = -1;
        private TutorialIcons.Kind flashKind;
        private float flashUntil;
        private readonly System.Collections.Generic.List<(RectTransform rect, CanvasGroup group, float born)> gains = new();

        private void RefreshGains()
        {
            int charges = guide.Charges;
            int hp = guide.Spirit != null ? guide.Spirit.CurrentHP : 0;
            bool playing = guide.Progress.Started && !guide.Progress.Completed && !guide.Progress.Failed;
            if (playing && lastCharges >= 0 && charges > lastCharges)
                SpawnGain("+" + (charges - lastCharges) + " POSSESSION", new Color(0.55f, 0.85f, 1f), TutorialIcons.Kind.SoulOrb);
            if (playing && lastHP >= 0 && hp > lastHP)
                SpawnGain("+" + (hp - lastHP), new Color(0.4f, 1f, 0.55f), TutorialIcons.Kind.HealthOrb);
            lastCharges = charges;
            lastHP = hp;

            for (int i = gains.Count - 1; i >= 0; i--)
            {
                var g = gains[i];
                float t = (Time.unscaledTime - g.born) / GainSeconds;
                if (t >= 1f || g.rect == null)
                {
                    if (g.rect != null) Destroy(g.rect.gameObject);
                    gains.RemoveAt(i);
                    continue;
                }
                // Pop in, drift upward beside the player, fade out
                g.rect.anchoredPosition = new Vector2(HintWidth * 0.5f + 80f, 6f + t * 36f);
                g.rect.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(1.4f, 1f, t / 0.15f) : 1f);
                g.group.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            }
        }

        private void SpawnGain(string text, Color color, TutorialIcons.Kind kind)
        {
            var rect = Rect(youMarker, "Gain", new Vector2(0f, 0f), Vector2.zero, new Vector2(260f, 44f));
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var icon = Rect(rect, "Icon", new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(44f, 44f));
            icon.localScale = Vector3.one * 0.8f;
            TutorialIcons.Build(icon, kind);
            var label = Label(rect, "Text", 28, new Vector2(210f, 40f), new Vector2(26f, 0f), color);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = text;
            AddOutline(label);
            // Sits just right of the player so it never covers the bubble or the body
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            gains.Add((rect, group, Time.unscaledTime));
            flashKind = kind;
            flashUntil = Time.unscaledTime + GainSeconds;
        }

        // The bubble above the player is the tutorial's only instruction: the current step in gold,
        // plus a short tip underneath while a hint is active. The bottom objective bar is hidden.
        private string lastBubbleText;

        private void RefreshDialog()
        {
            var current = guide.ActivePopup;
            if (current != lastPopup && current != TutorialGuide.PopupState.None) hintVisible = 0.4f; // small pop for a new tip
            lastPopup = current;

            string tip = "WASD - move  |  SPACE - jump";
            // While inside any body, always remind how to get out
            bool possessing = guide.Possession != null && guide.Possession.CurrentState != PlayerState.Spirit;
            bool timedTip = current != TutorialGuide.PopupState.None && !possessing;
            if (possessing) tip = "<color=#FFD060>E</color> - eject early\nEmpty ring = forced eject";
            string step = guide.Title;
            bool danger = guide.UnderAttack;
            if (danger) tip = "Leave the <color=#FF6B6B>red zone</color>! SPACE - jump";
            string text = (danger ? "<color=#FF5555>DANGER - KEEP MOVING</color>" : "<color=#FFD060>" + step + "</color>")
                + (tip != null ? "\n<size=21><b>" + tip + "</b></size>" : "");
            if (text != lastBubbleText)
            {
                lastBubbleText = text;
                dialogTitle.text = "";
                dialogBody.text = text;
                int lines = 1 + (tip == null ? 0 : tip.Split('\n').Length);
                float h = 22f + 32f * lines;
                hintCard.sizeDelta = new Vector2(HintWidth, h);
                dialogBody.rectTransform.sizeDelta = new Vector2(HintWidth - 24f, h - 10f);
                dialogBody.rectTransform.anchoredPosition = Vector2.zero;
            }

            bool show = guide.Progress.Started && !guide.Progress.Completed && !guide.Progress.Failed;
            hintVisible = Mathf.MoveTowards(hintVisible, show ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            float ease = 1f - (1f - hintVisible) * (1f - hintVisible);
            hintGroup.alpha = ease;
            hintCard.anchoredPosition = new Vector2(0f, BubbleY - (1f - ease) * 14f);
            // The thin bar shows how long the tip line stays; no bar when only the step is shown
            hintTimer.sizeDelta = new Vector2(timedTip && !danger ? HintWidth * guide.HintRemaining01 : 0f, 3f);
            // Pulse the bubble red while under attack so the warning is impossible to miss
            float pulse = danger ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f) : 0f;
            hintBackground.color = Color.Lerp(BubbleColor, new Color(0.55f, 0.06f, 0.06f, 0.8f), pulse);

            RefreshGuideHighlights(current);
        }

        // Light up the Field Guide rows that the current hint or action is about
        private void RefreshGuideHighlights(TutorialGuide.PopupState hint)
        {
            var state = guide.Possession != null ? guide.Possession.CurrentState : PlayerState.Spirit;
            foreach (var pair in guideRows)
            {
                bool on = pair.Key switch
                {
                    TutorialIcons.Kind.Spirit => state == PlayerState.Spirit && Time.timeSinceLevelLoad < 8f,
                    TutorialIcons.Kind.Guard => hint == TutorialGuide.PopupState.Guard || state == PlayerState.Possessed_Guard,
                    TutorialIcons.Kind.Heavy => hint == TutorialGuide.PopupState.Heavy || state == PlayerState.Possessed_Heavy,
                    TutorialIcons.Kind.VisionCone => hint == TutorialGuide.PopupState.Guard,
                    TutorialIcons.Kind.PossessionRing => hint == TutorialGuide.PopupState.Possessing,
                    TutorialIcons.Kind.SoulOrb => guide.Charges == 0 && state == PlayerState.Spirit,
                    TutorialIcons.Kind.HealthOrb => guide.Spirit != null && guide.Spirit.CurrentHP < guide.Spirit.MaxHP,
                    _ => false
                };
                bool flash = pair.Key == flashKind && Time.unscaledTime < flashUntil;
                float target = flash ? 0.5f : on ? 0.16f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
                var c = pair.Value.color;
                pair.Value.color = new Color(Gold.r, Gold.g, Gold.b,
                    flash ? target : Mathf.MoveTowards(c.a, target, Time.unscaledDeltaTime));
            }
        }

        // No separate controls line: the objective bar already names the key to press
        private void RefreshControls() => controlsText.gameObject.SetActive(false);

        private void RefreshResult()
        {
            bool failed = guide.Progress.Failed;
            bool complete = guide.Progress.Completed;
            result.SetActive(failed || complete);
            if (!result.activeSelf) return;
            resultTitle.text = failed ? guide.FailureMessage : "YOU'RE READY";
            resultTitle.color = failed ? Red : Green;
            resultBody.text = failed ? "Restarting..." : "";
            play.gameObject.SetActive(complete);
            menu.gameObject.SetActive(complete);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Text Label(Transform parent, string name, int fontSize, Vector2 size, Vector2 position, Color color)
        {
            var rect = Rect(parent, name, new Vector2(0.5f, 0.5f), position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size,
            UnityAction clicked, Color color)
        {
            var rect = Panel(parent, name, new Vector2(0.5f, 0.5f), position, size, color);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(clicked);
            Label(rect, "Text", 22, size - new Vector2(16f, 6f), Vector2.zero,
                new Color(0.08f, 0.1f, 0.14f)).text = label;
            return button;
        }
    }
}
