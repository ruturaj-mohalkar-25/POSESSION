using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main")]
        [SerializeField] private GameObject mainButtons; // PLAY + QUIT
        [SerializeField] private GameObject quitButton;

        [Header("Level Select")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private Button[] levelButtons;
        [SerializeField] private string[] levelScenes = { "Level_01", "Level_02", "Level_03" };

        private void Start()
        {
            // Arriving from a paused Game Over / Level Complete screen
            Time.timeScale = 1f;

            // Browsers can't close the tab, so Quit would do nothing
            if (quitButton != null && Application.platform == RuntimePlatform.WebGLPlayer)
                quitButton.SetActive(false);

            ShowLevelSelect(false);
            RefreshLevelButtons();
        }

        public void OnPlayButton()
        {
            ShowLevelSelect(true);
        }

        public void OnBackButton()
        {
            ShowLevelSelect(false);
        }

        public void OnLevelButton(int index)
        {
            if (index < 0 || index >= levelScenes.Length) return;
            if (!LevelFlow.IsInBuild(levelScenes[index])) return;
            LevelFlow.Load(levelScenes[index]);
        }

        public void OnQuitButton()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowLevelSelect(bool show)
        {
            if (levelSelectPanel != null) levelSelectPanel.SetActive(show);
            if (mainButtons != null) mainButtons.SetActive(!show);
        }

        // Levels that aren't built yet stay visible but locked
        private void RefreshLevelButtons()
        {
            if (levelButtons == null) return;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                bool available = i < levelScenes.Length && LevelFlow.IsInBuild(levelScenes[i]);
                levelButtons[i].interactable = available;

                var label = levelButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = available ? $"LEVEL {i + 1}" : $"LEVEL {i + 1}  (LOCKED)";
            }
        }
    }
}
