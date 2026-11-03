using UnityEngine;
namespace Possession
{
    public sealed class Level02CheckpointVolume : MonoBehaviour
    {
        [SerializeField] private Level02Director director;
        private void OnTriggerEnter(Collider other) { if (director.IsPlayer(other)) director.MarkCheckpoint(); }
        private void OnTriggerStay(Collider other) { if (director.IsPlayer(other)) director.MarkCheckpoint(); }
    }
}
