using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    // Native controls over the approved USC art; preview changes never save until Equip.
    public sealed class USCAvatarWardrobe : MonoBehaviour
    {
        private MainMenuController owner;
        private GameObject page,main,selection;
        private RawImage hero;
        private readonly Texture2D[] art=new Texture2D[4];
        private readonly Image[] borders=new Image[4],footers=new Image[4];
        private readonly GameObject[] checks=new GameObject[4];
        private readonly Text[] names=new Text[4];
        private Button equip;
        private Text message;
        private Sprite rounded,circle,backdrop;
        private Texture2D roundTexture,circleTexture;
        private int draft;
        private float aspect;
        private static readonly Color Ink=new Color(.085f,.095f,.115f),Panel=new Color(.16f,.17f,.20f);
        public bool IsOpen => page!=null&&page.activeSelf;
        public int Draft => draft;
        public void Build(MainMenuController controller,GameObject mainButtons,GameObject levelSelection,Sprite unused)
        {
            owner=controller;main=mainButtons;selection=levelSelection;
            rounded=Surface(false,out roundTexture);circle=Surface(true,out circleTexture);
            page=GoldTheme.Box(transform,"AvatarPage",Vector2.zero,Vector2.one,Ink).gameObject;
            var texture=Resources.Load<Texture2D>("USCAvatar/LockerBackground");
            if(texture!=null)
            {
                backdrop=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f);
                var background=GoldTheme.Box(page.transform,"USC locker artwork",Vector2.zero,Vector2.one,Color.white);background.sprite=backdrop;background.raycastTarget=false;
            }
            Label(page.transform,"Wardrobe brand","POSSESSION",58,new Vector2(.027f,.908f),new Vector2(.345f,.995f),TextAnchor.MiddleLeft);
            Label(page.transform,"Wardrobe heading","YOUR AVATAR",62,new Vector2(.555f,.745f),new Vector2(.897f,.875f));
            GoldTheme.Box(page.transform,"Heading left rule",new Vector2(.472f,.803f),new Vector2(.548f,.811f),PlayerAvatarSelection.Gold).raycastTarget=false;
            GoldTheme.Box(page.transform,"Heading right rule",new Vector2(.91f,.803f),new Vector2(.967f,.811f),PlayerAvatarSelection.Gold).raycastTarget=false;
            var heroFrame=GoldTheme.Rect(page.transform,"Player outfit preview",new Vector2(.12f,.17f),new Vector2(.35f,.72f));
            hero=Portrait(heroFrame,"Selected outfit");
            string[] paths={"Classic","Varsity","Hoodie","Sport"};
            for(int i=0;i<4;i++)
            {
                int index=i;art[i]=Resources.Load<Texture2D>("USCAvatar/"+paths[i]);
                var border=GoldTheme.Box(page.transform,"Outfit border "+i,Vector2.zero,Vector2.one,GoldTheme.Secondary);borders[i]=border;border.sprite=rounded;border.type=Image.Type.Sliced;
                var button=MakeButton(border.transform,"Outfit_"+i,"",Vector2.zero,Vector2.one,()=>Choose(index),Panel);
                var rect=button.GetComponent<RectTransform>();rect.offsetMin=Vector2.one*5;rect.offsetMax=Vector2.one*-5;
                button.GetComponentInChildren<Text>().gameObject.SetActive(false);button.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                var frame=GoldTheme.Rect(button.transform,"Portrait frame",new Vector2(.055f,.285f),new Vector2(.945f,.94f));
                var portrait=Portrait(frame,"Outfit portrait");portrait.texture=art[i];Fit(portrait);
                footers[i]=GoldTheme.Box(button.transform,"Outfit caption",Vector2.zero,new Vector2(1,.245f),Panel);footers[i].raycastTarget=false;
                names[i]=Label(button.transform,"Outfit name",PlayerAvatarSelection.Names[i],i==0?30:26,new Vector2(.025f,.015f),new Vector2(.975f,.235f));
                checks[i]=Check(button.transform);
            }
            MakeButton(page.transform,"AvatarBack","BACK",new Vector2(.02f,.04f),new Vector2(.188f,.139f),Close,new Color(.25f,.27f,.31f));
            message=Label(page.transform,"Wardrobe message","",20,new Vector2(.45f,.15f),new Vector2(.98f,.21f));message.color=new Color(.80f,.81f,.83f);
            equip=MakeButton(page.transform,"EquipAvatar","EQUIP",new Vector2(.713f,.035f),new Vector2(.983f,.141f),Equip,PlayerAvatarSelection.Gold);
            equip.GetComponentInChildren<Text>().color=PlayerAvatarSelection.Cardinal;
            page.SetActive(false);Layout();
        }
        public void Open()
        {
            draft=PlayerAvatarSelection.Selected;main.SetActive(false);selection.SetActive(false);page.SetActive(true);page.transform.SetAsLastSibling();Layout();Refresh();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(borders[draft].GetComponentInChildren<Button>().gameObject);
        }
        public void Choose(int index){draft=PlayerAvatarSelection.Valid(index);Refresh();}
        public void Equip(){PlayerAvatarSelection.Equip(draft);Refresh();message.text="Outfit equipped.";}
        public void Close(){page.SetActive(false);owner.OnBackButton();}
        private void Refresh()
        {
            for(int i=0;i<4;i++)
            {
                bool selected=i==draft;borders[i].color=selected?PlayerAvatarSelection.Gold:new Color(.30f,.31f,.35f);
                footers[i].color=selected?PlayerAvatarSelection.Cardinal:Panel;checks[i].SetActive(selected);
            }
            bool equipped=PlayerPrefs.HasKey(PlayerAvatarSelection.Key)&&PlayerPrefs.GetInt(PlayerAvatarSelection.Key)==draft;
            equip.interactable=!equipped;equip.GetComponentInChildren<Text>().text=equipped?"EQUIPPED":"EQUIP";message.text="";
            hero.texture=art[draft];Fit(hero);
        }
        private static RawImage Portrait(Transform parent,string name)
        {
            var image=GoldTheme.Rect(parent,name,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            image.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;return image;
        }
        private static void Fit(RawImage image){if(image.texture!=null)image.GetComponent<AspectRatioFitter>().aspectRatio=(float)image.texture.width/image.texture.height;}
        private void Update(){if(!IsOpen)return;if(Mathf.Abs(aspect-(float)Screen.width/Mathf.Max(1,Screen.height))>.01f)Layout();if(Input.GetKeyDown(KeyCode.Escape))Close();}
        private void Layout()=>LayoutForAspect((float)Screen.width/Mathf.Max(1,Screen.height));
        public void LayoutForAspect(float value)
        {
            aspect=value;bool narrow=aspect<1.45f;
            for(int i=0;i<4;i++)
            {
                float left=narrow?.455f+(i%2)*.265f:.452f+i*.137f;
                float bottom=narrow?.475f-(i/2)*.265f:.23f;
                GoldTheme.Fill(borders[i].rectTransform,new Vector2(left,bottom),new Vector2(left+(narrow?.25f:.128f),bottom+(narrow?.255f:.47f)));
            }
        }
        private Text Label(Transform parent,string name,string value,int size,Vector2 min,Vector2 max,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var text=GoldTheme.Label(parent,name,value,size,min,max,align);text.raycastTarget=false;
            text.font=Resources.Load<Font>(size>=48?"Menu/Barlow-Black":"Menu/Barlow-Bold");text.fontStyle=FontStyle.Normal;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Max(12,size*2/3);text.resizeTextMaxSize=size;return text;
        }
        private Button MakeButton(Transform parent,string name,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,Color color)
        {
            var button=GoldTheme.Button(parent,name,value,min,max,true);button.onClick.AddListener(action);
            var image=button.GetComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;
            var colors=button.colors;colors.normalColor=color;colors.highlightedColor=Color.Lerp(color,Color.white,.08f);colors.selectedColor=color;colors.pressedColor=Color.Lerp(color,Color.black,.15f);colors.disabledColor=Color.Lerp(color,Ink,.20f);button.colors=colors;
            var text=button.GetComponentInChildren<Text>();text.font=Resources.Load<Font>("Menu/Barlow-Bold");text.fontStyle=FontStyle.Normal;text.fontSize=name=="EquipAvatar"?50:36;
            if(name=="AvatarBack")
            {
                GoldTheme.Fill(text.rectTransform,new Vector2(.22f,.05f),new Vector2(.94f,.95f));
                for(int i=0;i<2;i++){var line=GoldTheme.Box(button.transform,"Back arrow",new Vector2(.14f,.5f),new Vector2(.14f,.5f),Color.white);line.raycastTarget=false;line.rectTransform.sizeDelta=new Vector2(6,23);line.rectTransform.anchoredPosition=new Vector2(0,i==0?7:-7);line.rectTransform.localRotation=Quaternion.Euler(0,0,i==0?-45:45);}
            }
            return button;
        }
        private GameObject Check(Transform parent)
        {
            var image=GoldTheme.Box(parent,"Selected check",Vector2.one,Vector2.one,PlayerAvatarSelection.Gold);image.sprite=circle;image.raycastTarget=false;
            var r=image.rectTransform;r.pivot=Vector2.one;r.sizeDelta=new Vector2(38,38);r.anchoredPosition=new Vector2(-10,-10);
            var a=GoldTheme.Box(r,"Check left",Vector2.one*.5f,Vector2.one*.5f,Ink);a.raycastTarget=false;a.rectTransform.sizeDelta=new Vector2(5,14);a.rectTransform.anchoredPosition=new Vector2(-7,-2);a.rectTransform.localRotation=Quaternion.Euler(0,0,42);
            var b=GoldTheme.Box(r,"Check right",Vector2.one*.5f,Vector2.one*.5f,Ink);b.raycastTarget=false;b.rectTransform.sizeDelta=new Vector2(5,23);b.rectTransform.anchoredPosition=new Vector2(4,1);b.rectTransform.localRotation=Quaternion.Euler(0,0,-42);return image.gameObject;
        }
        private static Sprite Surface(bool disk,out Texture2D texture)
        {
            const int size=64;float radius=disk?32:18;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=x+.5f-Mathf.Clamp(x+.5f,radius,size-radius),dy=y+.5f-Mathf.Clamp(y+.5f,radius,size-radius);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy))));}texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,disk?Vector4.zero:new Vector4(18,18,18,18));
        }
        private static void Dispose(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){Dispose(rounded);Dispose(circle);Dispose(backdrop);Dispose(roundTexture);Dispose(circleTexture);}
    }
}
