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

        private GameObject dialogRoot;
        private RectTransform dialogCard;
        private Image dialogBorder;
        private Text dialogTitle;
        private Text dialogBody;
        private RectTransform tail;

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

            BuildDialog(root.transform);
            BuildControlsLegend(root.transform);
            BuildResultPanel(root.transform);
            Refresh();
        }

        private void BuildDialog(Transform root)
        {
            dialogRoot = new GameObject("DialogRoot");
            var rootRect = dialogRoot.AddComponent<RectTransform>();
            rootRect.SetParent(root, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.3f, 0.4f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(520f, 220f);

            var borderRect = Panel(rootRect, "DialogBorder", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(524f, 224f), Border);
            dialogBorder = borderRect.GetComponent<Image>();

            dialogCard = Panel(rootRect, "DialogCard", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(516f, 216f), Dark);

            tail = Panel(rootRect, "DialogTail", new Vector2(0.5f, 0f), new Vector2(0f, -12f),
                new Vector2(22f, 14f), Border);

            dialogTitle = Label(dialogCard, "DialogTitle", 30, new Vector2(490f, 40f),
                new Vector2(0f, 76f), Gold);

            dialogBody = Label(dialogCard, "DialogBody", 22, new Vector2(480f, 140f),
                new Vector2(0f, -6f), new Color(0.85f, 0.9f, 0.95f));
            dialogBody.fontStyle = FontStyle.Normal;
            dialogBody.alignment = TextAnchor.UpperCenter;

            var btn = Button(rootRect, "ContinueBtn", "GOT IT", new Vector2(0f, -126f),
                new Vector2(160f, 36f), guide.DismissPopup, Gold);

            dialogRoot.SetActive(false);
        }

        private void BuildControlsLegend(Transform root)
        {
            var legend = Panel(root, "ControlsLegend", new Vector2(1f, 0f), new Vector2(-130f, 80f),
                new Vector2(230f, 110f), new Color(0.02f, 0.035f, 0.06f, 0.85f));

            var header = Label(legend, "ControlsHeader", 20, new Vector2(210f, 28f),
                new Vector2(0f, 38f), Gold);
            header.text = "CONTROLS";

            controlsText = Label(legend, "ControlsText", 22, new Vector2(210f, 70f),
                new Vector2(0f, -10f), new Color(0.82f, 0.86f, 0.92f));
            controlsText.fontStyle = FontStyle.Normal;
            controlsText.alignment = TextAnchor.MiddleLeft;
        }

        private void BuildResultPanel(Transform root)
        {
            result = Panel(root, "Result", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(780f, 300f), Dark).gameObject;
            resultTitle = Label(result.transform, "ResultTitle", 44, new Vector2(720f, 70f),
                new Vector2(0f, 72f), Color.white);
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
            RefreshControls();
            RefreshResult();
        }

        private void RefreshDialog()
        {
            var current = guide.ActivePopup;
            if (current == TutorialGuide.PopupState.None)
            {
                dialogRoot.SetActive(false);
                lastPopup = current;
                return;
            }

            if (current != lastPopup)
            {
                lastPopup = current;
                dialogRoot.SetActive(true);

                var rootRect = dialogRoot.GetComponent<RectTransform>();

                float h = 220f;
                switch (current)
                {
                    case TutorialGuide.PopupState.SpiritIntro:
                        h = 220f;
                        dialogTitle.text = "YOU ARE THE SPIRIT";
                        dialogBody.text =
                            "Move with <color=#FFD060>WASD</color> to explore the level.\n" +
                            "Press <color=#FFD060>E</color> near an enemy to <color=#FFD060>POSSESS</color> it.\n" +
                            "Each possession costs <color=#66DDFF>1 charge</color>.\n\n" +
                            "<color=#66DDFF>White Soul Orb</color>  =  +1 Possession Charge\n" +
                            "<color=#70E8A0>Green Cross Orb</color>  =  Restores Health";
                        break;

                    case TutorialGuide.PopupState.HudIntro:
                        h = 240f;
                        dialogTitle.text = "KNOW YOUR HUD";
                        dialogBody.text =
                            "<color=#FF6B6B>Health Bar</color>  --  your HP. Enemies hurt you in Spirit form.\n" +
                            "<color=#FFD060>Level Timer</color>  --  reach the exit before it hits zero.\n" +
                            "<color=#FFD060>Objective</color>  --  your current goal, shown at the top.\n\n" +
                            "<color=#66DDFF>Possession Timer Ring</color>  --  appears while possessing.\n" +
                            "When it empties you are <color=#FF5555>forced out</color> and the\n" +
                            "enemy turns <color=#FF5555>hostile</color>. Press <color=#FFD060>E</color> to eject early!";
                        break;

                    case TutorialGuide.PopupState.Guard:
                        h = 220f;
                        dialogTitle.text = "THE GUARD";
                        dialogBody.text =
                            "<color=#FF5555>Red Cone</color>  =  enemy vision. Stay <color=#FFD060>behind</color> to possess safely.\n" +
                            "If caught in the cone, the enemy will <color=#FF5555>chase</color> you!\n\n" +
                            "Press <color=#FFD060>E</color> when the green prompt appears to possess.\n" +
                            "Aim with <color=#FFD060>Mouse</color>, press <color=#FFD060>LMB</color> to shoot.\n" +
                            "Hit the target to unlock the gold door.";
                        break;

                    case TutorialGuide.PopupState.Heavy:
                        h = 200f;
                        dialogTitle.text = "THE HEAVY";
                        dialogBody.text =
                            "The Heavy can <color=#FFD060>smash breakable walls</color>.\n\n" +
                            "Press <color=#FFD060>E</color> to possess the Heavy.\n" +
                            "Face the cracked wall, press <color=#FFD060>LMB</color> to smash.\n" +
                            "Then eject with <color=#FFD060>E</color> and reach the <color=#70E8A0>green exit</color>.";
                        break;
                }

                rootRect.sizeDelta = new Vector2(520f, h);
                dialogCard.sizeDelta = new Vector2(516f, h - 4f);
                dialogBorder.rectTransform.sizeDelta = new Vector2(524f, h + 4f);
                dialogTitle.rectTransform.anchoredPosition = new Vector2(0f, h * 0.5f - 26f);
                dialogBody.rectTransform.sizeDelta = new Vector2(480f, h - 60f);
                dialogBody.rectTransform.anchoredPosition = new Vector2(0f, -6f);
                if (tail != null) tail.anchoredPosition = new Vector2(0f, -h * 0.5f - 6f);
            }
        }

        private void RefreshControls()
        {
            bool showControls = guide.Progress.Started && !guide.Progress.Completed && !guide.Progress.Failed
                && guide.ActivePopup == TutorialGuide.PopupState.None;
            controlsText.transform.parent.gameObject.SetActive(showControls);
            if (showControls)
                controlsText.text = guide.ControlsLegend;
        }

        private void RefreshResult()
        {
            bool failed = guide.Progress.Failed;
            bool complete = guide.Progress.Completed;
            result.SetActive(failed || complete);
            if (!result.activeSelf) return;
            resultTitle.text = failed ? guide.FailureMessage : "YOU'RE READY";
            resultTitle.color = failed ? Red : Green;
            resultBody.text = failed ? "Restarting..." : "Guard. Soul. Heavy. Exit.";
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
