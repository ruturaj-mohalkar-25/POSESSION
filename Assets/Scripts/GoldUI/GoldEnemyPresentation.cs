using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Adds silhouette-only visual pieces at runtime. The visuals use no colliders and
    // do not alter the enemy transform, renderer, movement, vision, or abilities.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBase))]
    public sealed class GoldEnemyPresentation : MonoBehaviour
    {
        private readonly List<Renderer> pieces = new List<Renderer>();
        private readonly List<Material> materials = new List<Material>();
        private EnemyBase enemy;
        private bool built;

        public bool IsBuilt => built;
        public int VisualPieceCount => pieces.Count;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            Build();
        }

        public void Build()
        {
            if (built) return;
            if (enemy == null) enemy = GetComponent<EnemyBase>();
            if (enemy == null) return;

            if (enemy is GuardEnemy) BuildGuard();
            else if (enemy is HeavyEnemy) BuildHeavy();
            else return;

            built = true;
        }

        private void BuildGuard()
        {
            Material cloth = Own(RuntimeMaterials.Lit(new Color(.06f, .24f, .28f)));
            Material badge = Own(RuntimeMaterials.Lit(GoldTheme.Teal,
                GoldTheme.Teal * .25f));

            AddPiece("Guard Cap", PrimitiveType.Cylinder, new Vector3(0f, 1.03f, 0f),
                new Vector3(.44f, .10f, .44f), cloth);
            AddPiece("Guard Cap Brim", PrimitiveType.Cube, new Vector3(0f, .99f, .28f),
                new Vector3(.40f, .055f, .28f), cloth);
            AddPiece("Guard Badge", PrimitiveType.Cube, new Vector3(0f, .30f, .48f),
                new Vector3(.20f, .22f, .035f), badge);
        }

        private void BuildHeavy()
        {
            Material armour = Own(RuntimeMaterials.Lit(new Color(.19f, .21f, .24f)));
            Material accent = Own(RuntimeMaterials.Lit(new Color(.72f, .34f, .12f),
                new Color(.16f, .055f, .015f)));

            AddPiece("Heavy Left Shoulder", PrimitiveType.Cube, new Vector3(-.54f, .52f, .02f),
                new Vector3(.34f, .24f, .50f), armour);
            AddPiece("Heavy Right Shoulder", PrimitiveType.Cube, new Vector3(.54f, .52f, .02f),
                new Vector3(.34f, .24f, .50f), armour);
            AddPiece("Heavy Chest Plate", PrimitiveType.Cube, new Vector3(0f, .22f, .51f),
                new Vector3(.58f, .52f, .075f), armour);
            AddPiece("Heavy Chest Mark", PrimitiveType.Cube, new Vector3(0f, .23f, .555f),
                new Vector3(.18f, .34f, .025f), accent);
            AddPiece("Heavy Belt", PrimitiveType.Cube, new Vector3(0f, -.27f, 0f),
                new Vector3(.70f, .10f, .56f), accent);
        }

        private void AddPiece(string pieceName, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material)
        {
            var piece = new GameObject(pieceName, typeof(MeshFilter), typeof(MeshRenderer));
            piece.layer = 2; // Ignore Raycast; presentation must never affect targeting.
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = scale;

            var filter = piece.GetComponent<MeshFilter>();
            filter.sharedMesh = Resources.GetBuiltinResource<Mesh>(MeshName(type));
            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            pieces.Add(renderer);
        }

        private static string MeshName(PrimitiveType type)
        {
            return type switch
            {
                PrimitiveType.Sphere => "Sphere.fbx",
                PrimitiveType.Capsule => "Capsule.fbx",
                PrimitiveType.Cylinder => "Cylinder.fbx",
                PrimitiveType.Plane => "Plane.fbx",
                PrimitiveType.Quad => "Quad.fbx",
                _ => "Cube.fbx"
            };
        }

        private Material Own(Material material)
        {
            materials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (var material in materials)
                if (material != null) Destroy(material);
        }
    }

    public static class GoldEnemyPresentationInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Tutorial presentation belongs to Sushma this week.
            if (scene.name == TutorialScene.Name) return;

            foreach (var root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                    if (enemy.GetComponent<GoldEnemyPresentation>() == null)
                        enemy.gameObject.AddComponent<GoldEnemyPresentation>();
        }
    }
}
