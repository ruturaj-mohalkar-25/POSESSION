using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // With no possession charges left the Spirit turns black with white eyes, so it reads at a glance
    // that it cannot possess; it goes back to white as soon as it collects a charge.
    // Only the body and its modelled parts change; the eject shield and charge orbs are left alone.
    [RequireComponent(typeof(SpiritController))]
    public sealed class SpiritChargeLook : MonoBehaviour
    {
        private static readonly Color EmptyBody = new Color(0.07f, 0.07f, 0.09f);
        private static readonly Color EmptyFeature = new Color(0.95f, 0.95f, 1f);
        private static readonly HashSet<string> FeatureNames = new HashSet<string> { "EyeL", "EyeR", "Mouth" };
        const string ModelName = "P5_Model";

        private PossessionCharge charge;
        private readonly List<Renderer> bodyParts = new List<Renderer>();
        private readonly List<Renderer> featureParts = new List<Renderer>();
        private readonly Dictionary<Renderer, Material> originals = new Dictionary<Renderer, Material>();
        private Material emptyBody;
        private Material emptyFeature;
        private bool showingEmpty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AddToSpirit();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AddToSpirit();

        private static void AddToSpirit()
        {
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            if (spirit != null && spirit.GetComponent<SpiritChargeLook>() == null)
                spirit.gameObject.AddComponent<SpiritChargeLook>();
        }

        private void Start()
        {
            charge = GetComponent<PossessionCharge>();
            if (charge == null) { enabled = false; return; } // levels without charges keep the white Spirit

            var body = GetComponent<Renderer>();
            if (body != null) bodyParts.Add(body);
            var model = transform.Find(ModelName);
            if (model != null)
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    (FeatureNames.Contains(r.name) ? featureParts : bodyParts).Add(r);

            foreach (var r in bodyParts) originals[r] = r.sharedMaterial;
            foreach (var r in featureParts) originals[r] = r.sharedMaterial;

            emptyBody = RuntimeMaterials.Lit(EmptyBody);
            emptyBody.SetFloat("_Glossiness", 0.1f);
            emptyFeature = RuntimeMaterials.Lit(EmptyFeature, EmptyFeature * 0.8f); // eyes stay bright on the dark body
        }

        private void LateUpdate()
        {
            bool empty = charge.Count <= 0;
            if (empty == showingEmpty) return;
            showingEmpty = empty;

            foreach (var r in bodyParts)
                if (r != null) r.sharedMaterial = empty ? emptyBody : originals[r];
            foreach (var r in featureParts)
                if (r != null) r.sharedMaterial = empty ? emptyFeature : originals[r];
        }

        private void OnDestroy()
        {
            if (emptyBody != null) Destroy(emptyBody);
            if (emptyFeature != null) Destroy(emptyFeature);
        }
    }
}
