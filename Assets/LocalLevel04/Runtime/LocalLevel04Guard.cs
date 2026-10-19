using UnityEngine;
namespace Possession.LocalLevel04
{
    [RequireComponent(typeof(LocalLevel04Brain))]
    public sealed class LocalLevel04Guard : GuardEnemy
    {
        private LocalLevel04Brain brain;
        protected override void Awake() { base.Awake(); brain = GetComponent<LocalLevel04Brain>(); }
        protected override void HandlePatrol() => brain.Tick(waypoints, moveSpeed, false);
        protected override void HandleHostileBehavior() => brain.Tick(waypoints, moveSpeed, isHostile);
        protected override void HandlePossessedMovement() { brain.Drive(moveSpeed); HandlePossessedAbility(); }
        // Required puzzle hosts remain reusable if another Guard's projectile hits them.
        public override void TakeDamage(int amount) { }
    }
}
