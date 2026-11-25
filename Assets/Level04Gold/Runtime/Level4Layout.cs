using UnityEngine;

namespace Possession.Level04Gold
{
    // Convert original plan coordinates once at authoring boundaries. Physical body sizes,
    // movement speeds, possession/interaction ranges and floor heights do not use this map.
    public static class Level4Layout
    {
        public const float GeometryScale = 1.25f;
        public const float CorridorWidth = 6f;
        // Clears 1.7 m cover while keeping a second jump below the 4.5 m room boundary.
        public const float SpiritJumpSpeed = 6.5f;

        public static Vector3 Map(Vector3 point) =>
            new Vector3(point.x * GeometryScale, point.y, point.z * GeometryScale);

        public static Vector3 MapSize(Vector3 size) =>
            new Vector3(size.x * GeometryScale, size.y, size.z * GeometryScale);

        public static Bounds MapBounds(Bounds bounds) =>
            new Bounds(Map(bounds.center), MapSize(bounds.size));
    }
}
