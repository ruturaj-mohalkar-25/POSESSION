using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    // The possession-charge collectible's look: a glowing gold Soul Coin with a pale spirit core and a soft halo.
    // It hovers, spins and pulses, and bursts when the Spirit collects it.
    // The same look is used by the reusable prefab (for any level) and by Level_02's pickups.
    public static class SoulCoinPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/Puzzle/SoulCoin.prefab";
        const string Level02Path = "Assets/Scenes/Level_02.unity";
        const string MaterialFolder = "Assets/Art/Materials";
        const string ModelName = "Soul Coin";

        static readonly Color Gold = new Color(1f, 0.78f, 0.22f);
        static readonly Color GoldGlow = new Color(1f, 0.62f, 0.12f);
        static readonly Color SpiritWhite = new Color(0.92f, 0.95f, 1f);

        // Reusable one-use collectible for level designers to drop into a level
        [MenuItem("Possession/P3/Build Soul Coin Prefab")]
        public static void Build()
        {
            if (!NotPlaying()) return;

            var root = new GameObject("SoulCoin");
            try
            {
                var trigger = root.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.9f;
                trigger.center = new Vector3(0f, 1f, 0f);

                var pickup = root.AddComponent<PossessionChargePickup>();
                var so = new SerializedObject(pickup);
                so.FindProperty("reforms").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                DressAsSoulCoin(pickup);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // Swaps Level_02's existing pickups to the Soul Coin look (positions and re-form rules stay the same)
        // and gives its exit the same checkered finish mat as Level_01
        [MenuItem("Possession/P3/Apply Soul Coin Look to Level_02")]
        public static void ApplyToLevel02()
        {
            if (!NotPlaying() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Level02Path, OpenSceneMode.Single);
            var checker = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/P5_CheckerMat.mat");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var pickup in root.GetComponentsInChildren<PossessionChargePickup>(true))
                    DressAsSoulCoin(pickup);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "ExitPlatform" && checker != null && t.GetComponent<Renderer>() is Renderer r)
                        r.sharedMaterial = checker;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Replaces whatever model the pickup shows with the Soul Coin, and wires its glow and pickup flash
        public static void DressAsSoulCoin(PossessionChargePickup pickup)
        {
            var so = new SerializedObject(pickup);
            var shardProp = so.FindProperty("shard");
            var oldShard = shardProp.objectReferenceValue as Renderer;
            if (oldShard != null && oldShard.gameObject != pickup.gameObject) Object.DestroyImmediate(oldShard.gameObject);
            var oldModel = pickup.transform.Find(ModelName);
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

            // Halo is the part the pickup bobs, spins and hides; the coin and core ride inside it
            var halo = Part(pickup.transform, PrimitiveType.Sphere, ModelName, Vector3.up * 1.05f, Vector3.one * 0.95f,
                Quaternion.identity, HaloMat());
            halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Part(halo.transform, PrimitiveType.Cylinder, "Coin", Vector3.zero, new Vector3(0.68f, 0.05f, 0.68f),
                Quaternion.Euler(90f, 0f, 0f), LitMat("P5_SoulCoin", Gold, GoldGlow));
            Part(halo.transform, PrimitiveType.Cylinder, "Core", Vector3.zero, new Vector3(0.36f, 0.065f, 0.36f),
                Quaternion.Euler(90f, 0f, 0f), LitMat("P5_SoulCoinCore", SpiritWhite, new Color(0.55f, 0.6f, 0.7f)));

            shardProp.objectReferenceValue = halo.GetComponent<Renderer>();
            so.FindProperty("collectBurst").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/P5_FXFlash.mat");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Gentle golden pulse so the coin reads from the top-down camera
            var glow = pickup.GetComponent<HintGlow>();
            if (glow == null) glow = pickup.gameObject.AddComponent<HintGlow>();
            var gso = new SerializedObject(glow);
            gso.FindProperty("glow").colorValue = GoldGlow;
            gso.FindProperty("speed").floatValue = 2.6f;
            gso.FindProperty("min").floatValue = 0.35f;
            gso.FindProperty("max").floatValue = 1f;
            gso.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool NotPlaying()
        {
            if (!EditorApplication.isPlaying) return true;
            EditorUtility.DisplayDialog("Soul Coin", "Stop Play mode first, then run this again.", "OK");
            return false;
        }

        static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale,
            Quaternion localRotation, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material LitMat(string name, Color color, Color emission)
        {
            var mat = LoadOrCreate(name, "Standard");
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.55f);
            mat.SetFloat("_Metallic", 0.6f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Soft see-through glow around the coin
        static Material HaloMat()
        {
            var mat = LoadOrCreate("P5_SoulCoinHalo", "Sprites/Default");
            mat.color = new Color(1f, 0.82f, 0.35f, 0.18f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreate(string name, string shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
