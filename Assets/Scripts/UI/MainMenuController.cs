using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string firstLevel = "Level_01";

        private void Start()
        {
            // Arriving from a paused Game Over / Level Complete screen
            Time.timeScale = 1f;
        }

        public void OnPlayButton()
        {
            EventBus.Clear();
            SceneManager.LoadScene(firstLevel);
        }

        public void OnQuitButton()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
