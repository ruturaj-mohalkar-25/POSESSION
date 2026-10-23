using UnityEngine;

namespace Possession
{
    // Sells the Heavy's weight: a slower top speed, a bigger gap below the Guard.
    // Runtime-only, installed by EnemyReadabilityInstaller; no shared prefab or AI edits.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeavyEnemy))]
    public sealed class HeavyFeel : MonoBehaviour
    {
        [SerializeField, Range(0.3f, 1f)] private float slowFactor = 0.65f;

        private HeavyEnemy heavy;

        private void Awake()
        {
            heavy = GetComponent<HeavyEnemy>();
            // Widen the speed gap vs the Guard so the Heavy reads as heavy and slow.
            heavy.MoveSpeed *= slowFactor;
        }
    }
}
