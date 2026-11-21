using UnityEngine;
using UnityEngine.Rendering;

namespace Possession
{
    // Original typographic campus posters, not reproductions of a university seal or logo.
    // The parent decor owns placement/fading; this component owns its font material and atlas hook.
    public sealed class UscWallArt : MonoBehaviour
    {
        private Font font;
        private Material textMaterial;
        private static readonly Color Gold = new Color(1f,.8f,0f);
        private static readonly Color Paper = new Color(.98f,.95f,.85f);

        public void Build(int variant,bool television)
        {
            if (textMaterial != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMaterial = new Material(Resources.Load<Material>("Gold/RoomSignText"));
            font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZ ./",96,FontStyle.Bold);
            RefreshAtlas(font);
            Font.textureRebuilt += RefreshAtlas;
            if (television)
            {
                Label("USC",new Vector3(0,.26f,-.13f),new Vector2(2.9f,1.05f),Gold);
                Label("TROJANS  /  FIGHT ON",new Vector3(0,-.6f,-.13f),new Vector2(3.05f,.28f),Paper);
                return;
            }

            var designs = new[]
            {
                ("USC","UNIVERSITY OF","SOUTHERN CALIFORNIA"),
                ("FIGHT\nON","SOUTHERN CALIFORNIA","TROJANS"),
                ("TROJANS","UNIVERSITY OF","SOUTHERN CALIFORNIA"),
                ("SC","SOUTHERN CALIFORNIA","LOS ANGELES"),
                ("LA","LOS ANGELES","SOUTHERN CALIFORNIA"),
                ("BEAT\nUCLA","SOUTHERN CALIFORNIA","FIGHT ON"),
            };
            var design = designs[Mathf.Abs(variant)%designs.Length];
            Label(design.Item2,new Vector3(0,1.02f,-.13f),new Vector2(3.2f,.22f),Paper);
            Label(design.Item1,new Vector3(0,.08f,-.13f),new Vector2(3.2f,1.35f),Gold);
            Label(design.Item3,new Vector3(0,-1.02f,-.13f),new Vector2(3.2f,.25f),Paper);
        }

        private void Label(string value,Vector3 position,Vector2 box,Color tint)
        {
            var go = new GameObject("Campus lettering",typeof(TextMesh)) { layer = 2 };
            go.transform.SetParent(transform,false);
            go.transform.localPosition = position;
            var text = go.GetComponent<TextMesh>();
            text.text = value;
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 96;
            text.characterSize = .1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.lineSpacing = .85f;
            text.color = tint;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = textMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Vector3 size = renderer.bounds.size;
            float fit = Mathf.Min(box.x/Mathf.Max(.01f,size.x),box.y/Mathf.Max(.01f,size.y));
            go.transform.localScale = Vector3.one*fit;
        }

        private void RefreshAtlas(Font rebuilt)
        {
            if (rebuilt == font && textMaterial != null) textMaterial.mainTexture = font.material.mainTexture;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= RefreshAtlas;
            if (textMaterial == null) return;
            if (Application.isPlaying) Destroy(textMaterial); else DestroyImmediate(textMaterial);
        }
    }
}
