using System.Collections.Generic;
using UnityEngine;

namespace Possession
{
    // Skin the existing sliding leaves, retaining their animation, targets and original blocker.
    [DisallowMultipleComponent, RequireComponent(typeof(SlidingDoorView))]
    public sealed class GoldDoorPresentation : MonoBehaviour
    {
        private SlidingDoorView view;
        private readonly List<Renderer> hidden = new();
        private readonly List<Renderer> panels = new();
        public IReadOnlyList<Renderer> Panels => panels;

        public void Build()
        {
            view = GetComponent<SlidingDoorView>();
            foreach (var leaf in new[] { view.LeftLeaf, view.RightLeaf })
            {
                if (leaf == null) continue;
                var old = leaf.Find("Security leaf details");
                if (old != null) foreach (var renderer in old.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
                leaf.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Gold/Wood");
                var root = new GameObject("Gold door panels").transform;
                root.SetParent(leaf, false);
                float inner = leaf == view.LeftLeaf ? 1f : -1f;
                foreach (float side in new[] { -1f, 1f })
                {
                    foreach (float y in new[] { -.25f, .19f })
                        Piece(root, "Raised wood panel", new Vector3(0, y, side * .56f),
                            new Vector3(.72f, .31f, .10f), "DoorFace");
                    Piece(root, "Handle backplate", new Vector3(inner * .39f, -.04f, side * .57f),
                        new Vector3(.11f, .17f, .10f), "DoorFrame");
                    Piece(root, "Door handle", new Vector3(inner * .38f, -.04f, side * .67f),
                        new Vector3(.15f, .045f, .10f), "DoorBrass");
                }
            }
            var blocker = GetComponent<Renderer>();
            if (blocker != null && blocker.enabled) { hidden.Add(blocker); blocker.enabled = false; }
            var lockModel = transform.Find("P5_Model");
            if (lockModel != null) foreach (var r in lockModel.GetComponentsInChildren<Renderer>(true))
                if (r.enabled) { hidden.Add(r); r.enabled = false; }
        }

        private void Piece(Transform parent, string name, Vector3 position, Vector3 scale, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.layer = 2; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = Resources.Load<Material>("Gold/" + material);
            panels.Add(renderer);
        }

        private void LateUpdate()
        {
            bool visible = view != null && view.Opening < .999f;
            foreach (var panel in panels) if (panel != null) panel.enabled = visible;
        }

        private void OnDestroy()
        {
            foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
        }
    }
}
