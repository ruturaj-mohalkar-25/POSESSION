using UnityEngine;
namespace Possession
{
    public sealed class Level03Exit : MonoBehaviour
    {
        private Level03Director level;
        private bool completed;
        private void Awake() => level = FindFirstObjectByType<Level03Director>();
        private void OnTriggerEnter(Collider other) => TryFinish(other);
        private void OnTriggerStay(Collider other) => TryFinish(other);
        public bool TryFinish(Collider other)
        {
            if (completed || level == null || !level.IsPlayer(other) || level.Spirit.CurrentHP <= 0
                || !GetComponent<Collider>().bounds.Intersects(other.bounds)
                || GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            completed = true; EventBus.LevelCompleted?.Invoke(); return true;
        }
    }
}
