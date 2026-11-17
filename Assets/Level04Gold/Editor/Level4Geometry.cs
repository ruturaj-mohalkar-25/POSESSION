using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Possession.Level04Gold.Editor
{
    // Arrays retain the original plan coordinates; generated geometry uses Level4Layout once.
    // Scene/gameplay wiring belongs to Level4GoldBuilder.
    public static class Level4Geometry
    {
        public const float UpperFloorHeight = 5f;
        public const float WallHeight = 4.5f;
        public const float CoverHeight = 1.7f;
        public static Vector3 Spawn => Level4Layout.Map(new Vector3(9f, 1.4f, 7f));
        private const string Art = "Assets/Level04Gold/Materials";
        private const string Meshes = "Assets/Level04Gold/Meshes";
        private const float WallThickness = .4f;
        private const float Scale = Level4Layout.GeometryScale;
        // The gallery enters at Z=8 in the plan. Keep its entire widened throat in
        // front of the stair guards so the approach is east, then north, without a U-turn.
        private static float StairFoot => 8f*Scale+Level4Layout.CorridorWidth*.5f+1f;
        private static float StairTop => 18f*Scale;

        private readonly struct Area
        {
            public readonly string Name;
            public readonly Rect Rect;
            public Area(string name, float x, float z, float width, float depth)
            { Name = name; Rect = new Rect(x, z, width, depth); }
        }

        private static readonly Area[] LowerRooms =
        {
            new Area("Entry", 2, 2, 14, 10),
            new Area("Junction", 20, 2, 20, 12),
            new Area("LongGallery", 44, 2, 20, 12),
            new Area("RearStair", 68, 2, 10, 20),
            new Area("EngineerBay", 2, 18, 14, 12),
            new Area("ServiceRoom", 20, 18, 14, 12),
            new Area("Workshop", 38, 18, 16, 12),
            new Area("Maintenance", 2, 34, 14, 12),
            new Area("ServiceLanding", 20, 34, 32, 8),
            new Area("Vault", 24, 46, 24, 10)
        };

        private static readonly Area[] LowerHalls =
        {
            new Area("Entry_Junction",16,5,4,4),
            new Area("GalleryDoor",40,6,4,4),
            new Area("Gallery_Stair",64,6,4,4),
            new Area("Entry_Engineer",7,12,4,6),
            new Area("Junction_Service",24,14,4,4),
            new Area("Engineer_Service",16,22,4,4),
            new Area("Engineer_Maintenance",7,30,4,4),
            new Area("ServiceDoor",34,22,4,4),
            new Area("AccessGate",24,30,4,4),
            new Area("SmashWall",44,30,4,4),
            new Area("MaintenanceAccess",16,36,4,4),
            new Area("VaultDoor",33,42,6,4)
        };

        private static readonly Area[] UpperRooms =
        {
            new Area("StairLanding",64,18,14,12),
            new Area("Records",44,18,14,12),
            new Area("WatchGallery",60,34,18,12),
            new Area("BrokenOverlook",38,34,14,12)
        };

        private static readonly Area[] UpperHalls =
        {
            new Area("Landing_Records",58,22,6,4),
            new Area("Landing_Watch",68,30,4,4),
            new Area("Watch_Overlook",52,38,8,4)
        };

        private static readonly Rect[] LowerCover =
        {
            new Rect(22,10,4,1.2f), new Rect(32,6,1.2f,4),
            new Rect(49,9,6,1.3f), new Rect(57,6,1.3f,4),
            new Rect(21,26,4,1.2f), new Rect(30,22,1.2f,4),
            new Rect(40,26,4,1.2f), new Rect(48,23,1.2f,4),
            new Rect(29,36,1.2f,3), new Rect(42,35,5,1.2f)
        };

        private static readonly Rect[] UpperCover =
        {
            new Rect(65,35,1.2f,4), new Rect(71,39,4,1.2f),
            new Rect(47,35,1.2f,4), new Rect(45,24,5,1.2f)
        };

        public static GameObject Build()
        {
            EnsureFolder(Art);
            EnsureFolder(Meshes);
            var floor = Material("Floor_Slate", new Color(.22f,.25f,.30f));
            var upperFloor = Material("Floor_Upper", new Color(.24f,.27f,.31f));
            var wall = Material("Wall_BlueSteel", new Color(.16f,.21f,.28f));
            var trim = Material("Trim_Metal", new Color(.38f,.45f,.52f));
            var cover = Material("Cover_Crate", new Color(.36f,.31f,.25f));
            var glow = Material("Wayfinding_Cyan", new Color(.24f,.72f,.80f), new Color(.10f,.42f,.48f));
            var amber = Material("Hazard_Amber",new Color(.95f,.62f,.16f),new Color(.35f,.18f,.02f));
            var glass = Material("Guard_Glass",new Color(.22f,.62f,.73f,.12f));
            MakeTransparent(glass);

            var root = new GameObject("Level04_Geometry");
            var lower = Child(root.transform,"Lower floor");
            var upper = Child(root.transform,"Upper floor");
            var stairs = Child(root.transform,"Stairs");

            // Floors and boundary topology share these exact world-space rectangles.
            // Widen hall cross-sections locally instead of scaling a parent transform.
            var lowerAreas = LowerRooms.Select(a=>Expanded(a,false)).Concat(LowerHalls.Select(a=>Expanded(a,true))).ToArray();
            var upperAreas = UpperRooms.Select(a=>Expanded(a,false)).Concat(UpperHalls.Select(a=>Expanded(a,true))).ToArray();
            foreach (var area in lowerAreas) Floor(lower,area,0,floor);
            foreach (var area in upperAreas) Floor(upper,area,UpperFloorHeight,upperFloor);

            BoundaryWalls(lower,lowerAreas.Select(a=>a.Rect).ToArray(),0,wall,trim,glass);
            // This rectangle is used only for the wall union. There is no flat slab above the flight.
            var upperBoundary = upperAreas.Select(a=>a.Rect)
                .Concat(new[] {new Rect(71f*Scale,StairFoot,6f*Scale,StairTop-StairFoot)}).ToArray();
            BoundaryWalls(upper,upperBoundary,UpperFloorHeight,wall,trim,glass);

            Covers(lower,LowerCover,0,cover,trim);
            Covers(upper,UpperCover,UpperFloorHeight,cover,trim);
            Stair(stairs,trim,glow,glass);
            BrokenBridge(upper,trim,amber);
            Landmarks(lower,glow,trim);

            root.AddComponent<Level4FloorVisibility>().Configure(upper);
            AssetDatabase.SaveAssets();
            return root;
        }

        private static Rect MappedRect(Rect rect)
        {
            var center=Level4Layout.Map(new Vector3(rect.center.x,0,rect.center.y));
            return new Rect(center.x-rect.width*Scale*.5f,center.z-rect.height*Scale*.5f,
                rect.width*Scale,rect.height*Scale);
        }

        private static Area Expanded(Area area,bool hall)
        {
            Rect r=MappedRect(area.Rect);
            if(hall)
            {
                Vector2 center=r.center;
                r.width=Mathf.Max(r.width,Level4Layout.CorridorWidth);
                r.height=Mathf.Max(r.height,Level4Layout.CorridorWidth);
                r.center=center;
            }
            return new Area(area.Name,r.x,r.y,r.width,r.height);
        }

        private static void Floor(Transform parent,Area area,float y,Material material)
        {
            Rect r=area.Rect;
            Cube(parent,"Floor_"+area.Name,new Vector3(r.center.x,y-.175f,r.center.y),
                new Vector3(r.width,.35f,r.height),material);
        }

        private static void BoundaryWalls(Transform parent,Rect[] rects,float y,Material wall,Material trim,Material glass)
        {
            var xs=rects.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(v=>v).ToArray();
            var zs=rects.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(v=>v).ToArray();
            var vertical=new Dictionary<float,List<Vector2>>();
            var horizontal=new Dictionary<float,List<Vector2>>();
            bool Inside(float x,float z)=>rects.Any(r=>x>r.xMin&&x<r.xMax&&z>r.yMin&&z<r.yMax);
            for(int xi=0;xi<xs.Length-1;xi++)
            for(int zi=0;zi<zs.Length-1;zi++)
            {
                float x=xs[xi],xx=xs[xi+1],z=zs[zi],zz=zs[zi+1];
                float mx=(x+xx)*.5f,mz=(z+zz)*.5f;
                if(!Inside(mx,mz))continue;
                if(!Inside(x-.01f,mz))Add(vertical,x,new Vector2(z,zz));
                if(!Inside(xx+.01f,mz))Add(vertical,xx,new Vector2(z,zz));
                if(!Inside(mx,z-.01f))Add(horizontal,z,new Vector2(x,xx));
                if(!Inside(mx,zz+.01f))Add(horizontal,zz,new Vector2(x,xx));
            }
            int index=0;
            foreach(var item in vertical.OrderBy(p=>p.Key))
            foreach(var segment in Merge(item.Value))
            {
                // Ramp sides have sloped, full-height glass guards instead of a floating wall.
                if(y>0 && segment.y<=StairTop+.001f && (Mathf.Approximately(item.Key,71f*Scale)||Mathf.Approximately(item.Key,77f*Scale)))continue;
                bool window=y>0&&Mathf.Approximately(item.Key,38f*Scale)&&segment.x>=34f*Scale-.01f&&segment.y<=46f*Scale+.01f;
                Vector3 pos=new Vector3(item.Key,y+WallHeight*.5f,(segment.x+segment.y)*.5f);
                var obj=Cube(parent,(window?"Overlook_Glass_Wall_":"Wall_")+(++index),pos,
                    new Vector3(WallThickness,WallHeight,segment.y-segment.x),window?glass:wall);
                obj.AddComponent<VisibilityOccluder>();
                if(window)WindowFrame(parent,item.Key,segment.x,segment.y,y,trim);
                else Cube(parent,"Wall_Cap_"+index,pos+Vector3.up*(WallHeight*.5f+.025f),
                    new Vector3(.49f,.05f,segment.y-segment.x),trim,false);
            }
            foreach(var item in horizontal.OrderBy(p=>p.Key))
            foreach(var segment in Merge(item.Value))
            {
                if(y>0 && item.Key<StairTop)continue; // Open bottom of the actual ramp.
                var pos=new Vector3((segment.x+segment.y)*.5f,y+WallHeight*.5f,item.Key);
                var obj=Cube(parent,"Wall_"+(++index),pos,
                    new Vector3(segment.y-segment.x,WallHeight,WallThickness),wall);
                obj.AddComponent<VisibilityOccluder>();
                Cube(parent,"Wall_Cap_"+index,pos+Vector3.up*(WallHeight*.5f+.025f),
                    new Vector3(segment.y-segment.x,.05f,.49f),trim,false);
            }
        }

        private static void Add(Dictionary<float,List<Vector2>> map,float key,Vector2 segment)
        {
            if(!map.TryGetValue(key,out var entries))map.Add(key,entries=new List<Vector2>());
            entries.Add(segment);
        }

        private static IEnumerable<Vector2> Merge(List<Vector2> segments)
        {
            var sorted=segments.OrderBy(s=>s.x).ThenBy(s=>s.y).ToArray();
            if(sorted.Length==0)yield break;
            Vector2 current=sorted[0];
            for(int i=1;i<sorted.Length;i++)
            {
                if(sorted[i].x<=current.y+.001f)current.y=Mathf.Max(current.y,sorted[i].y);
                else{yield return current;current=sorted[i];}
            }
            yield return current;
        }

        private static void Covers(Transform parent,Rect[] covers,float y,Material wood,Material trim)
        {
            for(int i=0;i<covers.Length;i++)
            {
                Rect r=MappedRect(covers[i]);
                Cube(parent,"Cover_"+(i+1).ToString("00"),new Vector3(r.center.x,y+CoverHeight*.5f,r.center.y),
                    new Vector3(r.width,CoverHeight,r.height),wood);
                // Keep the metal cap above the body's top face to prevent depth flicker.
                Cube(parent,"Cover_Rim_"+(i+1).ToString("00"),new Vector3(r.center.x,y+CoverHeight-.025f,r.center.y),
                    new Vector3(r.width+.04f,.10f,r.height+.04f),trim,false);
            }
        }

        private static void Stair(Transform parent,Material metal,Material glow,Material glass)
        {
            // The gallery opens directly into a flat foyer ahead of the northbound flight.
            // Its guard starts beyond the entry throat, leaving an intuitive east-then-north turn.
            // All stair coordinates below are physical world metres; rise and guard thickness stay fixed.
            float left=71f*Scale,right=77f*Scale,bottom=StairFoot,topEnd=StairTop;
            float centre=(left+right)*.5f,run=topEnd-bottom,width=right-left;
            Mesh ramp=PrismMesh("Stair_Ramp",left,right,bottom,topEnd,0,UpperFloorHeight,.30f);
            var walk=new GameObject("Stair_Ramp_WalkSurface",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            walk.transform.SetParent(parent,false);
            walk.GetComponent<MeshFilter>().sharedMesh=ramp;
            walk.GetComponent<MeshRenderer>().sharedMaterial=metal;
            walk.GetComponent<MeshCollider>().sharedMesh=ramp;
            walk.isStatic=true;

            const int steps=25;
            float tread=run/steps;
            for(int i=0;i<steps;i++)
            {
                float top=(i+1)*UpperFloorHeight/steps;
                float z=bottom+(i+.5f)*tread;
                Cube(parent,"Stair_Tread_"+(i+1).ToString("00"),new Vector3(centre,top-.035f,z),
                    new Vector3(width-.08f,.07f,tread),metal,false);
                Cube(parent,"Stair_Nosing_"+(i+1).ToString("00"),new Vector3(centre,top+.003f,bottom+i*tread),
                    new Vector3(width-.10f,.016f,.035f),glow,false);
            }
            foreach(float x in new[]{left-.10f,right+.10f})
            {
                Mesh side=PrismMesh(x<centre?"Stair_Guard_Left":"Stair_Guard_Right",x-.10f,x+.10f,bottom,topEnd,0,UpperFloorHeight,WallHeight,true);
                var pane=new GameObject(x<centre?"Stair_Glass_Guard_Left":"Stair_Glass_Guard_Right",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
                pane.transform.SetParent(parent,false);pane.GetComponent<MeshFilter>().sharedMesh=side;
                pane.GetComponent<MeshRenderer>().sharedMaterial=glass;pane.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                pane.GetComponent<MeshCollider>().sharedMesh=side;
                pane.AddComponent<VisibilityOccluder>();
                for(int i=0;i<=4;i++)
                {
                    float z=bottom+i*run/4f,ground=i*UpperFloorHeight/4f;
                    Cube(parent,"Stair_Rail_Post",new Vector3(x,ground+WallHeight*.5f,z),new Vector3(.12f,WallHeight,.12f),metal,false);
                }
                var rail=Cube(parent,"Stair_Handrail",new Vector3(x,UpperFloorHeight*.5f+1.15f,(bottom+topEnd)*.5f),
                    new Vector3(.13f,.13f,Mathf.Sqrt(run*run+UpperFloorHeight*UpperFloorHeight)),metal,false);
                rail.transform.rotation=Quaternion.Euler(-Mathf.Atan2(UpperFloorHeight,run)*Mathf.Rad2Deg,0,0);
            }
            // Noncolliding floor paint communicates the turn by shape as well as contrast.
            float entryZ=8f*Scale,entryX=66.4f*Scale;
            Cube(parent,"Stair_Approach_East",new Vector3((entryX+centre)*.5f,.025f,entryZ),
                new Vector3(centre-entryX,.025f,.16f),glow,false);
            Cube(parent,"Stair_Approach_North",new Vector3(centre,.025f,(entryZ+bottom-.35f)*.5f),
                new Vector3(.16f,.025f,bottom-.35f-entryZ),glow,false);
            FloorArrow(parent,"Stair_East_Arrow",new Vector3(69.2f*Scale,.045f,entryZ),Vector3.right,glow);
            FloorArrow(parent,"Stair_Up_Arrow",new Vector3(centre,.045f,bottom-1f),Vector3.forward,glow);
            Cube(parent,"Stair_Entrance_Threshold",new Vector3(centre,.035f,bottom-.15f),
                new Vector3(width-.2f,.03f,.18f),glow,false);
            FloorArrow(parent,"Stair_Down_Arrow",new Vector3(centre,UpperFloorHeight+.045f,topEnd+1.5f),Vector3.back,glow);
        }

        private static void FloorArrow(Transform parent,string name,Vector3 centre,Vector3 forward,Material paint)
        {
            Quaternion facing=Quaternion.LookRotation(forward);
            var stem=Cube(parent,name+"_Stem",centre-forward*.2f,new Vector3(.18f,.025f,1f),paint,false);
            stem.transform.rotation=facing;
            foreach(float side in new[]{-1f,1f})
            {
                var arm=Cube(parent,name+"_Chevron",centre+forward*.25f+facing*Vector3.right*(side*.23f),
                    new Vector3(.18f,.025f,.75f),paint,false);
                arm.transform.rotation=facing*Quaternion.Euler(0,-side*45f,0);
            }
        }

        private static Mesh PrismMesh(string name,float x0,float x1,float z0,float z1,float y0,float y1,float depth,bool above=false)
        {
            float bottom0=above?y0:y0-depth,bottom1=above?y1:y1-depth;
            float top0=above?y0+depth:y0,top1=above?y1+depth:y1;
            var mesh=new Mesh{name=name};
            mesh.vertices=new[]{
                new Vector3(x0,bottom0,z0),new Vector3(x1,bottom0,z0),new Vector3(x1,bottom1,z1),new Vector3(x0,bottom1,z1),
                new Vector3(x0,top0,z0),new Vector3(x1,top0,z0),new Vector3(x1,top1,z1),new Vector3(x0,top1,z1)};
            mesh.triangles=new[]{4,7,6,4,6,5,0,1,2,0,2,3,0,4,5,0,5,1,3,2,6,3,6,7,0,3,7,0,7,4,1,5,6,1,6,2};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Meshes+"/"+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }

        private static void WindowFrame(Transform parent,float x,float z0,float z1,float y,Material metal)
        {
            for(float z=z0;z<z1+.001f;z+=3f)
                Cube(parent,"Overlook_Glass_Post",new Vector3(x,y+WallHeight*.5f,Mathf.Min(z,z1)),new Vector3(.13f,WallHeight,.13f),metal,false);
            Cube(parent,"Overlook_Glass_Handrail",new Vector3(x,y+1.15f,(z0+z1)*.5f),new Vector3(.15f,.15f,z1-z0),metal,false);
            Cube(parent,"Overlook_Glass_Crown",new Vector3(x,y+WallHeight,(z0+z1)*.5f),new Vector3(.15f,.13f,z1-z0),metal,false);
        }

        private static void BrokenBridge(Transform parent,Material metal,Material warning)
        {
            // Visible wreckage behind the high glazed guard; never a hidden walkable shortcut.
            for(int i=0;i<3;i++)
            {
                var piece=Cube(parent,"Collapsed_Bridge_Girder",Level4Layout.Map(new Vector3(36.3f-i*.7f,4.35f-i*.5f,38.2f+i*.7f)),
                    new Vector3(3f*Scale,.22f,.28f),metal,false);
                piece.transform.rotation=Quaternion.Euler(0,-10+i*18,18+i*14);
            }
            for(int i=0;i<6;i++)
                Cube(parent,"Overlook_Warning_Lamp",Level4Layout.Map(new Vector3(38.18f,5.13f,35+i*1.8f)),new Vector3(.16f,.12f,.35f),warning,false);
        }

        private static void Landmarks(Transform parent,Material glow,Material metal)
        {
            // Matching split-arch silhouettes identify maintenance access from the overlook.
            foreach(float x in new[]{16.4f,19.6f})
            {
                float worldX=x*Scale,worldZ=38f*Scale;
                foreach(float z in new[]{worldZ-Level4Layout.CorridorWidth*.5f,worldZ+Level4Layout.CorridorWidth*.5f})
                    Cube(parent,"Maintenance_Arch_Post",new Vector3(worldX,2.0f,z),new Vector3(.22f,4,.22f),metal,false);
                Cube(parent,"Maintenance_Arch_Crown",new Vector3(worldX,4.05f,worldZ),new Vector3(.25f,.20f,Level4Layout.CorridorWidth+.2f),glow,false);
                Cube(parent,"Maintenance_Arch_Centre_Notch",new Vector3(worldX,3.8f,worldZ),new Vector3(.28f,.35f,.45f),glow,false);
            }
        }

        private static Transform Child(Transform parent,string name)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
        }

        private static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.position=position;go.transform.localScale=scale;go.isStatic=true;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            if(material.color.a<.99f)go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            return go;
        }

        private static Material Material(string name,Color color,Color? emission=null)
        {
            string path=Art+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Glossiness",.20f);material.SetFloat("_Metallic",.12f);
            if(emission.HasValue){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",emission.Value);}
            EditorUtility.SetDirty(material);return material;
        }

        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Mode",3);material.SetOverrideTag("RenderType","Transparent");
            material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
            EditorUtility.SetDirty(material);
        }

        private static void EnsureFolder(string path)
        {
            string parent="Assets";
            foreach(string part in path.Split('/').Skip(1))
            {
                string next=parent+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(parent,part);parent=next;
            }
        }
    }
}
