using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Presentation only. MainMenuController continues to own progression and scene loading.
    public sealed class ReferenceMenuView : MonoBehaviour
    {
        private MainMenuController controller;
        private Button launch;
        private Button[] cards;
        private Image[] borders;
        private Text[] statuses;
        private float aspect;
        private Texture2D roundTexture;
        private Sprite rounded;
        private static readonly Color Background = new Color(.085f,.095f,.115f);

        public void Build(MainMenuController owner, GameObject main, GameObject selection, Button[] levels)
        {
            controller = owner;
            rounded = RoundedSurface();
            var canvas = GetComponent<Canvas>();
            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null) { scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f; }
            var title = transform.Find("Title");
            if (title != null) title.gameObject.SetActive(false);
            var background = GoldTheme.Box(transform,"Menu backdrop",Vector2.zero,Vector2.one,Background);
            background.transform.SetAsFirstSibling();
            GoldTheme.Fill((RectTransform)main.transform,Vector2.zero,Vector2.one);
            GoldTheme.Fill((RectTransform)selection.transform,Vector2.zero,Vector2.one);
            var selectionBackground = selection.GetComponent<Image>();
            if (selectionBackground != null) selectionBackground.color = Background;
            var play = main.GetComponentsInChildren<Button>(true).First(b=>b.name=="PlayButton");
            var tutorial = main.GetComponentsInChildren<Button>(true).First(b=>b.name=="HowToPlayButton");
            var quit = main.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="QuitButton");
            if (quit != null) quit.gameObject.SetActive(false);
            Art(main.transform,"Main menu scene",0,new Vector2(.395f,0),Vector2.one);
            GoldTheme.Box(main.transform,"Left panel",Vector2.zero,new Vector2(.395f,1),Background);
            Label(main.transform,"Game title","POSSESSION",76,new Vector2(.035f,.72f),new Vector2(.37f,.86f),TextAnchor.MiddleLeft);
            var subtitle = Label(main.transform,"Subtitle","A possession puzzle game",29,new Vector2(.06f,.65f),new Vector2(.365f,.72f));
            subtitle.fontStyle = FontStyle.Normal;
            StyleButton(play,"PLAY",new Vector2(.043f,.49f),new Vector2(.357f,.60f),true);
            var avatar=GoldTheme.Button(main.transform,"AvatarButton","AVATAR",new Vector2(.043f,.36f),new Vector2(.357f,.46f),false);
            StyleButton(avatar,"AVATAR",new Vector2(.043f,.36f),new Vector2(.357f,.46f),false);
            var wardrobe=gameObject.AddComponent<USCAvatarWardrobe>();wardrobe.Build(owner,main,selection,rounded);
            avatar.onClick.AddListener(wardrobe.Open);
            StyleButton(tutorial,"HOW TO PLAY",new Vector2(.043f,.23f),new Vector2(.357f,.33f),false);
            var guide = gameObject.AddComponent<CharacterGuideView>();
            guide.Build(main, rounded);
            var characters = GoldTheme.Button(main.transform,"CharactersButton","CHARACTERS",new Vector2(.043f,.10f),new Vector2(.357f,.20f));
            StyleButton(characters,"CHARACTERS",new Vector2(.043f,.10f),new Vector2(.357f,.20f),false);
            characters.onClick.AddListener(guide.Open);
            var hint = Label(main.transform,"Controls hint","Keyboard + mouse",26,new Vector2(.05f,.025f),new Vector2(.35f,.075f));
            hint.fontStyle = FontStyle.Normal; hint.color = new Color(.78f,.80f,.83f);

            var oldHeading = selection.GetComponentsInChildren<Text>(true).Where(t=>t.GetComponentInParent<Button>(true)==null).ToArray();
            foreach (var text in oldHeading) text.gameObject.SetActive(false);
            Label(selection.transform,"Selection brand","POSSESSION",30,new Vector2(.3f,.93f),new Vector2(.7f,.985f));
            Label(selection.transform,"Selection title","SELECT LEVEL",64,new Vector2(.2f,.81f),new Vector2(.8f,.94f));
            var back = selection.GetComponentsInChildren<Button>(true).First(b=>b.name=="BackButton");
            StyleButton(back,"← BACK",new Vector2(.025f,.89f),new Vector2(.15f,.965f),false);
            cards = levels; borders = new Image[levels.Length]; statuses = new Text[levels.Length];
            for (int i=0;i<levels.Length;i++)
            {
                var card=levels[i];
                var border=GoldTheme.Box(selection.transform,"Level "+(i+1)+" border",Vector2.zero,Vector2.one,GoldTheme.Secondary);
                borders[i]=border;
                border.sprite=rounded;border.type=Image.Type.Sliced;
                card.transform.SetParent(border.transform,false);
                GoldTheme.Fill((RectTransform)card.transform,new Vector2(.012f,.009f),new Vector2(.988f,.991f));
                card.targetGraphic=card.GetComponent<Image>();
                card.GetComponent<Image>().sprite=rounded;card.GetComponent<Image>().type=Image.Type.Sliced;
                card.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                var colors=card.colors; colors.normalColor=new Color(.16f,.17f,.19f); colors.highlightedColor=new Color(.22f,.24f,.27f);
                colors.selectedColor=colors.highlightedColor; colors.disabledColor=new Color(.12f,.13f,.15f); card.colors=colors;
                foreach(var text in card.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);
                Art(card.transform,"Level "+(i+1)+" scene",i,new Vector2(0,.28f),Vector2.one);
                // Keep the original label first for existing navigation/accessibility consumers.
                var label=card.GetComponentsInChildren<Text>(true).First(); label.gameObject.SetActive(true);
                GoldTheme.Fill(label.rectTransform,new Vector2(.04f,.14f),new Vector2(.96f,.28f)); GoldTheme.Style(label,40,TextAnchor.MiddleCenter);
                statuses[i]=Label(card.transform,"Selection status","",22,new Vector2(.04f,.035f),new Vector2(.96f,.13f));
            }
            launch=GoldTheme.Button(selection.transform,"Play selected level","PLAY LEVEL 1",new Vector2(.30f,.035f),new Vector2(.70f,.135f),true);
            launch.onClick.AddListener(controller.OnPlaySelectedLevel);
            launch.GetComponent<Image>().sprite=rounded;launch.GetComponent<Image>().type=Image.Type.Sliced;
            foreach(var text in main.GetComponentsInChildren<Text>(true).Concat(selection.GetComponentsInChildren<Text>(true)))
            {
                if(text==subtitle||text==hint)continue;
                var font=Resources.Load<Font>(text.name=="Game title"?"Menu/Barlow-Black":"Menu/Barlow-Bold");
                if(font==null)continue;
                text.font=font;text.fontStyle=FontStyle.Normal;
                if(text.text.Contains("←")&&!font.HasCharacter('←'))text.text=text.text.Replace("←","<");
            }
            LayoutCards(); Refresh();
        }

        public void Refresh()
        {
            if (cards == null) return;
            for(int i=0;i<cards.Length;i++)
            {
                bool available=controller.CanPlayLevel(i),selected=controller.SelectedLevelIndex==i;
                cards[i].interactable=available;
                cards[i].GetComponentInChildren<Text>().text="LEVEL "+(i+1);
                foreach(var art in cards[i].GetComponentsInChildren<RawImage>()) art.color=new Color(1,1,1,available?1f:.45f);
                borders[i].color=selected?GoldTheme.Teal:GoldTheme.Secondary;
                statuses[i].text=!available?"LOCKED":selected?"SELECTED":"AVAILABLE";
                statuses[i].color=selected?GoldTheme.Teal:new Color(.72f,.75f,.79f);
            }
            launch.interactable=controller.CanPlayLevel(controller.SelectedLevelIndex);
            launch.GetComponentInChildren<Text>().text="PLAY LEVEL "+(controller.SelectedLevelIndex+1);
        }

        private void Update()
        {
            if (cards==null) return;
            if(Mathf.Abs(aspect-(float)Screen.width/Mathf.Max(1,Screen.height))>.01f)LayoutCards();
            if(!launch.gameObject.activeInHierarchy)return;
            if(Input.GetKeyDown(KeyCode.Escape)){controller.OnBackButton();return;}
            if(Input.GetKeyDown(KeyCode.Return))controller.OnPlaySelectedLevel();
            int direction=Input.GetKeyDown(KeyCode.RightArrow)?1:Input.GetKeyDown(KeyCode.LeftArrow)?-1:0;
            if(direction==0)return;
            int next=controller.SelectedLevelIndex+direction;
            if(controller.CanPlayLevel(next))controller.OnLevelButton(next);
        }

        private void LayoutCards()
        {
            LayoutForAspect((float)Screen.width/Mathf.Max(1,Screen.height));
        }
        public void LayoutForAspect(float value)
        {
            aspect=value;
            bool narrow=aspect<1.45f;
            for(int i=0;i<cards.Length;i++)
            {
                float width=narrow?.445f:.218f;
                float left=narrow?.04f+(i%2)*.475f:.035f+i*.237f;
                float bottom=narrow?.49f-(i/2)*.32f:.19f;
                GoldTheme.Fill((RectTransform)borders[i].transform,new Vector2(left,bottom),new Vector2(left+width,bottom+(narrow?.30f:.60f)));
            }
        }

        private static Text Label(Transform p,string name,string value,int size,Vector2 min,Vector2 max,TextAnchor alignment=TextAnchor.MiddleCenter)
            =>GoldTheme.Label(p,name,value,size,min,max,alignment);
        private void StyleButton(Button button,string label,Vector2 min,Vector2 max,bool primary)
        {
            button.transform.SetAsLastSibling();
            GoldTheme.Fill((RectTransform)button.transform,min,max);
            var image=button.GetComponent<Image>();image.color=Color.white;
            image.sprite=rounded;image.type=Image.Type.Sliced;
            var colors=button.colors;colors.normalColor=primary?GoldTheme.Teal:GoldTheme.Secondary;
            colors.highlightedColor=Color.Lerp(colors.normalColor,Color.white,.15f); colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=Color.Lerp(colors.normalColor,Color.black,.2f);button.colors=colors;
            var text=button.GetComponentInChildren<Text>(true);text.gameObject.SetActive(true);text.text=label;GoldTheme.Style(text,primary?54:44,TextAnchor.MiddleCenter);
            GoldTheme.Fill(text.rectTransform,new Vector2(.03f,.05f),new Vector2(.97f,.95f));
        }
        private Sprite RoundedSurface()
        {
            const int size=32;const float radius=8;
            roundTexture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Menu rounded surface",hideFlags=HideFlags.DontSave};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=x+.5f-Mathf.Clamp(x+.5f,radius,size-radius);
                float dy=y+.5f-Mathf.Clamp(y+.5f,radius,size-radius);
                roundTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            roundTexture.Apply();
            return Sprite.Create(roundTexture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
        }
        private void OnDestroy()
        {
            if(rounded!=null)Destroy(rounded);
            if(roundTexture!=null)Destroy(roundTexture);
        }
        private static void Art(Transform parent,string name,int index,Vector2 min,Vector2 max)
        {
            var clip=GoldTheme.Rect(parent,name,min,max);clip.gameObject.AddComponent<RectMask2D>();
            var image=GoldTheme.Rect(clip,"Preview",Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("Menu/Level"+(index+1));image.raycastTarget=false;
            if(image.texture==null){image.color=new Color(.23f,.26f,.30f);return;}
            var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio=(float)image.texture.width/image.texture.height;
        }
    }
}
