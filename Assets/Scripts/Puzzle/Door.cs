using UnityEngine;

namespace Possession
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private Target[] requiredTargets;
        [SerializeField] private bool isExit;

        private bool isOpen;
        private bool securityControlled;

        public bool IsOpen => isOpen;

        private void OnEnable()
        {
            EventBus.TargetHit += OnTargetHit;
        }

        private void OnDisable()
        {
            EventBus.TargetHit -= OnTargetHit;
        }

        private void Start()
        {
            // Exit with no required targets starts open
            if (!securityControlled && (requiredTargets == null || requiredTargets.Length == 0))
                isOpen = true;
        }

        private void OnTargetHit(Target target)
        {
            if (isOpen || securityControlled) return;

            foreach (var t in requiredTargets)
            {
                if (!t.IsHit) return;
            }

            Open();
        }

        // Security controllers may relock a door; ordinary target doors retain their existing behavior.
        public void SetSecurityState(bool open)
        {
            securityControlled = true;
            if (open) { if (!isOpen) Open(); return; }
            isOpen = false;
            var blocker = GetComponent<Collider>();
            if (blocker != null) blocker.isTrigger = false;
        }

        private void Open()
        {
            isOpen = true;

            // Visual: disable the door blocker
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0f, 1f, 0f, 0.3f);

            EventBus.DoorOpened?.Invoke(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isOpen || !isExit) return;

            if (other.GetComponent<SpiritController>() != null)
                EventBus.LevelCompleted?.Invoke();
        }
    }
}
