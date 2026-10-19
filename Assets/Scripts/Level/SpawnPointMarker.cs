using UnityEngine;

namespace Possession
{
    /// <summary>
    /// Editor-only marker for the named enemy spawn points the level builders place.
    /// It draws a colored gizmo so a designer can see, in the Scene view, where each
    /// Guard or Heavy will stand before P1 and P3 wire the real enemies in. The gizmo
    /// is compiled out of player builds, so it costs nothing at runtime.
    /// </summary>
    public class SpawnPointMarker : MonoBehaviour
    {
        public enum SpawnKind { Guard, Heavy, Other }

        [Tooltip("Which enemy stands here. Drives the gizmo color only.")]
        [SerializeField] private SpawnKind kind = SpawnKind.Other;

        public SpawnKind Kind => kind;

        public void SetKind(SpawnKind value) => kind = value;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color color = kind switch
            {
                SpawnKind.Guard => new Color(1f, 0.85f, 0.2f),
                SpawnKind.Heavy => new Color(0.9f, 0.3f, 0.25f),
                _ => new Color(0.4f, 0.8f, 1f),
            };

            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, 0.6f);

            // A stalk up from the floor so the marker is easy to spot from any angle.
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
#endif
    }
}
