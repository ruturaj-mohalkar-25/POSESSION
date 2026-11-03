using UnityEngine;
namespace Possession
{
    [RequireComponent(typeof(Door))]
    public sealed class Level03SecurityGate : MonoBehaviour
    {
        private Door door;
        private Renderer blocker;
        private Transform padlock;
        private Color closedColor;
        public bool IsOpen => GetComponent<Door>().IsOpen;
        private void Awake()
        {
            door = GetComponent<Door>(); blocker = GetComponent<Renderer>(); closedColor = blocker.sharedMaterial.color; padlock = transform.Find("P5_Model/Padlock");
            door.SetSecurityState(false);
        }
        public void Apply(bool open)
        {
            door.SetSecurityState(open);
            if (padlock != null) padlock.gameObject.SetActive(!open);
            if (blocker != null) { blocker.enabled = !open; if (!open) blocker.material.color = closedColor; }
        }
    }
}
