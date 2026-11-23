using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class RoomDecorPreview
    {
        public static void CaptureAndBuild()
        {
            Directory.CreateDirectory("Docs/RoomDecor");
            foreach(string path in CampaignLevels.ScenePaths)
            {
                var scene=EditorSceneManager.OpenScene(path);
                GoldPresentationInstaller.Install(scene);
                var before=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();
                GoldRoomDecor.Install(scene);
                var after=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .Select(c=>c.GetInstanceID()).OrderBy(i=>i).ToArray();
                if(!before.SequenceEqual(after))throw new InvalidOperationException("Decor changed colliders: "+scene.name);
                var props=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Decor_")).ToArray();
                Debug.Log("ROOM_DECOR "+scene.name+": "+string.Join(", ",props.GroupBy(t=>t.name).Select(g=>g.Key+"="+g.Count())));
                var camera=Camera.main;camera.cullingMask&=~(1<<5);
                var player=UnityEngine.Object.FindFirstObjectByType<SpiritController>();
                camera.transform.position=player.transform.position+new Vector3(0,25,-18);camera.transform.LookAt(player.transform.position);
                Save(camera,scene.name+"-room");
                foreach(var kind in new[]{"Decor_Painting","Decor_WallTV","Decor_CornerPlant","Decor_CornerVase","Decor_FloorMat"})
                {
                    var prop=props.FirstOrDefault(t=>t.name==kind);if(prop==null)continue;
                    camera.transform.position=prop.position+new Vector3(0,4,-5);camera.transform.LookAt(prop.position+Vector3.up*.4f);
                    Save(camera,scene.name+"-"+kind);
                }
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            PlaytestFeedbackBuild.WebGL();
        }
        private static void Save(Camera camera,string name)
        {
            var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var old=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Docs/RoomDecor/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
