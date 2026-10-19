using UnityEngine;

namespace Possession.LocalLevel04
{
    // Deliberately not IDamageable: ordinary projectiles cannot break a Heavy gate.
    public sealed class LocalLevel04Gate : MonoBehaviour
    {
        public bool requiresHeavy;
        public Target target;
        public LocalLevel04Gate prerequisite;
        public Renderer panel;
        public TextMesh sign;
        public bool IsOpen { get; private set; }
        public bool Ready => prerequisite == null || prerequisite.IsOpen;

        private void Update()
        {
            if (!requiresHeavy && target != null && target.IsHit && Ready) Open();
        }

        public bool TrySmash(EnemyBase actor)
        {
            var player = FindFirstObjectByType<PossessionController>();
            if (!requiresHeavy || IsOpen || !Ready || !(actor is HeavyEnemy)
                || player == null || player.PossessedEnemy != actor) return false;
            Open();
            return true;
        }

        private void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            GetComponent<Collider>().enabled = false;
            if (panel != null) panel.enabled = false;
            if (sign != null) { sign.text = "OPEN"; sign.color = Color.green; }
        }
    }
}
