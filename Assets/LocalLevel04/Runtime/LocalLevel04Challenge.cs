using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04Challenge : MonoBehaviour
    {
        public LocalLevel04Gate[] gates;
        public LocalLevel04Navigation navigation;
        public Target[] targets;
        public EnemyBase[] hosts;
        public Transform finish;
        private SpiritController spirit;
        private PossessionController player;
        public bool PossessPrompt { get; private set; }
        public int Stage
        {
            get { int next = 0; while (next < gates.Length && gates[next].IsOpen) next++; return next; }
        }

        private void Start()
        {
            spirit = FindFirstObjectByType<SpiritController>();
            player = spirit.GetComponent<PossessionController>();
            gameObject.AddComponent<LocalLevel04HUD>().Bind(this);
        }
        private void OnEnable() => EventBus.PossessPromptUpdated += SetPrompt;
        private void OnDisable() => EventBus.PossessPromptUpdated -= SetPrompt;
        private void SetPrompt(bool value) => PossessPrompt = value;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
        }

        public bool TryFinish(SpiritController candidate)
        {
            if (candidate == null || candidate.CurrentHP <= 0 || player == null
                || player.CurrentState != PlayerState.Spirit || GameManager.Instance == null
                || GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            foreach (var gate in gates) if (!gate.IsOpen) return false;
            EventBus.LevelCompleted?.Invoke();
            return true;
        }

        public void Restart()
        {
            string path = gameObject.scene.path;
            Time.timeScale = 1f;
            EventBus.Clear();
            // This experiment intentionally has no build index.
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(gameObject.scene.name);
#endif
        }

    }
}
