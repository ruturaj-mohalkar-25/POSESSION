using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Possession
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private TutorialGuide guide;
        private Canvas canvas;
        private Text heading;
        private Text instruction;
        private Text progress;
        private Text keys;
        private TutorialPopIn lessonPop;
        private GameObject modal;
        private Text modalTitle;
        private Text modalBody;
        private Button primary;
        private Button replay;
        private Button menu;
        private TutorialLesson previous = (TutorialLesson)(-1);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
        private static readonly Color Dark = new Color(0.035f, 0.055f, 0.09f, 0.95f);

        public void Initialize(TutorialGuide tutorial)
        {
            guide = tutorial;
            var root = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var badge = Panel(root.transform, "PracticeBadge", new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(520f, 50f), Dark);
            progress = Label(badge, "PracticeProgress", 24, new Vector2(500f, 44f), Vector2.zero, Gold);
            var nav = Rect(root.transform, "Navigation", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(560f, 44f));
            Button(nav, "RestartPractice", "RESTART", new Vector2(-130f, 0f), new Vector2(240f, 42f), guide.Replay, Dark);
            Button(nav, "ReturnToMenu", "MAIN MENU", new Vector2(130f, 0f), new Vector2(240f, 42f), guide.MainMenu, Dark);

            var card = Panel(root.transform, "LessonCard", new Vector2(0.5f, 0f), new Vector2(0f, 135f), new Vector2(1100f, 220f), Dark);
            lessonPop = card.gameObject.AddComponent<TutorialPopIn>();
            heading = Label(card, "LessonTitle", 32, new Vector2(1050f, 44f), new Vector2(0f, 70f), Gold);
            instruction = Label(card, "LessonInstruction", 27, new Vector2(1020f, 100f), new Vector2(0f, -2f), Color.white);
            instruction.fontStyle = FontStyle.Normal;
            keys = Label(card, "MovementKeys", 24, new Vector2(1040f, 35f), new Vector2(0f, -82f), Color.white);

            modal = Panel(root.transform, "PracticeModal", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(1920f, 1080f), new Color(0.015f, 0.025f, 0.045f, 0.9f)).gameObject;
            var cover = modal.GetComponent<RectTransform>();
            cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one;
            cover.offsetMin = cover.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().raycastTarget = true;
            var frame = Panel(modal.transform, "WelcomeCard", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 650f), Dark);
            frame.gameObject.AddComponent<TutorialPopIn>();
            Label(frame, "Eyebrow", 25, new Vector2(1000f, 40f), new Vector2(0f, 250f), Gold).text = "POSSESSION / HOW TO PLAY";
            modalTitle = Label(frame, "Title", 54, new Vector2(1040f, 90f), new Vector2(0f, 165f), Color.white);
            modalBody = Label(frame, "Body", 30, new Vector2(960f, 220f), new Vector2(0f, 10f), new Color(0.85f, 0.9f, 0.95f));
            modalBody.fontStyle = FontStyle.Normal;
            primary = Button(frame, "StartPractice", "START PRACTICE", new Vector2(0f, -150f), new Vector2(420f, 70f), Primary, Gold);
            replay = Button(frame, "ReplayPractice", "REPLAY PRACTICE", new Vector2(-220f, -245f), new Vector2(390f, 60f), guide.Replay, new Color(0.2f, 0.27f, 0.36f));
            menu = Button(frame, "MainMenu", "MAIN MENU", new Vector2(220f, -245f), new Vector2(390f, 60f), guide.MainMenu, new Color(0.2f, 0.27f, 0.36f));
            Refresh();
        }

        private void LateUpdate() { if (guide != null) Refresh(); }
        private void Primary()
        {
            if (guide.Progress.Completed) guide.PlayLevelOne();
            else if (guide.Progress.Failed) guide.Replay();
            else guide.BeginPractice();
        }

        private void Refresh()
        {
            var state = guide.Progress;
            string form = state.Form == PlayerState.Possessed_Guard ? "GUARD"
                : state.Form == PlayerState.Possessed_Heavy ? "WALL BREAKER" : "SPIRIT";
            progress.text = state.Completed ? "HOW TO PLAY  ·  PRACTICE COMPLETE"
                : "HOW TO PLAY  ·  " + form + "  ·  " + state.ClearedSkills + "/9";
            heading.text = guide.Title;
            heading.color = state.Lesson == TutorialLesson.EscapeHostile ? new Color(1f, 0.35f, 0.3f) : Gold;
            instruction.text = guide.Instruction;
            keys.text = state.Lesson == TutorialLesson.Movement
                ? Key("W", TutorialMovement.Forward) + "   " + Key("A", TutorialMovement.Left) + "   "
                    + Key("S", TutorialMovement.Back) + "   " + Key("D", TutorialMovement.Right) + "   " + Key("SPACE", TutorialMovement.Jump)
                : state.Form == PlayerState.Possessed_Heavy ? "E  eject     ·     WASD  face wall     ·     LMB  smash"
                : state.Form == PlayerState.Possessed_Guard ? "E  eject     ·     MOUSE  aim     ·     LMB  shoot"
                : "WASD  move     ·     E  possess";
            bool show = state.Lesson == TutorialLesson.Welcome || state.Completed || state.Failed;
            modal.SetActive(show);
            if (state.Lesson != previous)
            {
                previous = state.Lesson;
                lessonPop.Play();
                if (show)
                {
                    bool failed = state.Failed;
                    modalTitle.text = state.Completed ? "YOU'RE READY." : failed ? "TRY AGAIN." : "BORROW A BODY. FIND THE EXIT.";
                    modalBody.text = state.Completed
                        ? "You opened the door, broke the wall, and reached the exit as the Spirit.\n\nIn every level: use each body's ability and finish before the clock reaches zero. Eject early to avoid a hostile enemy."
                        : failed ? "The level clock ran out, or the Spirit lost its health.\n\nEject early to avoid hostility. If you are forced out, move away from the red enemy. Restart whenever you need."
                        : "Borrow the Guard to shoot. Borrow the Wall Breaker to smash.\n\nFollow the key prompts, then reach the green exit as the Spirit.";
                    primary.GetComponentInChildren<Text>().text = state.Completed ? "PLAY LEVEL 01" : failed ? "RESTART PRACTICE" : "START PRACTICE";
                    replay.gameObject.SetActive(state.Completed);
                    (menu.transform as RectTransform).anchoredPosition = new Vector2(state.Completed ? 220f : 0f, -245f);
                }
            }
        }

        private string Key(string key, TutorialMovement direction) => (guide.Progress.Movement & direction) != 0
            ? "<color=#70E8A0>[" + key + "] DONE</color>" : "[" + key + "]";

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
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityAction clicked, Color color)
        {
            var rect = Panel(parent, name, new Vector2(0.5f, 0.5f), position, size, color);
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(clicked);
            Label(rect, "Text", 26, size - new Vector2(20f, 8f), Vector2.zero,
                color == Gold ? new Color(0.1f, 0.13f, 0.18f) : Color.white).text = label;
            return button;
        }
    }
}
