using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class CampaignDoorSecurityStyle
    {
        private const string Folder = "Assets/Art/DoorSecurity";
        private const string LeafDetails = "Security leaf details";
        private const string FrameDetails = "Security frame details";
        private static Material backing, face, frame, recess, cyan;
        private static Mesh plate, leftLock, rightLock;

        [MenuItem("Possession/Level Design/Style Campaign Security Doors")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before styling campaign doors.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                CampaignDoorSetup.Apply(scene);
                Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        public static void Apply(Scene scene)
        {
            if (!CampaignLevels.SceneNames.Contains(scene.name)) return;
            PrepareAssets();
            foreach (var view in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SlidingDoorView>(true)))
            {
                StyleLeaf(view.LeftLeaf, true);
                StyleLeaf(view.RightLeaf, false);
                StyleFrame(view);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void PrepareAssets()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "DoorSecurity");
            backing = Material("Panel backing", new Color(.59f, .68f, .75f), false, .35f);
            face = Material("Pale panel face", new Color(.82f, .88f, .92f), false, .22f);
            frame = Material("Security frame", new Color(.46f, .56f, .64f), false, .3f);
            recess = Material("Panel recess", new Color(.16f, .25f, .32f), false, .1f);
            cyan = Material("Cyan light", new Color(.12f, .9f, 1f), true, 0f);
            var outline = new[] {
                new Vector2(-.42f,-.5f), new Vector2(.42f,-.5f), new Vector2(.5f,-.42f), new Vector2(.5f,.42f),
                new Vector2(.42f,.5f), new Vector2(-.42f,.5f), new Vector2(-.5f,.42f), new Vector2(-.5f,-.42f)
            };
            plate = BeveledPrism("Chamfered panel", outline, Vector3.zero);
            var left = new[] { new Vector2(-.5f,-.23f), new Vector2(-.23f,-.5f), new Vector2(0,-.5f),
                new Vector2(0,.5f), new Vector2(-.23f,.5f), new Vector2(-.5f,.23f) };
            leftLock = BeveledPrism("Left lock half", left, new Vector3(-.2f, 0, 0));
            var right = left.Select(p => new Vector2(-p.x, p.y)).Reverse().ToArray();
            rightLock = BeveledPrism("Right lock half", right, new Vector3(.2f, 0, 0));
        }

        private static void StyleLeaf(Transform leaf, bool left)
        {
            if (leaf == null) throw new System.InvalidOperationException("Sliding door leaf is missing.");
            leaf.GetComponent<Renderer>().sharedMaterial = backing;
            var old = leaf.Find(LeafDetails);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var details = Node(leaf, LeafDetails);
            float inner = left ? 1f : -1f;
            Mesh half = left ? leftLock : rightLock;
            // Both faces are decorated so the style reads from either room and camera direction.
            foreach (float side in new[] { -1f, 1f })
            {
                string surface = side < 0 ? "Front" : "Back";
                var root = Node(details, surface);
                foreach (float y in new[] { -.225f, .225f })
                    Shape(root, "Raised pale panel", plate, new Vector3(0, y, side * .57f), new Vector3(.88f, .385f, .18f), face);
                foreach (float x in new[] { -.465f, .465f })
                    Cube(root, "Edge recess", new Vector3(x, 0, side * .52f), new Vector3(.018f, .83f, .07f), recess);
                foreach (float ySign in new[] { -1f, 1f })
                {
                    var points = new[] { new Vector2(-inner*.44f,ySign*.29f), new Vector2(-inner*.18f,ySign*.29f),
                        new Vector2(-inner*.03f,ySign*.205f), new Vector2(inner*.36f,ySign*.205f) };
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Vector2 delta = points[i+1] - points[i];
                        var light = Cube(root, "Cyan panel trace", new Vector3((points[i].x+points[i+1].x)*.5f,
                            (points[i].y+points[i+1].y)*.5f, side*.69f), new Vector3(delta.magnitude,.012f,.07f), cyan);
                        light.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                    }
                }
                // The split lock is carried by its own panel; no centre object remains in an open doorway.
                var lockAt = new Vector3(inner * .5f, 0, side * .68f);
                Shape(root, "Split lock bezel", half, lockAt, new Vector3(.43f,.27f,.18f), frame);
                lockAt.z = side*.79f;
                Shape(root, "Split lock face", half, lockAt, new Vector3(.34f,.21f,.13f), face);
                lockAt.z = side*.88f;
                Shape(root, "Split cyan lock", half, lockAt, new Vector3(.15f,.14f,.1f), cyan);
                lockAt.z = side*.95f;
                Shape(root, "Split lock inset", half, lockAt, new Vector3(.09f,.10f,.06f), recess);
            }
        }

        private static void StyleFrame(SlidingDoorView view)
        {
            var parent = view.LeftLeaf.parent;
            foreach (Transform child in parent)
                if (child.name == "Door jamb left" || child.name == "Door jamb right" || child.name == "Door header")
                    child.GetComponent<Renderer>().sharedMaterial = frame;
            var old = parent.Find(FrameDetails);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var details = Node(parent, FrameDetails);
            var door = view.GetComponent<BoxCollider>();
            details.localPosition = parent.InverseTransformPoint(door.transform.TransformPoint(door.center));
            details.localScale = new Vector3(
                Mathf.Abs(parent.InverseTransformVector(door.transform.TransformVector(Vector3.right*door.size.x)).x),
                Mathf.Abs(parent.InverseTransformVector(door.transform.TransformVector(Vector3.up*door.size.y)).y),
                Mathf.Abs(parent.InverseTransformVector(door.transform.TransformVector(Vector3.forward*door.size.z)).z));
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float x in new[] { -.525f,.525f })
                    Cube(details, "Cyan jamb light", new Vector3(x,0,side*.69f), new Vector3(.012f,.57f,.09f),cyan);
                Cube(details,"Pale header insert",new Vector3(0,.475f,side*.69f),new Vector3(.82f,.028f,.09f),face);
                Cube(details,"Cyan header line",new Vector3(0,.478f,side*.75f),new Vector3(.58f,.006f,.06f),cyan);
            }
        }

        private static Transform Node(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }
        private static Transform Cube(Transform parent, string name, Vector3 at, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = at; obj.transform.localScale = scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (material == cyan) obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return obj.transform;
        }
        private static void Shape(Transform parent, string name, Mesh mesh, Vector3 at, Vector3 scale, Material material)
        {
            var obj = Node(parent, name);
            obj.localPosition = at; obj.localScale = scale;
            obj.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        private static Material Material(string name, Color color, bool unlit, float metal)
        {
            string path = Folder+"/"+name.Replace(' ','_')+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(unlit ? "Sprites/Default" : "Standard"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.color=color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metal);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.32f);
            material.enableInstancing=true;
            EditorUtility.SetDirty(material);
            return material;
        }
        private static Mesh BeveledPrism(string name, Vector2[] outline, Vector3 inside)
        {
            string path=Folder+"/"+name.Replace(' ','_')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh!=null)return mesh;
            mesh=new Mesh { name=name };
            var vertices=new List<Vector3>(); var indices=new List<int>();
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3f-inside)<0f){var swap=b;b=c;c=swap;}
                int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                indices.Add(index);indices.Add(index+1);indices.Add(index+2);
            }
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Triangle(a,b,c);Triangle(a,c,d);}
            Vector3 Ring(int i,float z,float inset)=>new Vector3(outline[i].x*(1f-inset)+inside.x*inset,outline[i].y*(1f-inset),z);
            for(int i=0;i<outline.Length;i++)
            {
                int next=(i+1)%outline.Length;
                Triangle(new Vector3(inside.x,0,-.5f),Ring(i,-.5f,.07f),Ring(next,-.5f,.07f));
                Triangle(new Vector3(inside.x,0,.5f),Ring(i,.5f,.07f),Ring(next,.5f,.07f));
                Quad(Ring(i,-.5f,.07f),Ring(next,-.5f,.07f),Ring(next,-.28f,0),Ring(i,-.28f,0));
                Quad(Ring(i,-.28f,0),Ring(next,-.28f,0),Ring(next,.28f,0),Ring(i,.28f,0));
                Quad(Ring(i,.28f,0),Ring(next,.28f,0),Ring(next,.5f,.07f),Ring(i,.5f,.07f));
            }
            mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
