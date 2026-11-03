using UnityEngine;
namespace Possession
{
    public sealed class Level02Exit : MonoBehaviour
    {
        [SerializeField] private Level02Director director;
        private bool finished;
        private void OnTriggerEnter(Collider other) => TryFinish(other);
        private void OnTriggerStay(Collider other) => TryFinish(other);
        public bool TryFinish(Collider other)
        {
            if (finished || !director.CanExit || !director.IsPlayer(other) || director.Spirit.CurrentHP <= 0
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            finished = true; EventBus.LevelCompleted?.Invoke(); return true;
        }
    }
}
