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
        public static void CaptureRolePortraits()
        {
            Directory.CreateDirectory("Assets/Resources/CharacterGuide");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.72f,.75f,.82f);
            var light=new GameObject("Portrait key").AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.3f;
            light.transform.rotation=Quaternion.Euler(35,-25,0);
            var camera=new GameObject("Portrait camera").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GoldTheme.Panel;
            camera.orthographic=true;camera.orthographicSize=1.28f;
            camera.transform.position=new Vector3(3,2.2f,-7);
            camera.transform.LookAt(new Vector3(0,.94f,0));
            foreach(var role in new[]{"GUARD","HEAVY","ENGINEER"})
            {
                var host=GameObject.CreatePrimitive(PrimitiveType.Capsule);
                if(role=="GUARD")host.AddComponent<GuardEnemy>();
                else if(role=="HEAVY")host.AddComponent<HeavyEnemy>();
                else host.AddComponent<EngineerEnemy>();
                var view=host.AddComponent<CharacterAvatarView>();view.Build();view.Refresh();
                view.Model.localRotation=Quaternion.identity;
                host.transform.position=Vector3.up;
                Save(camera,"Assets/Resources/CharacterGuide/"+role+".png",768,768);
                Object.DestroyImmediate(host);
            }
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles("Assets/Resources/CharacterGuide","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            CaptureCharacterGuide();
        }

        public static void CaptureCharacterGuide()
        {
            Directory.CreateDirectory("Docs/CharacterGuide");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menu=Object.FindFirstObjectByType<MainMenuController>();
            TutorialMenuEntry.Install(menu.gameObject.scene);
            typeof(MainMenuController).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(menu,null);
            var canvas=menu.GetComponent<Canvas>();var camera=Camera.main;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
            menu.GetComponent<CharacterGuideView>().Open();
            Save(camera,"Docs/CharacterGuide/character-guide.png",1600,900);
            Debug.Log("CHARACTER_GUIDE_CAPTURED");
        }

        public static void PrepareWardrobeArt()
        {
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles("Assets/Resources/USCAvatar","*.png"))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=!path.Contains("LockerBackground");
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=path.Contains("LockerBackground")?2048:1024;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            }
        }
        public static void CaptureWardrobe()
        {
            PrepareWardrobeArt();
            Directory.CreateDirectory("Docs/AvatarWardrobe");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menu=Object.FindFirstObjectByType<MainMenuController>();
            TutorialMenuEntry.Install(menu.gameObject.scene);
            typeof(MainMenuController).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(menu,null);
            var wardrobe=menu.GetComponent<USCAvatarWardrobe>();wardrobe.Open();wardrobe.Choose(1);wardrobe.LayoutForAspect(16f/9f);
            var canvas=menu.GetComponent<Canvas>();var camera=Camera.main;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
            Save(camera,"Docs/AvatarWardrobe/usc-locker.png",1600,900);
            Debug.Log("USC_WARDROBE_CAPTURED");
        }
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
