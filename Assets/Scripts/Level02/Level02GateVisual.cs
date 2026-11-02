using UnityEngine;
namespace Possession
{
    [RequireComponent(typeof(Door))]
    public sealed class Level02GateVisual : MonoBehaviour
    {
        private Door door;
        private Transform padlock;
        private void Start() { door=GetComponent<Door>();padlock=transform.Find("P5_Model/Padlock"); }
        private void Update()
        {
            if (!door.IsOpen) return;
            if (padlock!=null)padlock.gameObject.SetActive(false);
            enabled=false;
        }
    }
}
