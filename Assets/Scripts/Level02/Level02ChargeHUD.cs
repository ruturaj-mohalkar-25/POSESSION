using UnityEngine;
using UnityEngine.UI;
namespace Possession
{
    public sealed class Level02ChargeHUD : MonoBehaviour
    {
        private Level02Director director;
        private Text state;
        private Button checkpoint;
        public void Initialize(Level02Director level)
        {
            director = level;
            var go = new GameObject("Charge HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; go.GetComponent<Canvas>().sortingOrder = 15;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 0.5f;
            var strip = Panel(go.transform,"Charge state",new Vector2(0.5f,1f),new Vector2(0,-50),new Vector2(720,90));
            Label(strip,"HUNTED · FIND A ROUTE TO THE EXIT",new Vector2(0,20),new Vector2(700,36),25);
            state = Label(strip,"",new Vector2(0,-20),new Vector2(700,36),24);
            var retry = Panel(go.transform,"Checkpoint",new Vector2(1f,1f),new Vector2(-20,-80),new Vector2(300,44));
            retry.GetComponent<Image>().raycastTarget=true;
            checkpoint=retry.gameObject.AddComponent<Button>(); checkpoint.targetGraphic=retry.GetComponent<Image>(); checkpoint.onClick.AddListener(director.RetryCourtyard);
            Label(retry,"C · RETRY COURTYARD",Vector2.zero,new Vector2(290,40),22);
            NavButton(go.transform,"Menu", "MENU",new Vector2(-20,-20),director.MainMenu);
            NavButton(go.transform,"Restart", "R · RESTART",new Vector2(-180,-20),director.FullRestart);
        }
        private void Update()
        {
            if (director == null || state == null) return;
            checkpoint.interactable=director.CheckpointReached;
            state.text = director.CanExit ? "Route clear · Reach the exit in any form"
                : director.Possession.CurrentState != PlayerState.Spirit ? "Body borrowed · E to eject · R to restart"
                : director.Charge.HasCharge ? "POSSESSION READY · E near a body"
                : "Find an energy shard to possess a body";
            state.color=director.Charge.HasCharge || director.CanExit ? new Color(0.45f,1f,0.7f) : Color.white;
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=anchor; rect.anchoredPosition=pos; rect.sizeDelta=size;
            go.GetComponent<Image>().color=new Color(0.04f,0.07f,0.09f,0.9f); go.GetComponent<Image>().raycastTarget=false; return rect;
        }
        private static void NavButton(Transform parent,string name,string title,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(parent,name,new Vector2(1,1),position,new Vector2(140,40));
            rect.GetComponent<Image>().raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(action);
            Label(rect,title,Vector2.zero,new Vector2(135,36),21);
        }
        private static Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int fontSize)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*0.5f; rect.anchoredPosition=pos; rect.sizeDelta=size;
            var label=go.GetComponent<Text>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=fontSize; label.text=text; label.color=Color.white; label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false; return label;
        }
    }
}
