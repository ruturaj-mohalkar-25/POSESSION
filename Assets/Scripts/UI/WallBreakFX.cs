using UnityEngine;

namespace Possession
{
    // Lives on the BreakableWall: bricks fly out and dust puffs when it is smashed.
    [RequireComponent(typeof(BreakableWall))]
    public class WallBreakFX : MonoBehaviour
    {
        [SerializeField] private Material chunkMaterial;
        [SerializeField] private Material dustMaterial;
        [SerializeField] private int chunkCount = 16;

        private BreakableWall wall;

        private void Awake()
        {
            wall = GetComponent<BreakableWall>();
        }

        private void OnEnable()
        {
            EventBus.WallBroken += OnWallBroken;
        }

        private void OnDisable()
        {
            EventBus.WallBroken -= OnWallBroken;
        }

        private void OnWallBroken(BreakableWall broken)
        {
            if (broken != wall) return;

            // Spread chunks over the wall face; push them back toward the smasher (-z)
            Vector3 size = transform.localScale;
            Vector3 center = transform.position + Vector3.down * size.y * 0.2f;
            SimpleFX.Chunks(center, new Vector3(size.x * 0.45f, size.y * 0.3f, 0.2f), chunkMaterial,
                chunkCount, Vector3.back * 3f);
            SimpleFX.Burst(center, dustMaterial, 0.8f, 2.2f, 0.3f);
        }
    }
}
