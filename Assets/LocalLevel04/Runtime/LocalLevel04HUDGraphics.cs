using UnityEngine;
using UnityEngine.UI;

namespace Possession.LocalLevel04
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LocalLevel04Stars : MaskableGraphic
    {
        public int count;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            for (int star = 0; star < 3; star++)
            {
                Vector2 centre = new Vector2(r.x + r.width * (star + .5f) / 3, r.center.y);
                int first = mesh.currentVertCount;
                Color tint = star < count ? LocalLevel04HUD.Amber : new Color(.17f, .25f, .29f);
                mesh.AddVert(centre, tint, Vector2.zero);
                for (int i = 0; i < 10; i++)
                {
                    float angle = Mathf.PI * .5f + i * Mathf.PI / 5;
                    float radius = i % 2 == 0 ? 17 : 7.5f;
                    mesh.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                }
                for (int i = 0; i < 10; i++) mesh.AddTriangle(first, first + i + 1, first + (i + 1) % 10 + 1);
            }
        }
    }

    // UI geometry stays vector-based: no textures or extra art assets are needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LocalLevel04Ring : MaskableGraphic
    {
        [Range(0, 1)] public float value = 1;
        public float thickness = 5;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            int segments = Mathf.Max(1, Mathf.CeilToInt(72 * value));
            Vector2 centre = rectTransform.rect.center;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * .5f - Mathf.PI * 2 * value * i / segments;
                float b = Mathf.PI * .5f - Mathf.PI * 2 * value * (i + 1) / segments;
                Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = mesh.currentVertCount;
                mesh.AddVert(centre + da * radius, color, Vector2.zero);
                mesh.AddVert(centre + db * radius, color, Vector2.zero);
                mesh.AddVert(centre + db * (radius - thickness), color, Vector2.zero);
                mesh.AddVert(centre + da * (radius - thickness), color, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LocalLevel04SignalMap : MaskableGraphic
    {
        public LocalLevel04Challenge level;
        public PossessionController player;
        public Transform spirit;
        private void LateUpdate() => SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (level == null || player == null) return;
            Rect area = rectTransform.rect;
            for (int i = 1; i < 6; i++)
            {
                Quad(mesh, new Rect(area.x + area.width * i / 6, area.y, 1, area.height), new Color(.1f, .22f, .26f, .4f));
                Quad(mesh, new Rect(area.x, area.y + area.height * i / 6, area.width, 1), new Color(.1f, .22f, .26f, .4f));
            }
            foreach (var wall in level.navigation.barriers)
            {
                if (wall == null || !wall.enabled) continue;
                Bounds b = wall.bounds;
                Vector2 p = Project(b.min, area);
                bool gate = wall.GetComponent<LocalLevel04Gate>() != null;
                Quad(mesh, new Rect(p.x, p.y, Mathf.Max(2, b.size.x / 66 * area.width),
                    Mathf.Max(2, b.size.z / 40 * area.height)), gate ? new Color(.92f, .61f, .23f) : new Color(.31f, .43f, .49f));
            }
            foreach (var target in level.targets)
                Dot(mesh, Project(target.transform.position, area), target.IsHit ? LocalLevel04HUD.Mint : LocalLevel04HUD.Red, 5);
            foreach (var host in level.hosts)
                if (host != null && host.gameObject.activeInHierarchy)
                    Dot(mesh, Project(host.transform.position, area), host is HeavyEnemy ? LocalLevel04HUD.Amber : LocalLevel04HUD.Blue, 5);
            Dot(mesh, Project(level.finish.position, area), LocalLevel04HUD.Mint, 7);
            Transform active = player.PossessedEnemy != null ? player.PossessedEnemy.transform : spirit;
            Vector2 point = Project(active.position, area);
            float angle = -active.eulerAngles.y * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            Vector2 right = new Vector2(direction.y, -direction.x);
            int n = mesh.currentVertCount;
            mesh.AddVert(point + direction * 7, Color.white, Vector2.zero);
            mesh.AddVert(point - direction * 4 + right * 4, Color.white, Vector2.zero);
            mesh.AddVert(point - direction * 4 - right * 4, Color.white, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2);
            Vector3 objective = level.Stage == 0 ? level.targets[0].transform.position
                : level.Stage == 1 ? level.gates[1].transform.position
                : level.Stage == 2 ? level.targets[1].transform.position
                : level.Stage == 3 ? level.gates[3].transform.position : level.finish.position;
            Vector2 beacon = Project(objective, area);
            float pulse = 7 + Mathf.Sin(Time.unscaledTime * 3) * 2;
            Color signal = LocalLevel04HUD.Mint; signal.a = .6f;
            Quad(mesh, new Rect(beacon.x - pulse, beacon.y - pulse, pulse * 2, 1.5f), signal);
            Quad(mesh, new Rect(beacon.x - pulse, beacon.y + pulse, pulse * 2, 1.5f), signal);
            Quad(mesh, new Rect(beacon.x - pulse, beacon.y - pulse, 1.5f, pulse * 2), signal);
            Quad(mesh, new Rect(beacon.x + pulse, beacon.y - pulse, 1.5f, pulse * 2), signal);
        }
        private static Vector2 Project(Vector3 p, Rect r) => new Vector2(r.x + p.x / 66 * r.width, r.y + p.z / 40 * r.height);
        private static void Dot(VertexHelper mesh, Vector2 point, Color color, float size) =>
            Quad(mesh, new Rect(point.x - size / 2, point.y - size / 2, size, size), color);
        private static void Quad(VertexHelper mesh, Rect rect, Color color)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(new Vector2(rect.x, rect.y), color, Vector2.zero);
            mesh.AddVert(new Vector2(rect.x, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.y), color, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
