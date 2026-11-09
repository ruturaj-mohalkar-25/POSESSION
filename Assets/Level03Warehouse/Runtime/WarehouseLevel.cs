using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Possession.Warehouse
{
    // Only route/extraction presentation is local. Movement, enemies, abilities and puzzles stay shared.
    public sealed class WarehouseLevel : MonoBehaviour
    {
        [SerializeField] private SpiritController spirit;
        [SerializeField] private Door inspection, exitPassage, cargoEntry, cargo;
        [SerializeField] private BreakableWall loadingWall;
        [SerializeField] private Text objective;
        [SerializeField] private Renderer[] exitLights;
        private bool completed;
        public SpiritController Spirit => spirit;
        public PossessionController Possession => spirit.GetComponent<PossessionController>();
        public bool LeftReady => inspection.IsOpen && exitPassage.IsOpen;
        public bool RightReady => cargoEntry.IsOpen && cargo.IsOpen && loadingWall.CurrentHP <= 0;
        public bool CanExtract => LeftReady || RightReady;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartPreview();
            if (objective != null) objective.text = CanExtract ? "EXIT OPEN  •  REACH THE EXIT" : "OPEN A WING  •  EACH DOOR NEEDS 3 TARGETS";
            if (exitLights != null) foreach (var light in exitLights) if (light != null)
                light.material.color = CanExtract ? new Color(.2f,1f,.65f) : new Color(1f,.65f,.18f);
        }
        public bool TryFinish(Collider other)
        {
            if (completed || !CanExtract || other == null || spirit.CurrentHP <= 0
                || GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            bool isSpirit = Possession.CurrentState == PlayerState.Spirit
                && other.GetComponentInParent<SpiritController>() == spirit;
            var host = Possession.PossessedEnemy;
            bool isHost = Possession.CurrentState != PlayerState.Spirit && host != null
                && host.gameObject.activeInHierarchy && host.CurrentHP > 0
                && other.GetComponentInParent<EnemyBase>() == host;
            if (!isSpirit && !isHost) return false;
            var exit = GetComponent<Collider>();
            if (exit == null || !exit.bounds.Intersects(other.bounds)) return false;
            completed = true;EventBus.LevelCompleted?.Invoke();return true;
        }
        private void OnTriggerEnter(Collider other) => TryFinish(other);
        private void OnTriggerStay(Collider other) => TryFinish(other);
        public void RestartPreview()
        {
            string scenePath = gameObject.scene.path;
            Time.timeScale=1f;EventBus.Clear();
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scenePath);
        }
    }
}
