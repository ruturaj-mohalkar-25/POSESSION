using UnityEngine;
using UnityEngine.Rendering;
namespace Possession
{
    // Solid cover: exactly one physics box; trim remains visual-only.
    [DisallowMultipleComponent]
    public sealed class HidingCrate : MonoBehaviour
    {
        public static void Build(Transform root)
        {
            if(root.GetComponent<HidingCrate>()!=null)return;
            root.gameObject.AddComponent<HidingCrate>();root.gameObject.AddComponent<VisibilityOccluder>();root.gameObject.layer=0;
            var box=root.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,1.1f,0);box.size=new Vector3(2.8f,2.2f,1.8f);
            var wood=Resources.Load<Material>("Gold/Wood");var trim=Resources.Load<Material>("Gold/Wall");
            Piece(root,"Solid wooden box",new Vector3(0,1.1f,0),box.size,wood);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
                Piece(root,"Corner batten",new Vector3(x*1.32f,1.1f,z*.83f),new Vector3(.16f,2.2f,.16f),trim);
            foreach(float y in new[]{.12f,2.08f})
                Piece(root,"Horizontal brace",new Vector3(0,y,-.86f),new Vector3(2.8f,.2f,.08f),trim);
            for(int i=-1;i<=1;i++)Piece(root,"Lid plank",new Vector3(i*.84f,2.22f,0),new Vector3(.8f,.06f,1.8f),wood);
        }
        private static void Piece(Transform root,string name,Vector3 at,Vector3 size,Material material)
        {
            var go=GoldRoomPresentation.Part(root,name,PrimitiveType.Cube,root.TransformPoint(at),size,material);
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
        }
    }
}
