using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // One visual follows the actual controlled body, including possession and ejection.
    // Nothing here changes body materials (including their existing warning flashes).
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class PlayerIdentityView : MonoBehaviour
    {
        private PossessionController possession;
        private SpiritController spirit;
        private GameObject visuals;
        private Transform collar;
        private Mesh collarMesh;
        private MeshRenderer collarRenderer;
        private MaterialPropertyBlock tint;
        private float pulseRemaining;
        private const float PulseDuration = .55f;
        public float PulseAmount { get; private set; }
        public Transform TrackedBody { get; private set; }
        public bool IsShowing => visuals != null && visuals.activeSelf;

        private void Awake() => EnsureVisuals();

        private void EnsureVisuals()
        {
            if (visuals != null) return;
            tint = new MaterialPropertyBlock();
            possession = GetComponent<PossessionController>();
            spirit = GetComponent<SpiritController>();
            visuals = new GameObject("Gold_PlayerIdentity") { layer = 2 };
            SceneManager.MoveGameObjectToScene(visuals, gameObject.scene);
            collarMesh = RingMesh();
            collar = MeshPart("Teal collar", collarMesh, Resources.Load<Material>("Gold/Teal"));
            collarRenderer = collar.GetComponent<MeshRenderer>();
        }

        private Transform MeshPart(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
            go.transform.SetParent(visuals.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            EnsureVisuals();
            var nextBody = possession != null && possession.PossessedEnemy != null
                ? possession.PossessedEnemy.transform : transform;
            if (TrackedBody != null && nextBody != TrackedBody) pulseRemaining = PulseDuration;
            TrackedBody = nextBody;
            var renderer = TrackedBody.GetComponent<Renderer>();
            bool alive = spirit != null && (!Application.isPlaying || spirit.CurrentHP > 0);
            if (possession != null && possession.PossessedEnemy != null)
                alive &= possession.PossessedEnemy.CurrentHP > 0;
            bool showing = alive && renderer != null && renderer.enabled && TrackedBody.gameObject.activeInHierarchy
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress);
            visuals.SetActive(showing);
            if (!showing) { pulseRemaining = 0; PulseAmount = 0; return; }
            if (Application.isPlaying && Time.timeScale > 0)
                pulseRemaining = Mathf.Max(0, pulseRemaining - Time.deltaTime);
            PulseAmount = pulseRemaining > 0 ? Mathf.Sin(Mathf.PI * (1f - pulseRemaining / PulseDuration)) : 0;
            var bounds = renderer.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            collar.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * .32f, bounds.center.z);
            float diameter = radius * 2.1f * (1f + .12f * PulseAmount);
            collar.localScale = new Vector3(diameter, .20f + .10f * PulseAmount, diameter);
            tint.SetColor("_Color", Color.Lerp(GoldTheme.Teal, new Color(.40f, .85f, .77f), PulseAmount));
            collarRenderer.SetPropertyBlock(tint);
        }

        private void OnDisable()
        {
            if (visuals != null) visuals.SetActive(false);
            pulseRemaining = 0; PulseAmount = 0;
        }
        private void OnDestroy()
        {
            Release(visuals);
            Release(collarMesh);
        }

        private static void Release(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private static Mesh RingMesh()
        {
            const int segments = 16;
            var vertices = new Vector3[segments * 4];
            var triangles = new int[segments * 24];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var axis = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[i*4] = axis * .55f + Vector3.up * .5f;
                vertices[i*4+1] = axis * .55f - Vector3.up * .5f;
                vertices[i*4+2] = axis * .50f + Vector3.up * .5f;
                vertices[i*4+3] = axis * .50f - Vector3.up * .5f;
                int p = i * 4, n = ((i + 1) % segments) * 4, t = i * 24;
                int[] faces = { p,n,p+1, p+1,n,n+1, p+2,p+3,n+2, p+3,n+3,n+2,
                    p,p+2,n, n,p+2,n+2, p+1,n+1,p+3, p+3,n+1,n+3 };
                System.Array.Copy(faces, 0, triangles, t, faces.Length);
            }
            var mesh = new Mesh { name = "Player collar", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
