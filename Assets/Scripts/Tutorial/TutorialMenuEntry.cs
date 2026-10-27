using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession
{
    public static class TutorialMenuEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() => Install(SceneManager.GetActiveScene());
        private static void Loaded(Scene scene, LoadSceneMode mode) => Install(scene);

        public static Button Install(Scene scene)
        {
            var controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuController>(true))
                .FirstOrDefault();
            if (controller == null) return null;
            var buttons = controller.GetComponentsInChildren<Button>(true);
            var play = buttons.FirstOrDefault(b => b.name == "PlayButton");
            if (play == null) return null;
            var button = buttons.FirstOrDefault(b => b.name == "HowToPlayButton");
            if (button == null)
            {
                button = Object.Instantiate(play, play.transform.parent);
                button.name = "HowToPlayButton";
            }
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(OpenTutorial);
            button.interactable = LevelFlow.IsInBuild(TutorialScene.Name);
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "HOW TO PLAY";
                label.color = new Color(0.08f, 0.11f, 0.16f);
            }
            var colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.67f, 0.22f);
            colors.highlightedColor = new Color(1f, 0.85f, 0.45f);
            button.colors = colors;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = Color.white;
            Position(play, 85f);
            Position(button, -25f);
            var quit = buttons.FirstOrDefault(b => b.name == "QuitButton");
            if (quit != null) Position(quit, -135f);
            return button;
        }

        public static void OpenTutorial()
        {
            if (LevelFlow.IsInBuild(TutorialScene.Name)) LevelFlow.Load(TutorialScene.Name);
        }

        private static void Position(Button button, float y)
        {
            var rect = button.transform as RectTransform;
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}
