using UnityEngine;

namespace Possession
{
    // Materials for visuals built at runtime. Shader.Find("Standard") returns null under URP/HDRP,
    // so each lookup falls back to that pipeline's equivalent and warns once instead of going magenta.
    public static class RuntimeMaterials
    {
        private static readonly string[] LitShaders =
            { "Standard", "Universal Render Pipeline/Lit", "HDRP/Lit", "Sprites/Default" };
        private static readonly string[] TransparentShaders =
            { "Sprites/Default", "Universal Render Pipeline/Unlit", "Unlit/Color" };

        private static Shader lit;
        private static Shader transparent;
        private static bool warnedLit;
        private static bool warnedTransparent;

        // Lit surface; with an emission colour it glows
        public static Material Lit(Color color, Color? emission = null)
        {
            if (lit == null) lit = Find(LitShaders, ref warnedLit);
            var mat = new Material(lit) { color = color };
            if (emission.HasValue) SetEmission(mat, emission.Value);
            return mat;
        }

        // Unlit, alpha-blended (halos and soft glows)
        public static Material Transparent(Color color)
        {
            if (transparent == null) transparent = Find(TransparentShaders, ref warnedTransparent);
            return new Material(transparent) { color = color };
        }

        public static void SetEmission(Material mat, Color emission)
        {
            if (mat == null) return;
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
        }

        private static Shader Find(string[] names, ref bool warned)
        {
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                if (name != names[0] && !warned)
                {
                    warned = true;
                    Debug.LogWarning($"RuntimeMaterials: shader '{names[0]}' not found, using '{name}' instead.");
                }
                return shader;
            }
            if (!warned)
            {
                warned = true;
                Debug.LogWarning($"RuntimeMaterials: none of the shaders [{string.Join(", ", names)}] were found; " +
                    "runtime pickups may render magenta.");
            }
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
