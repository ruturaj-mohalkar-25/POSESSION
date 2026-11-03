using UnityEngine;
using UnityEngine.UI;
namespace Possession
{
    public sealed class Level03StatusHUD : MonoBehaviour
    {
        private Level03Director level;
        private Text status, circuits, prompt;
        private Button retry;
        public void Initialize(Level03Director director)
        {
            level=director;
            var go=new GameObject("Compound status",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=15;
            var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
            status=Label(go.transform,"",new Vector2(0,-24),new Vector2(1050,42),23);
            circuits=Label(go.transform,"",new Vector2(0,-66),new Vector2(800,36),22);
            prompt=Label(go.transform,"",new Vector2(0,-105),new Vector2(900,32),20);
            Button(go.transform,"MENU",new Vector2(-20,-20),level.MainMenu);
            Button(go.transform,"R · RESTART",new Vector2(-20,-68),level.FullRestart);
            retry=Button(go.transform,"C · RETRY",new Vector2(-20,-116),level.RetryCheckpoint);
            Button(go.transform,"M · MAP",new Vector2(-20,-164),()=>level.GetComponent<Level03RouteMap>().Open());
        }
        private void Update()
        {
            if (level==null) return;
            retry.interactable=level.HasCheckpoint;
            Transform player=level.Possession.PossessedEnemy!=null?level.Possession.PossessedEnemy.transform:level.Spirit.transform;
            status.text=(player.position.y>5.5f?"UPPER FLOOR":"GROUND FLOOR")+" · "+level.NavigationHint;
            circuits.text=Circuit("WEST",level.West)+"     "+Circuit("EAST",level.East);
            circuits.color=level.West.Latched||level.East.Latched?new Color(1f,.4f,.3f):new Color(.6f,.8f,1f);
            string charge=level.Possession.CurrentState!=PlayerState.Spirit?"BODY BORROWED":level.Charge.HasCharge?"SHARD READY · GET BEHIND ENEMY":"FIND A GREEN SHARD";
            prompt.text=level.NearbyCheckpoint!=null ? charge+"  ·  F · SAVE CHECKPOINT "
                : charge+"  ·  M · PAUSE MAP"+(level.HasCheckpoint?"  ·  C · "+level.CheckpointName:"");
        }
        private static string Circuit(string title,Level03Circuit circuit) => title+": "+(circuit.Disabled?"SHUT DOWN":circuit.Latched?"LOCKDOWN":!circuit.Connected?"DISCONNECTED":circuit.Suspicion>0?"WARNING":"ARMED");
        private static Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int font)
        {
            var go=new GameObject("Status",typeof(RectTransform),typeof(Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=pos;rect.sizeDelta=size;go.GetComponent<Image>().color=new Color(.03f,.045f,.07f,.88f);go.GetComponent<Image>().raycastTarget=false;
            var child=new GameObject("Text",typeof(RectTransform),typeof(Text));var r=child.GetComponent<RectTransform>();r.SetParent(rect,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var label=child.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=font;label.text=text;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
        }
        private static Button Button(Transform parent,string title,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var label=Label(parent,title,position,new Vector2(180,40),21);var rect=label.transform.parent.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;
            var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);return button;
        }
    }
}
