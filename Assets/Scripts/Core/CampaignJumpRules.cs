using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Possession
{
    public static class CampaignJumpRules
    {
        public const float GroundSpeed = 5f;
        public const float CoverSpeed = 2.5f;
        public static float Speed(Scene scene, float configured, Collider support = null)
        {
            // Tutorial and two-storey stage retain their independently authored movement.
            if (Array.IndexOf(CampaignLevels.SceneNames, scene.name) < 0) return configured;
            return Mathf.Min(configured, SolidCover.Is(support) ? CoverSpeed : GroundSpeed);
        }
    }
    public static class SolidCover
    {
        public static bool Is(Collider collider)
        {
            if (collider == null) return false;
            if (collider.GetComponentInParent<HidingCrate>() != null) return true;
            for (var node = collider.transform; node != null; node = node.parent)
                if (node.name.StartsWith("Cover_", StringComparison.OrdinalIgnoreCase)
                    || node.name.IndexOf("Crate", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
