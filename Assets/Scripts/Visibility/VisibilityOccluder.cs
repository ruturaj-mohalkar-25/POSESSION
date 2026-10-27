using UnityEngine;

namespace Possession
{
    // Optional override for geometry whose name does not identify it as a wall.
    public sealed class VisibilityOccluder : MonoBehaviour
    {
        [SerializeField] private bool fadeWhenBlocking = true;
        public bool FadeWhenBlocking => fadeWhenBlocking;
    }
}
