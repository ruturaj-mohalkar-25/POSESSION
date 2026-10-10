using UnityEngine;

namespace Possession
{
    public abstract class Ability : MonoBehaviour
    {
        [SerializeField] protected float cooldown = 0.5f;

        protected float cooldownTimer;

        public bool CanUse => cooldownTimer <= 0f;

        protected virtual void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;
        }

        public void TryExecute()
        {
            if (!CanUse) return;
            Execute();
            cooldownTimer = cooldown;
        }

        protected abstract void Execute();
    }
}
