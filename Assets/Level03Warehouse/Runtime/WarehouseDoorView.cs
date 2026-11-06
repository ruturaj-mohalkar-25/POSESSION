using UnityEngine;

namespace Possession.Warehouse
{
    [RequireComponent(typeof(Door))]
    public sealed class WarehouseDoorView : MonoBehaviour
    {
        [SerializeField] private Target[] targets;
        [SerializeField] private Transform leftLeaf, rightLeaf;
        [SerializeField] private Renderer[] lamps;
        [SerializeField] private Color groupColor;
        [SerializeField] private float width;
        private Door door;
        private float opening;
        public Target[] Targets => targets;
        public float Opening => opening;
        private void Awake() => door=GetComponent<Door>();
        private void Update()
        {
            opening=Mathf.MoveTowards(opening,door.IsOpen?1f:0f,Time.deltaTime*2.5f);
            if(leftLeaf!=null)leftLeaf.localPosition=new Vector3(-width*(.25f+.5f*opening),0f,0f);
            if(rightLeaf!=null)rightLeaf.localPosition=new Vector3(width*(.25f+.5f*opening),0f,0f);
            for(int i=0;i<lamps.Length;i++)if(lamps[i]!=null)
                lamps[i].material.color=targets[i].IsHit?new Color(.2f,1f,.5f):groupColor*.65f;
        }
    }
}
