using UnityEngine;

namespace Possession
{
    public abstract class Ability : MonoBehaviour
    {
        [SerializeField] protected float cooldown = 0.5f;
        [SerializeField] protected int maxUses = -1;

        protected float cooldownTimer;
        protected int usesLeft;

        public bool CanUse => cooldownTimer <= 0f && (maxUses <= 0 || usesLeft > 0);
        public int UsesLeft => usesLeft;
        public int MaxUses => maxUses;

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
            if (maxUses > 0)
            {
                usesLeft--;
                EventBus.AmmoUpdated?.Invoke(usesLeft, maxUses);
            }
        }

        public void ResetUses()
        {
            usesLeft = maxUses;
            if (maxUses > 0)
                EventBus.AmmoUpdated?.Invoke(usesLeft, maxUses);
        }

        protected abstract void Execute();
    }
}
