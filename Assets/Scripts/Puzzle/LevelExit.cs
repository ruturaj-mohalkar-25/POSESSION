using UnityEngine;

namespace Possession
{
    // The level designer owns the final gate; existing puzzle and player APIs stay intact.
    [RequireComponent(typeof(BoxCollider))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private Door puzzleDoor;
        [SerializeField] private BreakableWall breakableWall;
        private bool completed;

        public bool IsReady => puzzleDoor != null && puzzleDoor.IsOpen
            && breakableWall != null && breakableWall.CurrentHP <= 0;

        private void OnTriggerEnter(Collider other) => TryComplete(other);

        // Also handles a spirit ejecting inside the exit after arriving as an enemy.
        private void OnTriggerStay(Collider other) => TryComplete(other);

        private void TryComplete(Collider other)
        {
            if (completed || !IsReady) return;
            var spirit = other.GetComponentInParent<SpiritController>();
            if (spirit == null || spirit.CurrentHP <= 0) return;
            var possession = spirit.GetComponent<PossessionController>();
            if (possession == null || possession.CurrentState != PlayerState.Spirit) return;
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != LevelState.InProgress) return;

            completed = true;
            EventBus.LevelCompleted?.Invoke();
        }
    }
}
