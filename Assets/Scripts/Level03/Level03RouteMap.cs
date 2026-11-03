using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Possession
{
    public sealed class Level03RouteMap : MonoBehaviour
    {
        [SerializeField] private TextAsset layout;
        [Serializable] private class Plan { public Floor[] floors; }
        [Serializable] private class Floor { public Area[] rooms,halls; }
        [Serializable] private class Area { public float x,y,width,height; }
        private Level03Director level;
        private GameObject overlay;
        private RectTransform[] frames = new RectTransform[2];
        private RectTransform playerDot;
        private readonly List<(PossessionChargePickup source,Image marker)> pickups = new();
        private readonly List<(Level03SecurityGate gate,Image marker)> gates = new();
        private readonly List<(Behaviour component,bool enabled)> inputs = new();
        private float previousScale;
        public bool IsOpen { get; private set; }
        private void Start() { level=GetComponent<Level03Director>();Build(); }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.M)||IsOpen&&Input.GetKeyDown(KeyCode.Escape)) { if(IsOpen)Close();else Open(); }
            if(!IsOpen)return;
            if(GameManager.Instance==null||GameManager.Instance.CurrentState!=LevelState.InProgress){Close();return;}
            Refresh();
        }
        public bool Open()
        {
            if(IsOpen||overlay==null||Time.timeScale<=0f||GameManager.Instance.CurrentState!=LevelState.InProgress)return false;
            previousScale=Time.timeScale;inputs.Clear();
            Suspend(level.Possession);Suspend(level.Spirit);
            foreach(var host in level.Hosts){Suspend(host.GetComponent<EnemyContactDamage>());Suspend(host.GetComponent<Level03EnemyPressure>());}
            IsOpen=true;Time.timeScale=0f;overlay.SetActive(true);Refresh();return true;
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;overlay.SetActive(false);RestoreInputs();
            Time.timeScale=GameManager.Instance!=null&&GameManager.Instance.CurrentState==LevelState.InProgress?previousScale:0f;
        }
        private void Suspend(Behaviour component) { if(component!=null){inputs.Add((component,component.enabled));component.enabled=false;} }
        private void RestoreInputs(){foreach(var entry in inputs)if(entry.component!=null)entry.component.enabled=entry.enabled;inputs.Clear();}
        private void OnDisable(){if(!IsOpen)return;IsOpen=false;RestoreInputs();Time.timeScale=1f;}
        private void Refresh()
        {
            var player=level.Possession.PossessedEnemy!=null?level.Possession.PossessedEnemy.transform:level.Spirit.transform;
            playerDot.SetParent(frames[FloorIndex(player.position)],false);playerDot.anchoredPosition=MapPoint(player.position);playerDot.SetAsLastSibling();
            foreach(var p in pickups)p.marker.color=p.source.IsReady?new Color(.3f,1f,.65f):new Color(.25f,.3f,.33f);
            foreach(var g in gates)g.marker.color=g.gate.IsOpen?new Color(.2f,.85f,.55f):new Color(1f,.4f,.25f);
        }
        private static int FloorIndex(Vector3 point)=>point.y>5.5f?1:0;
        private static Vector2 MapPoint(Vector3 point)=>new Vector2(point.x/Level03Director.LayoutUnit*14f,point.z/Level03Director.LayoutUnit*14f);
        private void Build()
        {
            overlay=new GameObject("Route map — paused",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));overlay.transform.SetParent(transform,false);
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var backdrop=Box(overlay.transform,"Backdrop",Vector2.zero,new Vector2(2400,1600),new Color(.025f,.035f,.055f,1f));backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;backdrop.GetComponent<Image>().raycastTarget=true;
            Text(overlay.transform,"ROUTE MAP · PAUSED",new Vector2(0,430),new Vector2(1200,60),34,Color.white);
            Text(overlay.transform,"Choose A or B to reach the upper-floor exit. C is an optional shortcut.",new Vector2(0,375),new Vector2(1600,45),24,new Color(.7f,.8f,.9f));
            var close=Box(overlay.transform,"Resume",new Vector2(690,440),new Vector2(220,55),new Color(.18f,.3f,.4f));close.GetComponent<Image>().raycastTarget=true;var button=close.gameObject.AddComponent<Button>();button.targetGraphic=close.GetComponent<Image>();button.onClick.AddListener(Close);Text(close,"RESUME [M]",Vector2.zero,new Vector2(220,50),24,Color.white);
            var plan=JsonUtility.FromJson<Plan>(layout.text);
            string[][] names={new[]{"SECURITY","MAIN A","MAINT. C","START","COURTYARD","SERVICE B","D1","D2"},new[]{"OPERATIONS","GALLERY","EXTRACTION","WORKSHOP","RECORDS","EAST CONTROL","D3","D4"}};
            for(int floor=0;floor<2;floor++)
            {
                var frame=Box(overlay.transform,floor==0?"Ground plan":"Upper plan",new Vector2(floor==0?-400:400,10),new Vector2(740,600),new Color(.06f,.085f,.12f));frames[floor]=frame;
                Text(frame,floor==0?"GROUND FLOOR":"UPPER FLOOR",new Vector2(0,265),new Vector2(600,45),28,new Color(.8f,.87f,1f));
                foreach(var area in plan.floors[floor].halls)AreaBox(frame,area,new Color(.23f,.29f,.37f));
                for(int i=0;i<plan.floors[floor].rooms.Length;i++)
                {
                    var area=plan.floors[floor].rooms[i];AreaBox(frame,area,new Color(.28f,.35f,.44f));
                    Text(frame,names[floor][i],new Vector2((area.x+area.width/2-18)*14,(14-area.y+.5f)*14),new Vector2(area.width*14,25),14,new Color(.9f,.93f,1f));
                }
                Stair(frame,"A",16,3,new Color(.25f,.55f,.9f));Stair(frame,"B",29,19,new Color(.8f,.54f,.17f));Stair(frame,"C",29,3,new Color(.25f,.55f,.6f));
            }
            foreach(var source in FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                pickups.Add((source,Box(frames[FloorIndex(source.transform.position)],"Shard",MapPoint(source.transform.position),Vector2.one*9,new Color(.3f,1f,.65f)).GetComponent<Image>()));
            foreach(var gate in FindObjectsByType<Level03SecurityGate>(FindObjectsSortMode.None))
                gates.Add((gate,Box(frames[FloorIndex(gate.transform.position)],"Gate",MapPoint(gate.transform.position),new Vector2(14,7),new Color(1f,.4f,.25f)).GetComponent<Image>()));
            foreach(var target in FindObjectsByType<Target>(FindObjectsSortMode.None))Text(frames[FloorIndex(target.transform.position)],target.name,MapPoint(target.transform.position),new Vector2(38,20),13,new Color(1f,.85f,.35f));
            foreach(var wall in FindObjectsByType<BreakableWall>(FindObjectsInactive.Include,FindObjectsSortMode.None))Text(frames[FloorIndex(wall.transform.position)],wall.name,MapPoint(wall.transform.position),new Vector2(38,20),13,new Color(1f,.65f,.3f));
            var exit=FindFirstObjectByType<Level03Exit>();Text(frames[1],"EXIT",MapPoint(exit.transform.position),new Vector2(50,25),16,new Color(.35f,1f,.65f));
            playerDot=Box(frames[0],"You",Vector2.zero,Vector2.one*12,Color.white);
            Text(overlay.transform,"A: GUARD → T1     B: HEAVY → B1     C: R → B3",new Vector2(0,-365),new Vector2(1700,45),25,Color.white);
            Text(overlay.transform,"UPSTAIRS: shoot T3, or unlock SE and smash PE.",new Vector2(0,-410),new Vector2(1700,40),24,new Color(.7f,.82f,.94f));
            Text(overlay.transform,"WHITE = YOU     GREEN = SHARD / OPEN GATE     RED = LOCKED GATE",new Vector2(0,-465),new Vector2(1700,40),21,new Color(.65f,.74f,.85f));
            overlay.SetActive(false);
        }
        private static void AreaBox(Transform parent,Area area,Color color)=>Box(parent,"Room",new Vector2((area.x+area.width/2-18)*14,(14-area.y-area.height/2)*14),new Vector2(area.width*14,area.height*14),color);
        private static void Stair(Transform parent,string name,float x,float y,Color color)
        {var rect=Box(parent,"Stair "+name,new Vector2((x+2.5f-18)*14,(14-y-3)*14),new Vector2(5*14,6*14),color);Text(rect,name,Vector2.zero,new Vector2(60,45),22,Color.white);}
        private static RectTransform Box(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;}
        private static void Text(Transform parent,string title,Vector2 position,Vector2 size,int fontSize,Color color)
        {var go=new GameObject(title,typeof(RectTransform),typeof(Text));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=title;text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;}
    }
}
