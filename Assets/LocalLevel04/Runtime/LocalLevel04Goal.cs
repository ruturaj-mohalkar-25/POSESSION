using UnityEngine;
namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04Goal : MonoBehaviour
    {
        public LocalLevel04Challenge challenge;
        private void OnTriggerEnter(Collider other) => challenge.TryFinish(other.GetComponent<SpiritController>());
        private void OnTriggerStay(Collider other) => challenge.TryFinish(other.GetComponent<SpiritController>());
    }
}
