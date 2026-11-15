using UnityEngine;

namespace Possession.Level04Gold
{
    public sealed class Level4AccessGate : MonoBehaviour
    {
        [SerializeField] private Transform releasePoint;
        [SerializeField] private Transform engineerPoint;
        [SerializeField] private Transform movingPanel;
        [SerializeField] private Renderer releaseLamp;
        [SerializeField] private bool hidePanelWhenOpen;
        private Vector3 closedPosition;
        private float opening;
        public bool IsOpen { get; private set; }
        public Transform ReleasePoint => releasePoint;
        public Transform EngineerPoint => engineerPoint;

        private void Awake()
        {
            if (movingPanel != null) closedPosition = movingPanel.localPosition;
        }

        public bool CanRelease(Transform actor) => CanReach(actor, releasePoint);

        public bool CanUnlock(Transform actor)
        {
            var player = FindFirstObjectByType<PossessionController>();
            var engineer = player != null ? player.PossessedEnemy as EngineerEnemy : null;
            return engineer != null && engineer.isActiveAndEnabled && engineer.IsPossessed && engineer.CurrentHP > 0
                && actor == engineer.transform && CanReach(actor, engineerPoint);
        }

        public bool TryUnlock(Transform actor)
        {
            if (!CanUnlock(actor)) return false;
            ReleaseFromMechanism();
            return true;
        }

        private bool CanReach(Transform actor, Transform point)
        {
            if (IsOpen || actor == null || point == null || Time.timeScale <= 0f) return false;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            var player = FindFirstObjectByType<PossessionController>();
            if (player == null || actor != (player.PossessedEnemy != null ? player.PossessedEnemy.transform : player.transform)) return false;
            if (Vector3.Distance(actor.position, point.position) > 2.8f || Mathf.Abs(actor.position.y - point.position.y) > 2f) return false;
            foreach (var hit in Physics.RaycastAll(actor.position, point.position - actor.position,
                         Vector3.Distance(actor.position, point.position), ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(actor) || hit.transform.IsChildOf(point)) continue;
                return false;
            }
            return true;
        }

        public bool TryRelease(Transform actor)
        {
            if (!CanRelease(actor)) return false;
            ReleaseFromMechanism();
            return true;
        }

        public void ReleaseFromMechanism()
        {
            if (IsOpen) return;
            IsOpen = true;
            var blocker = GetComponent<Collider>();
            if (blocker != null) blocker.enabled = false;
            if (releaseLamp != null) releaseLamp.material.color = new Color(.25f, 1f, .6f);
        }

        private void Update()
        {
            if (IsOpen) opening = Mathf.MoveTowards(opening, 1f, Time.deltaTime * 2f);
            if (movingPanel != null)
            {
                movingPanel.localPosition = closedPosition + Vector3.down * (5f * opening);
                if (hidePanelWhenOpen && opening >= 1f) movingPanel.gameObject.SetActive(false);
            }
        }
    }
}
