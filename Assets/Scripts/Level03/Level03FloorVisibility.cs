using UnityEngine;
using System.Collections.Generic;
namespace Possession
{
    // Only rendering changes: slabs and walls still block movement, projectiles, and sight.
    public sealed class Level03FloorVisibility : MonoBehaviour
    {
        private Level03Director level;
        private Renderer[] renderers;
        private float nextRefresh;
        private readonly HashSet<Renderer> hidden = new();
        private void LateUpdate()
        {
            if (level == null) level = FindFirstObjectByType<Level03Director>();
            if (level == null) return;
            if (renderers == null || Time.unscaledTime >= nextRefresh)
            { renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None); nextRefresh = Time.unscaledTime + .3f; }
            Transform player = level.Possession.PossessedEnemy != null ? level.Possession.PossessedEnemy.transform : level.Spirit.transform;
            foreach (var r in renderers)
            {
                if (r == null || r.gameObject.scene != gameObject.scene) continue;
                if (r.transform.parent != null && r.transform.parent.name.StartsWith("Stair_")) continue;
                bool above = r.bounds.center.y >= Level03Director.FloorHeight-.3f && player.position.y < Level03Director.FloorHeight+.35f
                    || r.bounds.center.y > player.position.y + 2.8f
                    || player.position.y > Level03Director.FloorHeight-1f && r.bounds.center.y < Level03Director.FloorHeight-1f;
                if (above && !r.forceRenderingOff) { r.forceRenderingOff = true; hidden.Add(r); }
                else if (!above && hidden.Remove(r)) r.forceRenderingOff = false;
            }
        }
        private void OnDisable() { foreach (var r in hidden) if (r != null) r.forceRenderingOff = false; hidden.Clear(); }
    }
}
