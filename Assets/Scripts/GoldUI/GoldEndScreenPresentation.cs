using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession
{
    // Restyles existing end states and owns a runtime pause overlay. It deliberately
    // leaves the existing button order and callbacks intact.
    [DisallowMultipleComponent]
    public sealed class GoldEndScreenPresentation : MonoBehaviour
    {
        private const string PauseName = "Gold Pause Panel";
        private GameObject pausePanel;
        private bool pausedHere;

        public bool IsPaused => pausedHere;
        public GameObject PausePanel => pausePanel;

        private void Start()
        {
            Apply();
        }

        public void Apply()
        {
            StyleExistingPanel("LevelCompletePanel");
            StyleExistingPanel("GameOverPanel");
            StyleExistingPanel("GameCompletePanel");
            EnsurePausePanel();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.P)) return;
            if (pausedHere) Resume();
            else if (GameManager.Instance == null
                || GameManager.Instance.CurrentState == LevelState.InProgress) Pause();
        }

        public void Pause()
        {
            EnsurePausePanel();
            if (pausePanel == null || pausedHere) return;
            pausedHere = true;
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (!pausedHere) return;
            pausedHere = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void Restart()
        {
            pausedHere = false;
            Time.timeScale = 1f;
            LevelFlow.Load(gameObject.scene.name);
        }

        public void MainMenu()
        {
            pausedHere = false;
            Time.timeScale = 1f;
            LevelFlow.Load("MainMenu");
        }

        private void OnDisable()
        {
            if (pausedHere) Resume();
        }

        private void StyleExistingPanel(string panelName)
        {
            RectTransform panel = FindRect(panelName);
            if (panel == null) return;

            Stretch(panel);
            var background = panel.GetComponent<Image>();
            if (background != null) background.color = GoldPart2Theme.Background;

            RectTransform card = FindDirect(panel, "Gold End Card");
            if (card == null)
            {
                card = CreateRect(panel, "Gold End Card", new Vector2(.20f, .10f), new Vector2(.80f, .90f));
                var cardImage = card.gameObject.AddComponent<Image>();
                cardImage.color = GoldPart2Theme.Panel;
                cardImage.raycastTarget = false;
                card.SetAsFirstSibling();
            }

            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                bool title = text.name == "Title" || text.name.Contains("Title");
                GoldPart2Theme.StyleText(text, title ? 64 : Mathf.Max(22, text.fontSize), text.alignment);
                if (title) text.color = GoldPart2Theme.Teal;
            }

            var buttons = panel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                GoldPart2Theme.StyleButton(buttons[i], i == 0);
        }

        private void EnsurePausePanel()
        {
            if (pausePanel != null) return;
            RectTransform existing = FindRect(PauseName);
            if (existing != null)
            {
                pausePanel = existing.gameObject;
                return;
            }

            RectTransform overlay = CreateRect(transform, PauseName, Vector2.zero, Vector2.one);
            pausePanel = overlay.gameObject;
            var overlayImage = pausePanel.AddComponent<Image>();
            overlayImage.color = GoldPart2Theme.Background;

            RectTransform card = CreateRect(overlay, "Pause Card", new Vector2(.31f, .16f), new Vector2(.69f, .84f));
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = GoldPart2Theme.Panel;

            CreateText(card, "Title", "PAUSED", 64, new Vector2(.08f, .70f), new Vector2(.92f, .92f),
                GoldPart2Theme.Teal);
            CreateButton(card, "ResumeButton", "RESUME", new Vector2(.18f, .48f), new Vector2(.82f, .62f),
                Resume, true);
            CreateButton(card, "RestartButton", "RESTART", new Vector2(.18f, .30f), new Vector2(.82f, .44f),
                Restart, false);
            CreateButton(card, "MenuButton", "MAIN MENU", new Vector2(.18f, .12f), new Vector2(.82f, .26f),
                MainMenu, false);
            pausePanel.SetActive(false);
        }

        private RectTransform FindRect(string objectName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == objectName) return rect;
            return null;
        }

        private static RectTransform FindDirect(Transform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            return child != null ? child as RectTransform : null;
        }

        private static RectTransform CreateRect(Transform parent, string objectName, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Text CreateText(Transform parent, string objectName, string value, int size,
            Vector2 min, Vector2 max, Color color)
        {
            var text = CreateRect(parent, objectName, min, max).gameObject.AddComponent<Text>();
            text.text = value;
            GoldPart2Theme.StyleText(text, size, TextAnchor.MiddleCenter);
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label,
            Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, bool primary)
        {
            var image = CreateRect(parent, objectName, min, max).gameObject.AddComponent<Image>();
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            CreateText(image.transform, "Label", label, 30, new Vector2(.04f, .08f),
                new Vector2(.96f, .92f), GoldPart2Theme.Text);
            GoldPart2Theme.StyleButton(button, primary);
            return button;
        }
    }

    public static class GoldEndScreenInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" || scene.name == TutorialScene.Name) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.GetComponentInChildren<HUDManager>(true) == null) continue;
                    if (canvas.GetComponent<GoldEndScreenPresentation>() == null)
                        canvas.gameObject.AddComponent<GoldEndScreenPresentation>();
                    return;
                }
            }
        }
    }
}
