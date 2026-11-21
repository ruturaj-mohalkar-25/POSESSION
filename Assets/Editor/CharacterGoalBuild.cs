using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Possession.Editor
{
    public static class CharacterGoalBuild
    {
        public static void Main()=>CaptureAndBuild(false);
        public static void Stretch()=>CaptureAndBuild(true);
        private static void CaptureAndBuild(bool stretch)
        {
            string folder=stretch?"Docs/StretchGoal":"Docs/MainGoal";Directory.CreateDirectory(folder);
            foreach(string path in CampaignLevels.ScenePaths.Concat(new[]{"Assets/Scenes/Tutorial.unity"}))
            {
                var scene=EditorSceneManager.OpenScene(path);
                Func<string> geometry=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>(true))
                    .OrderBy(c=>c.GetInstanceID()).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)+"|"+c.transform.position.ToString("R")+"|"+c.transform.lossyScale.ToString("R")));
                string before=geometry();GoldPresentationInstaller.Install(scene);GoldRoomDecor.Install(scene);CharacterPresentationInstaller.Install(scene);
                foreach(var root in scene.GetRootGameObjects())foreach(var view in root.GetComponentsInChildren<CharacterAvatarView>(true))view.Refresh();
                foreach(var root in scene.GetRootGameObjects())foreach(var identity in root.GetComponentsInChildren<PlayerIdentityView>(true))identity.Refresh();
                foreach(var root in scene.GetRootGameObjects())foreach(var shadows in root.GetComponentsInChildren<GoldContactShadows>(true))shadows.Refresh();
                if(before!=geometry())throw new InvalidOperationException("Character presentation changed colliders: "+scene.name);
                var camera=Camera.main;camera.cullingMask&=~(1<<5);
                var spirit=UnityEngine.Object.FindFirstObjectByType<SpiritController>();
                camera.transform.position=spirit.transform.position+new Vector3(0,25,-18);camera.transform.LookAt(spirit.transform.position);
                Save(camera,folder+"/"+scene.name+"-overview.png");
                if(!stretch)
                {
                    foreach(var actor in new Component[]{spirit,UnityEngine.Object.FindFirstObjectByType<GuardEnemy>()})
                    {
                        if(actor==null)continue;
                        var body=actor.GetComponent<Renderer>();var point=body.bounds.center;
                        var front=actor is GuardEnemy?actor.transform.forward:Vector3.back;
                        camera.transform.position=point+front*4+Vector3.up*3.5f;camera.transform.LookAt(point);
                        SaveClear(camera,point,folder+"/"+scene.name+(actor is GuardEnemy?"-police.png":"-player.png"));
                    }
                }
                else
                {
                    var props=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Decor_Painting"||t.name=="Decor_WallTV"||t.name=="Decor_CornerVase"||t.name=="Decor_CornerPlant").ToArray();
                    foreach(string kind in new[]{"Decor_Painting","Decor_WallTV","Decor_CornerVase","Decor_CornerPlant"})
                    {
                        var prop=props.FirstOrDefault(t=>t.name==kind);if(prop==null)continue;
                        camera.transform.position=prop.position+new Vector3(0,4,-5);camera.transform.LookAt(prop.position+Vector3.up*.4f);
                        SaveClear(camera,prop.position,folder+"/"+scene.name+"-"+kind+".png");
                    }
                }
                Debug.Log("CHARACTER_GOAL_GEOMETRY_OK "+scene.name);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");PlaytestFeedbackBuild.WebGL();
        }
        private static void SaveClear(Camera camera,Vector3 point,string path)
        {
            var hidden=new List<Renderer>();var direction=point-camera.transform.position;
            foreach(var hit in Physics.RaycastAll(camera.transform.position,direction.normalized,direction.magnitude,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponentInParent<EnemyBase>()!=null||hit.collider.GetComponentInParent<SpiritController>()!=null)continue;
                var renderer=hit.collider.GetComponent<Renderer>();if(renderer!=null&&renderer.enabled){renderer.enabled=false;hidden.Add(renderer);}
            }
            foreach(var label in UnityEngine.Object.FindObjectsByType<WorldLabel>(FindObjectsSortMode.None))
                foreach(var renderer in label.GetComponentsInChildren<Renderer>())if(renderer.enabled){renderer.enabled=false;hidden.Add(renderer);}
            try{Save(camera,path);}finally{foreach(var renderer in hidden)if(renderer!=null)renderer.enabled=true;}
        }
        private static void Save(Camera camera,string path)
        {
            var rt=new RenderTexture(1440,900,24);var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
            var target=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
