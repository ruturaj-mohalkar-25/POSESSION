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
        private static readonly Color Dark = new(0.025f, 0.04f, 0.07f, 0.94f);

        private TutorialGuide guide;
        private GameObject result;
        private Text resultTitle;
        private Text resultBody;
        private Button play;
        private Button menu;

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

            result = Panel(root.transform, "Result", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(780f, 300f), Dark).gameObject;
            resultTitle = Label(result.transform, "ResultTitle", 44, new Vector2(720f, 70f), new Vector2(0f, 72f), Color.white);
            resultBody = Label(result.transform, "ResultBody", 26, new Vector2(700f, 70f), new Vector2(0f, 8f), Color.white);
            play = Button(result.transform, "PlayLevelOne", "PLAY LEVEL 1", new Vector2(-175f, -92f),
                new Vector2(320f, 62f), guide.PlayLevelOne, Gold);
            menu = Button(result.transform, "MainMenu", "MAIN MENU", new Vector2(175f, -92f),
                new Vector2(260f, 62f), guide.MainMenu, new Color(0.18f, 0.24f, 0.32f));
            result.SetActive(false);
            Refresh();
        }

        private void LateUpdate()
        {
            if (guide != null) Refresh();
        }

        private void Refresh()
        {
            bool failed = guide.Progress.Failed;
            bool complete = guide.Progress.Completed;
            result.SetActive(failed || complete);
            if (!result.activeSelf) return;
            resultTitle.text = failed ? guide.FailureMessage : "YOU'RE READY";
            resultTitle.color = failed ? Red : Green;
            resultBody.text = failed ? "Restarting…" : "Guard. Soul. Heavy. Exit.";
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
            Label(rect, "Text", 24, size - new Vector2(20f, 8f), Vector2.zero,
                color == Gold ? new Color(0.08f, 0.1f, 0.14f) : Color.white).text = label;
            return button;
        }
    }
}
