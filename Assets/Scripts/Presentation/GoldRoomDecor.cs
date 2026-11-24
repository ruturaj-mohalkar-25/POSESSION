using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Dressing is visual only, except explicitly marked solid hiding crates.
    public sealed class GoldRoomDecor : MonoBehaviour
    {
        private readonly List<Material> materials = new();
        private readonly List<(Renderer wall, GameObject prop)> mounted = new();
        private readonly List<GameObject> scopedProps = new();
        private readonly List<Vector3> occupied = new();
        private readonly List<(Vector3 from, Vector3 to)> patrolSegments = new();
        private Material cream, brass, teal, ink, clay, leaf, stem, blue, rug;
        private int cornerProps;
        private Transform[] objectives;
        private Transform storeyRoot;
        private Transform[] interactionKeepouts;
        private float storeyFloorHeight;
        private bool includeSolidCover = true;

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
            // The two-storey level explicitly installs one scoped dressing owner per floor.
            if (scene.name == CampaignLevels.FourthScene) return;
            var roots = scene.GetRootGameObjects();
            if (!roots.Any(r => r.GetComponentInChildren<SpiritController>(true) != null)
                || roots.Any(r => r.GetComponent<GoldRoomDecor>() != null)) return;
            var go = new GameObject("Gold_RoomDecor");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<GoldRoomDecor>().Build(scene);
        }

        public static void Install(Scene scene, Transform storeyRoot, Transform[] interactionKeepouts,
            bool includeSolidCover)
        {
            if (!scene.IsValid() || !scene.isLoaded || storeyRoot == null
                || storeyRoot.gameObject.scene != scene) return;
            if (storeyRoot.GetComponentsInChildren<GoldRoomDecor>(true)
                .Any(owner => owner.storeyRoot == storeyRoot)) return;
            var go = new GameObject("Gold_RoomDecor");
            go.transform.SetParent(storeyRoot, false);
            var owner = go.AddComponent<GoldRoomDecor>();
            owner.storeyRoot = storeyRoot;
            owner.interactionKeepouts = interactionKeepouts;
            owner.includeSolidCover = includeSolidCover;
            owner.Build(scene);
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
            cream = Paint(new Color(.88f,.83f,.69f)); brass = Paint(new Color(.57f,.39f,.17f));
            teal = Paint(new Color(.12f,.43f,.4f)); ink = Paint(new Color(.065f,.085f,.11f));
            clay = Paint(new Color(.58f,.27f,.16f)); leaf = Paint(new Color(.19f,.40f,.24f));
            stem = Paint(new Color(.22f,.28f,.15f)); blue = Paint(new Color(.12f,.23f,.31f));
            rug = Paint(new Color(.36f,.23f,.19f));
            var all = storeyRoot != null ? storeyRoot.GetComponentsInChildren<MeshRenderer>(true)
                : scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
            var floors = all.Where(r => r.GetComponent<Collider>() != null && r.bounds.size.y < .6f
                && r.name.ToLowerInvariant().Contains("floor")).OrderBy(r => r.name).ToArray();
            if (storeyRoot != null && floors.Length > 0) storeyFloorHeight = floors.Min(f => f.bounds.max.y);
            var walls = all.Where(r => r.GetComponent<Collider>() != null && r.bounds.size.y >= 3f
                && r.GetComponentInParent<EnemyBase>() == null && r.GetComponentInParent<Door>() == null
                && r.GetComponentInParent<BreakableWall>() == null && r.GetComponentInParent<Target>() == null).ToArray();
            objectives = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.GetComponent<Door>() != null || t.GetComponent<Target>() != null
                    || t.GetComponent<BreakableWall>() != null || t.GetComponent<LevelExit>() != null
                    || t.GetComponent<EnemyBase>() != null || t.GetComponent<SpiritController>() != null
                    || t.GetComponent<PossessionChargePickup>() != null || t.GetComponent<HealthPickup>() != null
                    || t.name.ToLowerInvariant().Contains("waypoint"))
                .Concat(interactionKeepouts ?? new Transform[0])
                .Where(t => t != null && t.gameObject.scene == scene && OnStorey(t.position)).Distinct().ToArray();
            BuildPatrolClearance(scene);
            occupied.AddRange(all.Where(r => r.name == "Gold_CornerCabinet").Select(r => r.bounds.center));
            Physics.SyncTransforms();
            int room = 0;
            foreach (var floor in floors.Where(f => f.bounds.size.x >= 7f && f.bounds.size.z >= 7f))
            {
                if (room >= 7) break;
                var b = floor.bounds;
                Mount(walls,b,room);
                Vector3 a = new Vector3(b.min.x+1.25f,b.max.y,b.max.z-1.25f);
                Vector3 c = new Vector3(b.max.x-1.25f,b.max.y,b.max.z-1.25f);
                bool leftPlant=Corner(a,Vector3.left) && Clear(a,2.5f);
                if (leftPlant) { CornerProp(a); occupied.Add(a); }
                else if(Corner(c,Vector3.right) && Clear(c,2.5f)) { CornerProp(c); occupied.Add(c); }
                var vaseAt=leftPlant?c:a;
                if (Corner(vaseAt,leftPlant?Vector3.right:Vector3.left) && Clear(vaseAt,2.5f)) { CornerProp(vaseAt); occupied.Add(vaseAt); }
                var mat = new Vector3(b.min.x+2.2f,b.max.y+.035f,b.center.z);
                if (includeSolidCover && Clear(mat,3f) && !patrolSegments.Any(s=>DistanceToSegment(mat,s.from,s.to)<2.7f) && !Physics.CheckBox(mat+Vector3.up*1.14f,new Vector3(1.51f,1.14f,1.04f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    Mat(mat);
                room++;
            }
        }
        // Actor centres and interaction panels sit above their own floor; the other storey is five metres away.
        private bool OnStorey(Vector3 point) => storeyRoot == null || Mathf.Abs(point.y-storeyFloorHeight)<2.6f;
        private bool Clear(Vector3 point,float distance) => !objectives.Any(t => Flat(t.position,point)<distance)
            && !occupied.Any(p => Flat(p,point)<2.4f)
            && !patrolSegments.Any(segment=>DistanceToSegment(point,segment.from,segment.to)<2.2f);
        private static float Flat(Vector3 a,Vector3 b) => Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        private static bool Corner(Vector3 p,Vector3 side) =>
            Physics.Raycast(p+Vector3.up*.7f,side,1.7f,1,QueryTriggerInteraction.Ignore)
            && Physics.Raycast(p+Vector3.up*.7f,Vector3.forward,1.7f,1,QueryTriggerInteraction.Ignore)
            && !Physics.CheckBox(p+Vector3.up*.85f,new Vector3(.65f,.8f,.65f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);

        private static bool IsWaypoint(Transform t) => t.name.ToLowerInvariant().Contains("waypoint")
            || t.name.Contains("_WP");

        private void BuildPatrolClearance(Scene scene)
        {
            // Authored routes use either a Waypoint_* group or <enemy>_WP0/1 siblings.
            // Keep plants clear of whole route segments, not only their endpoints.
            var transforms = storeyRoot != null ? storeyRoot.GetComponentsInChildren<Transform>(true)
                : scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var points = transforms.Where(t => IsWaypoint(t) && OnStorey(t.position)).ToArray();
            foreach (var group in points.GroupBy(t => (t.parent, PatrolGroup(t))))
            {
                var route = group.OrderBy(t => t.GetSiblingIndex()).ToArray();
                for (int i = 0; i < route.Length; i++)
                    patrolSegments.Add((route[i].position,route[(i + 1) % route.Length].position));
            }
        }

        private string PatrolGroup(Transform point)
        {
            if (point.name.Contains("_WP")) return point.name.Substring(0,point.name.LastIndexOf("_WP"));
            // Level 4 keeps each Guard's named waypoint siblings under its storey root.
            int suffix = storeyRoot != null
                ? point.name.LastIndexOf("_Waypoint",System.StringComparison.Ordinal) : -1;
            return suffix >= 0 ? point.name.Substring(0,suffix) : string.Empty;
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
            foreach (var wall in walls.OrderBy(w => Mathf.Abs(w.bounds.center.x-room.center.x)))
            {
                Bounds b=wall.bounds;
                if (storeyRoot != null && (b.min.y>room.max.y+.5f || b.max.y<room.max.y+1f)) continue;
                if (b.size.z>1.5f || b.center.z<room.center.z || Mathf.Abs(b.min.z-room.max.z)>1f) continue;
                float left=Mathf.Max(b.min.x,room.min.x)+1.5f,right=Mathf.Min(b.max.x,room.max.x)-1.5f;
                if(right<left)continue;
                foreach(float x in new[]{left,right,(left+right)*.5f})
                {
                    Vector3 p=new Vector3(x,Mathf.Min(room.max.y+2.7f,b.max.y-.9f),b.min.z-.13f);
                    if(!Clear(p,2.7f))continue;
                    if(wall.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Gold_RoomSign" && Mathf.Abs(t.position.x-x)<3.5f))continue;
                    bool tv=index%3==1;
                    var root=Root(tv?"Decor_WallTV":"Decor_Painting",p);
                    if (storeyRoot == null) root.SetParent(wall.transform,true);
                    if(tv)TV(root);else Painting(root,index);
                    mounted.Add((wall,root.gameObject));occupied.Add(p);return;
                }
            }
        }
        private Transform Root(string name,Vector3 position)
        {
            var t=new GameObject(name).transform;t.gameObject.layer=2;t.SetParent(transform,false);t.position=position;
            if (storeyRoot != null) scopedProps.Add(t.gameObject);
            return t;
        }
        private GameObject Part(Transform root,string name,PrimitiveType kind,Vector3 position,Vector3 size,Material m)
            => GoldRoomPresentation.Part(root,name,kind,root.TransformPoint(position),size,m);
        private void Painting(Transform root,int variant)
        {
            Part(root,"Brass frame",PrimitiveType.Cube,Vector3.zero,new Vector3(2.5f,1.9f,.12f),brass);
            Part(root,"Linen canvas",PrimitiveType.Cube,new Vector3(0,0,-.075f),new Vector3(2.28f,1.68f,.035f),cream);
            var sun=Part(root,"Ochre sun",PrimitiveType.Cylinder,new Vector3(.57f,.36f,-.11f),new Vector3(.46f,.012f,.46f),clay);
            sun.transform.rotation=Quaternion.Euler(90,0,0);
            Part(root,"Horizon",PrimitiveType.Cube,new Vector3(0,-.44f,-.12f),new Vector3(2.15f,.44f,.025f),teal);
            for(int i=0;i<3;i++)
            {
                var hill=Part(root,"Abstract hills",PrimitiveType.Sphere,new Vector3(-.78f+i*.65f,-.18f,-.135f),new Vector3(.85f,.55f+i*.08f,.025f),i%2==0?blue:teal);
            }
        }
        private void TV(Transform root)
        {
            Part(root,"Thin bezel",PrimitiveType.Cube,Vector3.zero,new Vector3(2.65f,1.65f,.14f),ink);
            Part(root,"Screen",PrimitiveType.Cube,new Vector3(0,.02f,-.085f),new Vector3(2.4f,1.37f,.025f),blue);
            for(int i=0;i<5;i++)Part(root,"Quiet city screen",PrimitiveType.Cube,new Vector3(-.85f+i*.43f,-.21f,-.105f),new Vector3(.28f,.25f+(i%3)*.19f,.015f),teal);
            Part(root,"Screen reflection",PrimitiveType.Cube,new Vector3(-.45f,.48f,-.12f),new Vector3(1.2f,.045f,.01f),cream);
        }
        private void CornerProp(Vector3 at)
        { if (cornerProps++ % 2 == 0) Plant(at); else Vase(at); }

        private void Plant(Vector3 at)
        {
            var r=Root("Decor_CornerPlant",at);
            Part(r,"Clay planter",PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(.72f,.3f,.72f),clay);
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
            Part(r,"Vase neck",PrimitiveType.Cylinder,new Vector3(0,.99f,0),new Vector3(.25f,.23f,.25f),cream);
            Part(r,"Dark opening",PrimitiveType.Cylinder,new Vector3(0,1.23f,0),new Vector3(.2f,.008f,.2f),ink);
            for(int i=0;i<3;i++)
            {
                var reed=Part(r,"Dried reed",PrimitiveType.Cylinder,new Vector3((i-1)*.09f,1.48f,0),new Vector3(.025f,.38f,.025f),stem);
                reed.transform.rotation=Quaternion.Euler(0,0,(i-1)*-12f);
                Part(r,"Seed head",PrimitiveType.Sphere,new Vector3((i-1)*.18f,1.85f,0),new Vector3(.12f,.25f,.12f),brass);
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
            if (storeyRoot != null)
            { foreach(var prop in scopedProps)if(prop!=null)Dispose(prop); }
            else foreach(var item in mounted)if(item.prop!=null)Dispose(item.prop);
            foreach(var material in materials)if(material!=null)Dispose(material);
        }
        private static void Dispose(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
