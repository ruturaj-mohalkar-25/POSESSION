using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(Door))]
    public class SlidingDoorView : MonoBehaviour
    {
        [SerializeField] protected Target[] targets;
        [SerializeField] protected Transform leftLeaf, rightLeaf;
        [SerializeField] protected Renderer[] lamps;
        [SerializeField] protected Color groupColor;
        [SerializeField] protected float width;
        [SerializeField] private bool clipToFrame;
        private float leftWidth, rightWidth;
        private Door door;
        private float opening;
        public Target[] Targets => targets;
        public float Opening => opening;
        public Transform LeftLeaf => leftLeaf;
        public Transform RightLeaf => rightLeaf;

        protected virtual void Awake()
        {
            door = GetComponent<Door>();
            leftWidth = leftLeaf != null ? leftLeaf.localScale.x : 0f;
            rightWidth = rightLeaf != null ? rightLeaf.localScale.x : 0f;
        }

        protected virtual void Update()
        {
            opening = Mathf.MoveTowards(opening, door.IsOpen ? 1f : 0f, Time.deltaTime * 2.5f);
            UpdateLeaf(leftLeaf, leftWidth, -1f);
            UpdateLeaf(rightLeaf, rightWidth, 1f);
            if (lamps == null || targets == null) return;
            for (int i = 0; i < Mathf.Min(lamps.Length, targets.Length); i++)
                if (lamps[i] != null && targets[i] != null)
                    lamps[i].material.color = targets[i].IsHit ? new Color(.2f, 1f, .5f) : groupColor * .65f;
        }
        private void UpdateLeaf(Transform leaf, float closedWidth, float side)
        {
            if (leaf == null) return;
            leaf.localPosition = new Vector3(side * width * (.25f + (clipToFrame ? .25f : .5f) * opening), 0f, 0f);
            if (!clipToFrame) return;
            // Thin corridor walls have no deep pocket: show only the panel still inside the frame.
            var scale = leaf.localScale;
            scale.x = closedWidth * Mathf.Max(.0001f, 1f - opening);
            leaf.localScale = scale;
            leaf.GetComponent<Renderer>().enabled = opening < 1f;
        }
    }
}
