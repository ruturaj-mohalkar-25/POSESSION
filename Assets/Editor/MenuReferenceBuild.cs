using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Possession.Level04Gold;

namespace Possession.Editor
{
    public static class MenuReferenceBuild
    {
        public static void CaptureUI()
        {
            Directory.CreateDirectory("Docs/MenuReference");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menu=Object.FindFirstObjectByType<MainMenuController>();
            TutorialMenuEntry.Install(menu.gameObject.scene);
            typeof(MainMenuController).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(menu,null);
            menu.GetComponent<ReferenceMenuView>().LayoutForAspect(16f/9f);
            var canvas=menu.GetComponent<Canvas>();var camera=Camera.main;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
            Canvas.ForceUpdateCanvases();
            Save(camera,"Docs/MenuReference/main-menu.png",1600,900);
            menu.OnPlayButton();Canvas.ForceUpdateCanvases();
            Save(camera,"Docs/MenuReference/level-select.png",1600,900);
            Debug.Log("MENU_REFERENCE_UI_READY");
        }
        public static void CaptureArt()
        {
            Directory.CreateDirectory("Assets/Resources/Menu");
            var paths=CampaignLevels.PlayableScenePaths;
            for(int i=0;i<paths.Length;i++)
            {
                var scene=EditorSceneManager.OpenScene(paths[i]);
                GoldPresentationInstaller.Install(scene);
                if(i==3) Object.FindFirstObjectByType<Level4WorldPresentation>().Apply(scene);
                CharacterPresentationInstaller.Install(scene);
                foreach(var view in Object.FindObjectsByType<CharacterAvatarView>(FindObjectsSortMode.None))view.Refresh();
                foreach(var identity in Object.FindObjectsByType<PlayerIdentityView>(FindObjectsSortMode.None))identity.Refresh();
                var camera=Camera.main;
                camera.cullingMask &= ~(1<<5);
                var spirit=Object.FindFirstObjectByType<SpiritController>();
                var point=spirit.transform.position+Vector3.forward*(i==3?6:7);
                camera.transform.position=point+new Vector3(8,22,-16);
                camera.transform.LookAt(point);
                camera.orthographic=false;camera.fieldOfView=48;
                foreach(var label in Object.FindObjectsByType<WorldLabel>(FindObjectsSortMode.None))
                    foreach(var renderer in label.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                Save(camera,"Assets/Resources/Menu/Level"+(i+1)+".png");
                foreach(var lighting in Object.FindObjectsByType<CharacterShadowLighting>(FindObjectsSortMode.None))
                    typeof(CharacterShadowLighting).GetMethod("Release",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(lighting,null);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.Refresh();
            Debug.Log("MENU_REFERENCE_ART_READY");
        }
        private static void Save(Camera camera,string path,int width=1100,int height=1300)
        {
            var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var target=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=target;RenderTexture.active=active;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
    }
}
