using UnityEngine;

namespace Possession
{
    public class SpawnPointMarker : MonoBehaviour
    {
        public enum SpawnKind { Guard, Heavy, Other }

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
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
        }
#endif
    }
}
