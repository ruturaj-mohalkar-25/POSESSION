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
        private Transform collar, marker;
        private Mesh collarMesh, markerMesh;
        public Transform TrackedBody { get; private set; }
        public bool IsShowing => visuals != null && visuals.activeSelf;

        private void Awake() => EnsureVisuals();

        private void EnsureVisuals()
        {
            if (visuals != null) return;
            possession = GetComponent<PossessionController>();
            spirit = GetComponent<SpiritController>();
            visuals = new GameObject("Gold_PlayerIdentity") { layer = 2 };
            SceneManager.MoveGameObjectToScene(visuals, gameObject.scene);
            collarMesh = RingMesh();
            markerMesh = DiamondMesh();
            collar = MeshPart("Teal collar", collarMesh, Resources.Load<Material>("Gold/Teal"));
            marker = MeshPart("Player diamond", markerMesh, Resources.Load<Material>("Gold/Marker"));
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
            TrackedBody = possession != null && possession.PossessedEnemy != null
                ? possession.PossessedEnemy.transform : transform;
            var renderer = TrackedBody.GetComponent<Renderer>();
            bool alive = spirit != null && (!Application.isPlaying || spirit.CurrentHP > 0);
            if (possession != null && possession.PossessedEnemy != null)
                alive &= possession.PossessedEnemy.CurrentHP > 0;
            bool showing = alive && renderer != null && renderer.enabled && TrackedBody.gameObject.activeInHierarchy
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress);
            visuals.SetActive(showing);
            if (!showing) return;
            var bounds = renderer.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            collar.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * .32f, bounds.center.z);
            collar.localScale = new Vector3(radius * 2.1f, .14f, radius * 2.1f);
            marker.position = new Vector3(bounds.center.x, bounds.max.y + .75f, bounds.center.z);
            marker.localScale = Vector3.one * .5f;
        }

        private void OnDisable() { if (visuals != null) visuals.SetActive(false); }
        private void OnDestroy()
        {
            Release(visuals);
            Release(collarMesh);
            Release(markerMesh);
        }

        private static void Release(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private static Mesh DiamondMesh()
        {
            var mesh = new Mesh { name = "Player diamond" };
            mesh.vertices = new[] { Vector3.up, Vector3.right * .55f, Vector3.forward * .55f,
                Vector3.left * .55f, Vector3.back * .55f, Vector3.down };
            mesh.triangles = new[] { 0,2,1, 0,3,2, 0,4,3, 0,1,4, 5,1,2, 5,2,3, 5,3,4, 5,4,1 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
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
                vertices[i*4] = axis * .5f + Vector3.up * .5f;
                vertices[i*4+1] = axis * .5f - Vector3.up * .5f;
                vertices[i*4+2] = axis * .40f + Vector3.up * .5f;
                vertices[i*4+3] = axis * .40f - Vector3.up * .5f;
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
