using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Possession
{
    // Resolve overlapping floor rectangles in rendering only; every original collider is retained.
    public sealed class GoldFloorSurface : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new();
        private MeshRenderer[] originals;
        private bool[] enabledBefore;
        public IReadOnlyList<Rect> VisiblePatches => patches;
        private readonly List<Rect> patches = new();

        public void Build(MeshRenderer[] floors)
        {
            originals = floors;
            enabledBefore = floors.Select(f => f.enabled).ToArray();
            // Sort small rooms before large connecting floors, with a stable tie break.
            foreach (var height in floors.GroupBy(f => Mathf.RoundToInt(f.bounds.max.y * 1000f)))
            {
                var sources = height.OrderBy(f => f.bounds.size.x * f.bounds.size.z).ThenBy(f => f.name).ToArray();
                var xs = sources.SelectMany(f => new[] { f.bounds.min.x, f.bounds.max.x }).Distinct().OrderBy(x => x).ToArray();
                var zs = sources.SelectMany(f => new[] { f.bounds.min.z, f.bounds.max.z }).Distinct().OrderBy(z => z).ToArray();
                var batches = new Dictionary<Material, List<Rect>>();
                for (int x = 0; x < xs.Length - 1; x++) for (int z = 0; z < zs.Length - 1; z++)
                {
                    if (xs[x + 1] - xs[x] < .0001f || zs[z + 1] - zs[z] < .0001f) continue;
                    var middle = new Vector3((xs[x] + xs[x + 1]) * .5f, sources[0].bounds.center.y, (zs[z] + zs[z + 1]) * .5f);
                    var owner = sources.FirstOrDefault(f => middle.x > f.bounds.min.x && middle.x < f.bounds.max.x
                        && middle.z > f.bounds.min.z && middle.z < f.bounds.max.z);
                    if (owner == null) continue;
                    var rect = Rect.MinMaxRect(xs[x], zs[z], xs[x + 1], zs[z + 1]);
                    if (!batches.TryGetValue(owner.sharedMaterial, out var list)) batches[owner.sharedMaterial] = list = new List<Rect>();
                    list.Add(rect); patches.Add(rect);
                }
                foreach (var batch in batches) CreateMesh(batch.Key, batch.Value, height.Key / 1000f);
            }
            foreach (var floor in floors) floor.enabled = false;
        }

        private void CreateMesh(Material material, List<Rect> rects, float y)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            foreach (var r in rects)
            {
                int first = vertices.Count;
                var corners = new[] { new Vector3(r.xMin, y, r.yMin), new Vector3(r.xMin, y, r.yMax),
                    new Vector3(r.xMax, y, r.yMax), new Vector3(r.xMax, y, r.yMin) };
                foreach (var p in corners) { vertices.Add(transform.InverseTransformPoint(p)); uv.Add(new Vector2(p.x, p.z) / 4f); }
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            var mesh = new Mesh { name = "Non-overlapping " + material.name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes.Add(mesh);
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = 2 };
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void OnDestroy()
        {
            if (originals != null) for (int i = 0; i < originals.Length; i++)
                if (originals[i] != null) originals[i].enabled = enabledBefore[i];
            foreach (var mesh in meshes)
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
