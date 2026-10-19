using System.Collections.Generic;
using UnityEngine;

namespace Possession.LocalLevel04
{
    // Small deterministic grid for this greybox. Reads the actual closed gate colliders
    // each search, so newly opened passages become navigable without baking shared assets.
    public sealed class LocalLevel04Navigation : MonoBehaviour
    {
        public Collider[] barriers;
        private const float Cell = .75f, Clearance = .8f;
        private const int Width = 88, Height = 54;

        public bool Clear(Vector3 from, Vector3 to) => SegmentClear(from, to, Snapshot());

        private List<Bounds> Snapshot()
        {
            var result = new List<Bounds>();
            foreach (var barrier in barriers)
            {
                if (barrier == null || !barrier.enabled || !barrier.gameObject.activeInHierarchy) continue;
                Bounds bounds = barrier.bounds;
                bounds.Expand(new Vector3(Clearance * 2, 0, Clearance * 2));
                result.Add(bounds);
            }
            return result;
        }

        private static bool SegmentClear(Vector3 from, Vector3 to, List<Bounds> obstacles)
        {
            from.y = to.y = 2f;
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            var ray = new Ray(from, distance > .001f ? delta / distance : Vector3.forward);
            foreach (var bounds in obstacles)
            {
                if (bounds.Contains(from) || bounds.Contains(to)) return false;
                if (distance > .001f && bounds.IntersectRay(ray, out float hit) && hit <= distance)
                    return false;
            }
            return true;
        }

        public List<Vector3> FindPath(Vector3 from, Vector3 to)
        {
            var result = new List<Vector3>();
            var obstacles = Snapshot();
            if (SegmentClear(from, to, obstacles)) { result.Add(to); return result; }
            int start = Index(from), goal = Index(to);
            var parent = new int[Width * Height];
            var walkable = new bool[parent.Length];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = -1;
                walkable[i] = SegmentClear(Point(i), Point(i), obstacles);
            }
            start = Nearest(start); goal = Nearest(goal);
            if (start < 0 || goal < 0) return result;
            var queue = new Queue<int>();
            parent[start] = start;
            queue.Enqueue(start);
            while (queue.Count > 0 && parent[goal] == -1)
            {
                int current = queue.Dequeue(), x = current % Width, z = current / Width;
                Visit(x - 1, z, current); Visit(x + 1, z, current);
                Visit(x, z - 1, current); Visit(x, z + 1, current);
            }
            if (parent[goal] == -1) return result;
            int node = goal;
            while (node != start) { result.Add(Point(node)); node = parent[node]; }
            result.Reverse();
            if (!SegmentClear(from, Point(start), obstacles)) result.Insert(0, Point(start));
            // String-pull the grid route; collision clearance is checked along every shortcut.
            Vector3 anchor = from;
            var smooth = new List<Vector3>();
            for (int i = 0; i < result.Count;)
            {
                int far = i;
                while (far + 1 < result.Count && SegmentClear(anchor, result[far + 1], obstacles)) far++;
                anchor = result[far]; smooth.Add(anchor); i = far + 1;
            }
            if (SegmentClear(anchor, to, obstacles)) smooth.Add(to);
            return smooth;

            void Visit(int x, int z, int previous)
            {
                if (x < 0 || x >= Width || z < 0 || z >= Height) return;
                int next = z * Width + x;
                if (!walkable[next] || parent[next] != -1) return;
                parent[next] = previous;
                queue.Enqueue(next);
            }

            int Nearest(int index)
            {
                if (walkable[index]) return index;
                int best = -1;
                float distance = float.PositiveInfinity;
                for (int dz = -3; dz <= 3; dz++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int x = index % Width + dx, z = index / Width + dz;
                        if (x < 0 || x >= Width || z < 0 || z >= Height) continue;
                        int candidate = z * Width + x;
                        float squared = dx * dx + dz * dz;
                        if (walkable[candidate] && squared < distance) { distance = squared; best = candidate; }
                    }
                return best;
            }
        }

        private static int Index(Vector3 p) => Mathf.Clamp(Mathf.FloorToInt(p.z / Cell), 0, Height - 1)
            * Width + Mathf.Clamp(Mathf.FloorToInt(p.x / Cell), 0, Width - 1);
        private static Vector3 Point(int i) => new Vector3((i % Width + .5f) * Cell, 2f,
            (i / Width + .5f) * Cell);
    }
}
