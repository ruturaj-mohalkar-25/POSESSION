using UnityEngine;
namespace Possession
{
    public static class Level03AimPlane
    {
        public static float Height(Transform body)
        {
            if (body.gameObject.scene.name != "Level_03") return 0f;
            // The ramp continuously connects the two floors; use the actor's feet while on it.
            return Mathf.Clamp(body.position.y-1.15f,0f,Level03Director.FloorHeight);
        }
    }
}
