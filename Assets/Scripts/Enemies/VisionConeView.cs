using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Draws an enemy's vision cone flat on the ground so the player can see where it is
    // looking and sneak around it. The cone stops at walls, turns stronger red while the
    // enemy is chasing or hostile, and hides while the enemy is possessed.
    // Installed at runtime on every enemy so scenes and prefabs stay untouched.
    public sealed class VisionConeView : MonoBehaviour
    {
        private const int Segments = 24;
        private const float GroundOffset = 0.04f;
        private const string ShaderName = "Possession/P3/ReadabilityDust";

        private static readonly Color CalmColor = new Color(1f, 0.25f, 0.2f, 0.16f);
        private static readonly Color AlertColor = new Color(1f, 0.1f, 0.08f, 0.34f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly RaycastHit[] hits = new RaycastHit[8];
        private EnemyBase enemy;
        private CharacterController body;
        private GameObject cone;
        private Mesh mesh;
        private Material material;
        private Vector3[] vertices;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            body = GetComponent<CharacterController>();

            // Separate object so the cone never counts as part of the enemy's body
            // (tints, colliders and renderers that look at the enemy's children).
            cone = new GameObject(name + " Vision Cone");
            mesh = new Mesh { name = "Vision Cone" };
            mesh.MarkDynamic();
            vertices = new Vector3[Segments + 2];
            var triangles = new int[Segments * 3];
            for (int i = 0; i < Segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            cone.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = cone.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            Shader shader = Shader.Find(ShaderName);
            if (shader != null)
            {
                material = new Material(shader);
                meshRenderer.sharedMaterial = material;
            }
        }

        private void OnEnable()
        {
            if (cone != null) cone.SetActive(true);
        }

        private void OnDisable()
        {
            if (cone != null) cone.SetActive(false);
        }

        private void OnDestroy()
        {
            if (cone != null) Destroy(cone);
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }

        private void LateUpdate()
        {
            if (enemy == null || cone == null) return;

            bool show = !enemy.IsPossessed && enemy.CurrentHP > 0;
            if (cone.activeSelf != show) cone.SetActive(show);
            if (!show) return;

            float groundY = body != null ? body.bounds.min.y : transform.position.y;
            Vector3 eye = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            cone.transform.SetPositionAndRotation(
                new Vector3(eye.x, groundY + GroundOffset, eye.z),
                Quaternion.LookRotation(forward));

            float half = enemy.VisionAngle * 0.5f;
            vertices[0] = Vector3.zero;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)Segments);
                Vector3 local = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 world = cone.transform.rotation * local;
                vertices[i + 1] = local * VisibleDistance(eye, world, enemy.DetectionRange);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            if (material != null)
                material.SetColor(ColorId, enemy.IsChasing || enemy.IsHostile ? AlertColor : CalmColor);
        }

        // How far the enemy can see along a direction before a wall blocks it.
        // Enemies and the Spirit do not cut the cone short.
        private float VisibleDistance(Vector3 eye, Vector3 direction, float range)
        {
            int count = Physics.RaycastNonAlloc(eye, direction, hits, range, ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = range;
            for (int i = 0; i < count; i++)
            {
                Collider other = hits[i].collider;
                if (other.GetComponentInParent<EnemyBase>() != null) continue;
                if (other.GetComponentInParent<SpiritController>() != null) continue;
                if (hits[i].distance < nearest) nearest = hits[i].distance;
            }
            return nearest;
        }
    }

    public static class VisionConeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                    if (enemy.GetComponent<VisionConeView>() == null)
                        enemy.gameObject.AddComponent<VisionConeView>();
        }
    }
}
