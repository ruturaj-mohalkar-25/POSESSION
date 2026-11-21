using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Dressing is visual only, except explicitly marked solid hiding crates.
    [DefaultExecutionOrder(1100)]
    public sealed class GoldRoomDecor : MonoBehaviour
    {
        private readonly List<Material> materials = new();
        private readonly List<(Renderer wall, GameObject prop)> mounted = new();
        private readonly List<Vector3> occupied = new();
        private readonly List<(Vector3 point, Vector3 outward)> wallArtCenters = new();
        private readonly List<(Vector3 from, Vector3 to)> patrolSegments = new();
        private Material cream, ink, leaf, stem, rug, cardinal, gold;
        private int cornerProps, artworkVariant;
        private Transform[] objectives;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void FirstScene() => Install(SceneManager.GetActiveScene());
        private static void Loaded(Scene scene, LoadSceneMode mode)
        { if (CampaignLevels.NumberFor(scene.name) == 0) Install(scene); }

        public static void Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            var roots = scene.GetRootGameObjects();
            if (!roots.Any(r => r.GetComponentInChildren<SpiritController>(true) != null)
                || roots.Any(r => r.GetComponent<GoldRoomDecor>() != null)) return;
            var go = new GameObject("Gold_RoomDecor");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<GoldRoomDecor>().Build(scene);
        }

        private Material Paint(Color color)
        {
            var m = new Material(Resources.Load<Material>("Gold/Wall"));
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .12f);
            materials.Add(m); return m;
        }
        private void Build(Scene scene)
        {
            cream = Paint(new Color(.94f,.9f,.78f)); ink = Paint(new Color(.065f,.085f,.11f));
            leaf = Paint(new Color(.19f,.40f,.24f)); stem = Paint(new Color(.22f,.28f,.15f));
            cardinal = Paint(new Color(.6f,0f,0f)); gold = Paint(new Color(1f,.8f,0f));
            rug = Paint(new Color(.34f,.035f,.065f));
            artworkVariant = Mathf.Max(0,CampaignLevels.NumberFor(scene.name)-1)*2;
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
            var floors = all.Where(r => r.GetComponent<Collider>() != null && r.bounds.size.y < .6f
                && r.name.ToLowerInvariant().Contains("floor")).OrderBy(r => r.name).ToArray();
            var walls = all.Where(r => r.GetComponent<Collider>() != null && r.bounds.size.y >= 3f
                && r.GetComponentInParent<EnemyBase>() == null && r.GetComponentInParent<Door>() == null
                && r.GetComponentInParent<BreakableWall>() == null && r.GetComponentInParent<Target>() == null).ToArray();
            objectives = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.GetComponent<Door>() != null || t.GetComponent<Target>() != null
                    || t.GetComponent<BreakableWall>() != null || t.GetComponent<LevelExit>() != null
                    || t.GetComponent<EnemyBase>() != null || t.GetComponent<SpiritController>() != null
                    || t.GetComponent<PossessionChargePickup>() != null || t.GetComponent<HealthPickup>() != null
                    || IsWaypoint(t)).ToArray();
            BuildPatrolClearance(scene);
            occupied.AddRange(all.Where(r => r.name == "Gold_CornerCabinet").Select(r => r.bounds.center));
            Physics.SyncTransforms();
            int room = 0;
            foreach (var floor in floors.Where(f => f.bounds.size.x >= 4f && f.bounds.size.z >= 4f))
            {
                var b = floor.bounds;
                Mount(walls,b,room);
                // Visit every real corner, including the near wall, rather than only two corners
                // in the first seven rooms. Floor seams and openings fail the two-wall check.
                foreach (float x in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
                {
                    Vector3 at = new Vector3(x < 0f ? b.min.x + 1.25f : b.max.x - 1.25f,
                        b.max.y, z < 0f ? b.min.z + 1.25f : b.max.z - 1.25f);
                    if (!Corner(at, Vector3.right * x, Vector3.forward * z) || !Clear(at,2.5f)) continue;
                    CornerProp(at); occupied.Add(at);
                }
                var mat = new Vector3(b.min.x+2.2f,b.max.y+.035f,b.center.z);
                if (Clear(mat,3f) && !Physics.CheckBox(mat+Vector3.up*1.1f,new Vector3(1.45f,1.1f,1f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    Mat(mat);
                room++;
            }
        }
        private bool Clear(Vector3 point,float distance) => !objectives.Any(t => Flat(t.position,point)<distance)
            && !occupied.Any(p => Flat(p,point)<2.4f)
            && !patrolSegments.Any(segment => DistanceToSegment(point, segment.from, segment.to) < 2.2f);
        private static float Flat(Vector3 a,Vector3 b) => Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        private static bool Corner(Vector3 p,Vector3 side,Vector3 end) =>
            SolidWall(p+Vector3.up*.7f,side)
            && SolidWall(p+Vector3.up*.7f,end)
            && !Physics.CheckBox(p+Vector3.up*.85f,new Vector3(.65f,.8f,.65f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);

        private static bool SolidWall(Vector3 at,Vector3 direction) =>
            Physics.Raycast(at,direction,out var hit,1.7f,1,QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<Door>() == null
            && hit.collider.GetComponentInParent<BreakableWall>() == null
            && hit.collider.GetComponentInParent<EnemyBase>() == null
            && hit.collider.GetComponentInParent<SpiritController>() == null;

        private static bool IsWaypoint(Transform t) => t.name.ToLowerInvariant().Contains("waypoint")
            || t.name.Contains("_WP");

        private void BuildPatrolClearance(Scene scene)
        {
            // Authored routes use either a Waypoint_* group or <enemy>_WP0/1 siblings.
            // Keep plants clear of whole route segments, not only their endpoints.
            var points = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(IsWaypoint).ToArray();
            foreach (var group in points.GroupBy(t => (t.parent,
                t.name.Contains("_WP") ? t.name.Substring(0,t.name.LastIndexOf("_WP")) : string.Empty)))
            {
                var route = group.OrderBy(t => t.GetSiblingIndex()).ToArray();
                for (int i = 0; i < route.Length; i++)
                    patrolSegments.Add((route[i].position,route[(i + 1) % route.Length].position));
            }
        }

        private static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b)
        {
            var start = new Vector2(a.x,a.z); var span = new Vector2(b.x-a.x,b.z-a.z);
            var point = new Vector2(p.x,p.z);
            float t = span.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(point-start,span)/span.sqrMagnitude) : 0f;
            return Vector2.Distance(point,start+span*t);
        }

        private void Mount(MeshRenderer[] walls,Bounds room,int index)
        {
            // Local -Z is the picture front. Turn it toward each room, including side walls.
            foreach (var outward in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left})
            {
                bool horizontal=Mathf.Abs(outward.z)>.5f;
                foreach (var wall in walls.OrderBy(w => w.name))
                {
                    Bounds b=wall.bounds;
                    if (horizontal ? b.size.z>1.5f : b.size.x>1.5f) continue;
                    float face=horizontal ? (outward.z>0?b.min.z:b.max.z) : (outward.x>0?b.min.x:b.max.x);
                    float boundary=horizontal ? (outward.z>0?room.max.z:room.min.z) : (outward.x>0?room.max.x:room.min.x);
                    if(Mathf.Abs(face-boundary)>1f)continue;
                    float left=Mathf.Max(horizontal?b.min.x:b.min.z,horizontal?room.min.x:room.min.z)+2.1f;
                    float right=Mathf.Min(horizontal?b.max.x:b.max.z,horizontal?room.max.x:room.max.z)-2.1f;
                    if(right<left)continue;
                    // Fill long uninterrupted spans, with edge alternatives around doors/nameplates.
                    int slots=Mathf.Max(1,Mathf.FloorToInt((right-left+4.2f)/5.2f));
                    var candidates=new List<float>();
                    for(int i=0;i<slots;i++)candidates.Add(Mathf.Lerp(left,right,(i+.5f)/slots));
                    candidates.Add(left);candidates.Add(right);
                    foreach(float along in candidates)
                    {
                        Vector3 p=horizontal ? new Vector3(along,0,face) : new Vector3(face,0,along);
                        p-=outward*.13f;
                        p.y=Mathf.Min(room.max.y+2.7f,b.max.y-1.4f);
                        if(p.y-1.35f<room.max.y+.6f)continue;
                        if(objectives.Any(t=>Flat(t.position,p)<3f)
                            ||wallArtCenters.Any(c=>Vector3.Dot(c.outward,outward)>.5f&&Flat(c.point,p)<4.4f))continue;
                        if(wall.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Gold_RoomSign"&&Flat(t.position,p)<4.1f))continue;
                        bool tv=(index+mounted.Count)%3==1;
                        var root=Root(tv?"Decor_WallTV":"Decor_Painting",p);
                        root.rotation=Quaternion.LookRotation(outward);
                        root.SetParent(wall.transform,true);
                        if(tv)TV(root);else Painting(root,artworkVariant++);
                        mounted.Add((wall,root.gameObject));wallArtCenters.Add((p,outward));
                    }
                }
            }
        }
        private Transform Root(string name,Vector3 position)
        {
            var t=new GameObject(name).transform;t.gameObject.layer=2;t.SetParent(transform,false);t.position=position;return t;
        }
        private GameObject Part(Transform root,string name,PrimitiveType kind,Vector3 position,Vector3 size,Material m)
        {
            var part=GoldRoomPresentation.Part(root,name,kind,root.TransformPoint(position),size,m);
            part.transform.localRotation=Quaternion.identity;
            return part;
        }
        private void Painting(Transform root,int variant)
        {
            Part(root,"Gold gallery frame",PrimitiveType.Cube,Vector3.zero,new Vector3(3.9f,2.7f,.12f),gold);
            Part(root,"Cardinal art board",PrimitiveType.Cube,new Vector3(0,0,-.075f),new Vector3(3.66f,2.46f,.035f),cardinal);
            Part(root,"Gold rule",PrimitiveType.Cube,new Vector3(0,-.77f,-.11f),new Vector3(2.95f,.025f,.02f),gold);
            root.gameObject.AddComponent<UscWallArt>().Build(variant,false);
        }
        private void TV(Transform root)
        {
            Part(root,"Thin bezel",PrimitiveType.Cube,Vector3.zero,new Vector3(3.9f,2.5f,.14f),ink);
            Part(root,"Cardinal screen",PrimitiveType.Cube,new Vector3(0,.02f,-.085f),new Vector3(3.64f,2.24f,.025f),cardinal);
            Part(root,"Gold screen trim",PrimitiveType.Cube,new Vector3(0,-.9f,-.11f),new Vector3(3.2f,.035f,.015f),gold);
            root.gameObject.AddComponent<UscWallArt>().Build(0,true);
        }
        private void CornerProp(Vector3 at)
        { if (cornerProps++ % 2 == 0) Plant(at); else Vase(at); }

        private void Plant(Vector3 at)
        {
            var r=Root("Decor_CornerPlant",at);
            Part(r,"Cardinal planter",PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(.72f,.3f,.72f),cardinal);
            Part(r,"Gold planter rim",PrimitiveType.Cylinder,new Vector3(0,.59f,0),new Vector3(.75f,.035f,.75f),gold);
            Part(r,"Soil",PrimitiveType.Cylinder,new Vector3(0,.61f,0),new Vector3(.63f,.015f,.63f),ink);
            Part(r,"Stem",PrimitiveType.Cylinder,new Vector3(0,1.05f,0),new Vector3(.055f,.45f,.055f),stem);
            for(int i=0;i<7;i++)
            {
                float angle=i*137.5f*Mathf.Deg2Rad;
                var l=Part(r,"Leaf",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*.28f,.85f+i*.12f,Mathf.Sin(angle)*.28f),new Vector3(.8f,.16f,.32f),leaf);
                l.transform.rotation=Quaternion.Euler(0,-i*137.5f,-18f);
            }
        }
        private void Vase(Vector3 at)
        {
            var r=Root("Decor_CornerVase",at);
            Part(r,"Ceramic body",PrimitiveType.Sphere,new Vector3(0,.48f,0),new Vector3(.7f,.95f,.7f),cream);
            Part(r,"Vase neck",PrimitiveType.Cylinder,new Vector3(0,.99f,0),new Vector3(.25f,.23f,.25f),gold);
            Part(r,"Dark opening",PrimitiveType.Cylinder,new Vector3(0,1.23f,0),new Vector3(.2f,.008f,.2f),ink);
            for(int i=0;i<3;i++)
            {
                var reed=Part(r,"Artificial stem",PrimitiveType.Cylinder,new Vector3((i-1)*.09f,1.48f,0),new Vector3(.025f,.38f,.025f),stem);
                reed.transform.rotation=Quaternion.Euler(0,0,(i-1)*-12f);
                for(int j=0;j<3;j++)
                {
                    var l=Part(r,"Artificial leaf",PrimitiveType.Sphere,
                        new Vector3((i-1)*.18f+(j%2==0?.16f:-.16f),1.4f+j*.18f,0),new Vector3(.43f,.1f,.2f),leaf);
                    l.transform.rotation=Quaternion.Euler(0,i*53f,(j%2==0?1f:-1f)*23f);
                }
            }
        }
        private void Mat(Vector3 at)
        {
            var r=Root("Decor_HidingCrate",at-Vector3.up*.035f);
            HidingCrate.Build(r);
        }
        private void LateUpdate()
        {
            foreach(var item in mounted)
            {
                if(item.prop==null)continue;
                var m=item.wall!=null?item.wall.sharedMaterial:null;
                bool faded=m!=null && m.HasProperty("_P5OcclusionAlpha") && m.GetFloat("_P5OcclusionAlpha")<.98f;
                item.prop.SetActive(item.wall!=null && item.wall.enabled && !faded);
            }
        }
        private void OnDestroy()
        {
            foreach(var item in mounted)if(item.prop!=null)Dispose(item.prop);
            foreach(var material in materials)if(material!=null)Dispose(material);
        }
        private static void Dispose(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
