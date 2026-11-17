using UnityEngine;

namespace Possession.Level04Gold
{
    public sealed class Level4Cipher : MonoBehaviour
    {
        [SerializeField] private Transform recoveryPoint;
        [SerializeField] private GameObject moduleVisual;
        [SerializeField] private Renderer recoveredLamp;
        [SerializeField] private Level4AccessGate[] returnGates;
        private PossessionController player;

        public bool Recovered { get; private set; }
        public Transform RecoveryPoint => recoveryPoint;
        public static string Hint => "1 CW TRIANGLE • 2 CCW SQUARE";

        public bool CanRecover(Transform actor)
        {
            if (Recovered || actor == null || recoveryPoint == null || Time.timeScale <= 0f) return false;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            if (player == null) player = FindFirstObjectByType<PossessionController>();
            var engineer = player != null ? player.PossessedEnemy as EngineerEnemy : null;
            if (engineer == null || actor != engineer.transform || !engineer.isActiveAndEnabled
                || !engineer.IsPossessed || engineer.CurrentHP <= 0) return false;
            Vector3 delta = recoveryPoint.position - actor.position;
            if (delta.sqrMagnitude > 2.8f * 2.8f || Mathf.Abs(delta.y) > 2f) return false;
            foreach (var hit in Physics.RaycastAll(actor.position, delta, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(actor) || hit.transform.IsChildOf(transform)
                    || hit.transform.IsChildOf(recoveryPoint)) continue;
                return false;
            }
            return true;
        }

        public bool TryRecover(Transform actor)
        {
            if (!CanRecover(actor)) return false;
            Recovered = true;
            if (moduleVisual != null) moduleVisual.SetActive(false);
            if (recoveredLamp != null) recoveredLamp.material.color = new Color(.25f, 1f, .6f);
            if (returnGates != null)
                foreach (var gate in returnGates)
                    if (gate != null) gate.ReleaseFromMechanism();
            return true;
        }
    }
}
