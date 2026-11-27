using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Possession.Editor
{
    public static class ResponsiveWebPreview
    {
        private const string Marker="/* POSESSION responsive desktop preview */";
        [PostProcessBuild(50)]
        public static void AfterBuild(BuildTarget target,string path)
        {
            if(target!=BuildTarget.WebGL)return;
            var stylesheet=Path.Combine(path,"TemplateData/style.css");
            if(!File.Exists(stylesheet)||File.ReadAllText(stylesheet).Contains(Marker))return;
            File.AppendAllText(stylesheet,"\n"+Marker+"\n"+File.ReadAllText("Assets/Editor/WebPreviewResponsive.css"));
        }
    }
}
