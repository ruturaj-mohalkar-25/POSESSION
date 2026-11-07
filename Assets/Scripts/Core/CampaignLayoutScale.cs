using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Authoring metadata only: actors and shared gameplay retain their physical size/ranges.
    [DisallowMultipleComponent]
    public sealed class CampaignLayoutScale : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float horizontalScale = 1f;
        public float HorizontalScale => horizontalScale;
        public void RecordAppliedScale(float scale) => horizontalScale = scale;

        public static float ForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return 1f;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<CampaignLayoutScale>() is CampaignLayoutScale layout)
                    return layout.HorizontalScale;
            return 1f;
        }

        public static Vector3 FromOriginal(Scene scene, Vector3 point)
        {
            float scale = ForScene(scene);
            return new Vector3(point.x * scale, point.y, point.z * scale);
        }
    }
}
