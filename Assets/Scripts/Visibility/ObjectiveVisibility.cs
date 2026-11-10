using UnityEngine;
using UnityEngine.Rendering;

namespace Possession
{
    // A depth-tested silhouette reveals a puzzle object behind geometry without changing its collisions.
    [DisallowMultipleComponent]
    public sealed class ObjectiveVisibility : MonoBehaviour
    {
        private Target target;
        private PossessionChargePickup pickup;
        private MeshRenderer silhouette;
        private Material material;
        private Renderer source;
        public Renderer Source => source;
        public Renderer Silhouette => silhouette;

        public static void Ensure(Component objective)
        {
            var visibility = objective.GetComponent<ObjectiveVisibility>();
            if (visibility == null) visibility = objective.gameObject.AddComponent<ObjectiveVisibility>();
            visibility.RefreshVisibility();
        }

        private void OnEnable() => RefreshVisibility();
        private void LateUpdate() => RefreshVisibility();
        private void OnDisable() { if (silhouette != null) silhouette.enabled = false; }
        private void OnDestroy() { if (material != null) Destroy(material); }

        public void RefreshVisibility()
        {
            if (target == null) target = GetComponent<Target>();
            if (pickup == null) pickup = GetComponent<PossessionChargePickup>();
            Renderer visual = target != null ? target.GetComponent<MeshRenderer>()
                : pickup != null ? pickup.Visual : null;
            if (visual != source)
            {
                if (silhouette != null) Destroy(silhouette.gameObject);
                if (material != null) Destroy(material);
                source = visual; silhouette = null; material = null;
            }
            if (source == null) return;
            if (silhouette == null)
            {
                var mesh = source.GetComponent<MeshFilter>();
                var shader = Resources.Load<Shader>("Visibility/ObjectiveSilhouette");
                if (mesh == null || mesh.sharedMesh == null || shader == null) return;
                var go = new GameObject("Objective silhouette", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(source.transform, false);
                go.layer = source.gameObject.layer;
                go.GetComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                material = new Material(shader) { name = "Objective visibility", hideFlags = HideFlags.DontSave };
                silhouette = go.GetComponent<MeshRenderer>();
                silhouette.sharedMaterial = material;
                silhouette.shadowCastingMode = ShadowCastingMode.Off;
                silhouette.receiveShadows = false;
                silhouette.lightProbeUsage = LightProbeUsage.Off;
                silhouette.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            bool ready = target != null ? target.isActiveAndEnabled && !target.IsHit
                : pickup != null && pickup.isActiveAndEnabled && pickup.IsReady;
            silhouette.enabled = isActiveAndEnabled && ready && source.enabled && source.gameObject.activeInHierarchy;
            var color = source.sharedMaterial != null && source.sharedMaterial.HasProperty("_Color")
                ? source.sharedMaterial.color : new Color(.35f, 1f, .7f);
            color.a = .8f;
            material.color = color;
            material.SetFloat("_Target", target != null ? 1f : 0f);
        }
    }
}
