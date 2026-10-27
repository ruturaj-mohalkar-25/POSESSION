using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Possession
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), DefaultExecutionOrder(1000)]
    public sealed class WallOcclusionFader : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.8f)] private float obstructingOpacity = 0.18f;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float restoreSeconds = 0.2f;
        [SerializeField, Min(0.01f)] private float probeRadius = 0.25f;
        [SerializeField] private LayerMask occluderLayers = ~0;
        [SerializeField] private PossessionController possession;

        private static readonly int OpacityId = Shader.PropertyToID("_P5OcclusionAlpha");
        private readonly Dictionary<Renderer, FadedRenderer> faded = new();
        private readonly HashSet<Renderer> obstructing = new();
        private readonly List<Renderer> finished = new();
        private readonly List<Renderer> candidateRenderers = new();
        private RaycastHit[] hits = new RaycastHit[32];
        private Collider[] overlaps = new Collider[16];
        private Camera view;
        private Shader fadeShader;
        private bool missingShaderReported;

        public Transform TrackedTarget { get; private set; }
        public int FadedRendererCount => faded.Count;

        private void LateUpdate() => RefreshVisibility(Time.unscaledDeltaTime);

        // Also supports a deterministic preview from the editor without saving scene changes.
        public void RefreshVisibility(float deltaTime)
        {
            if (view == null) view = GetComponent<Camera>();
            if (possession == null) possession = FindFirstObjectByType<PossessionController>();
            TrackedTarget = possession == null ? null : possession.PossessedEnemy != null
                ? possession.PossessedEnemy.transform : possession.transform;
            obstructing.Clear();

            if (view != null && view.isActiveAndEnabled && TrackedTarget != null
                && TrackedTarget.gameObject.activeInHierarchy)
            {
                Collider body = TrackedTarget.GetComponent<Collider>();
                if (body == null || !body.enabled) body = TrackedTarget.GetComponentInChildren<Collider>();
                Bounds bounds = body != null && body.enabled ? body.bounds
                    : new Bounds(TrackedTarget.position, new Vector3(0.8f, 2f, 0.8f));
                Vector3 viewport = view.WorldToViewportPoint(bounds.center);
                if (viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f
                    && viewport.y >= 0f && viewport.y <= 1f)
                {
                    Probe(bounds.center);
                    Probe(bounds.center + Vector3.up * Mathf.Max(0f, bounds.extents.y - probeRadius));
                    Probe(bounds.center - Vector3.up * Mathf.Max(0f, bounds.extents.y - probeRadius));
                    ProbeCameraInterior();
                }
            }

            foreach (Renderer renderer in obstructing)
            {
                if (faded.ContainsKey(renderer) || !TryGetShader()) continue;
                Material[] materials = renderer.sharedMaterials;
                if (Array.Exists(materials, material => material != null))
                    faded.Add(renderer, new FadedRenderer(renderer, materials, fadeShader));
            }

            finished.Clear();
            foreach (var pair in faded)
            {
                FadedRenderer state = pair.Value;
                bool blocked = pair.Key != null && obstructing.Contains(pair.Key);
                float target = blocked ? obstructingOpacity : 1f;
                float seconds = blocked ? fadeSeconds : restoreSeconds;
                state.Opacity = Mathf.MoveTowards(state.Opacity, target,
                    Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, seconds));
                state.Apply();
                if (pair.Key == null || !blocked && state.Opacity >= 0.999f)
                {
                    state.Restore();
                    finished.Add(pair.Key);
                }
            }
            foreach (Renderer renderer in finished) faded.Remove(renderer);
        }

        public void RestoreAll()
        {
            foreach (FadedRenderer state in faded.Values) state.Restore();
            faded.Clear();
            obstructing.Clear();
        }

        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();

        private bool TryGetShader()
        {
            if (fadeShader == null) fadeShader = Resources.Load<Shader>("Visibility/P5WallFade");
            if (fadeShader != null) return true;
            if (!missingShaderReported)
            {
                Debug.LogWarning("Wall visibility shader is missing. Geometry will remain opaque.", this);
                missingShaderReported = true;
            }
            return false;
        }

        private void Probe(Vector3 destination)
        {
            Vector3 direction = destination - view.transform.position;
            float distance = direction.magnitude;
            if (distance <= 0.01f) return;
            int count;
            while (true)
            {
                count = Physics.SphereCastNonAlloc(view.transform.position, probeRadius,
                    direction / distance, hits, distance, occluderLayers, QueryTriggerInteraction.Ignore);
                if (count < hits.Length || hits.Length >= 512) break;
                Array.Resize(ref hits, hits.Length * 2);
            }
            for (int i = 0; i < count; i++) AddOccluder(hits[i].collider);
        }

        private void ProbeCameraInterior()
        {
            int count;
            while (true)
            {
                count = Physics.OverlapSphereNonAlloc(view.transform.position, probeRadius,
                    overlaps, occluderLayers, QueryTriggerInteraction.Ignore);
                if (count < overlaps.Length || overlaps.Length >= 512) break;
                Array.Resize(ref overlaps, overlaps.Length * 2);
            }
            for (int i = 0; i < count; i++) AddOccluder(overlaps[i]);
        }

        private void AddOccluder(Collider collider)
        {
            if (collider == null || !ShouldFade(collider)) return;
            Renderer renderer = collider.GetComponent<Renderer>();
            if (renderer == null) renderer = collider.GetComponentInParent<MeshRenderer>();
            if (renderer != null)
            {
                if (renderer.enabled && renderer.gameObject.activeInHierarchy) obstructing.Add(renderer);
                return;
            }
            candidateRenderers.Clear();
            collider.GetComponentsInChildren(false, candidateRenderers);
            foreach (Renderer child in candidateRenderers)
                if (child is MeshRenderer && child.enabled
                    && child.GetComponentInParent<EnemyBase>() == null
                    && child.GetComponentInParent<SpiritController>() == null)
                    obstructing.Add(child);
        }

        private static bool ShouldFade(Collider collider)
        {
            // Interactive puzzle visuals and character feedback remain owned by their systems.
            if (collider.GetComponentInParent<EnemyBase>() != null
                || collider.GetComponentInParent<SpiritController>() != null
                || collider.GetComponentInParent<Door>() != null
                || collider.GetComponentInParent<Target>() != null
                || collider.GetComponentInParent<BreakableWall>() != null) return false;

            var marker = collider.GetComponentInParent<VisibilityOccluder>();
            if (marker != null) return marker.FadeWhenBlocking;
            for (Transform node = collider.transform; node != null; node = node.parent)
            {
                string name = node.name;
                if (name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Divider", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Cover", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private sealed class FadedRenderer
        {
            private readonly Renderer renderer;
            private readonly Material[] originals;
            private readonly Material[] temporary;
            public float Opacity = 1f;

            public FadedRenderer(Renderer renderer, Material[] materials, Shader shader)
            {
                this.renderer = renderer;
                originals = materials;
                temporary = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    var clone = new Material(shader);
                    clone.CopyPropertiesFromMaterial(materials[i]);
                    clone.name = materials[i].name + " (Visibility)";
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    clone.renderQueue = (int)RenderQueue.Transparent;
                    clone.SetOverrideTag("RenderType", "Transparent");
                    clone.SetFloat(OpacityId, 1f);
                    temporary[i] = clone;
                }
                renderer.sharedMaterials = temporary;
            }

            public void Apply()
            {
                if (renderer == null) return;
                foreach (Material material in temporary)
                    if (material != null) material.SetFloat(OpacityId, Opacity);
            }

            public void Restore()
            {
                if (renderer != null)
                {
                    Material[] current = renderer.sharedMaterials;
                    for (int i = 0; i < current.Length && i < temporary.Length; i++)
                        if (current[i] == temporary[i]) current[i] = originals[i];
                    // Preserve material replacements made by another system during the fade.
                    renderer.sharedMaterials = current;
                }
                foreach (Material material in temporary)
                {
                    if (material == null) continue;
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
            }
        }
    }
}
