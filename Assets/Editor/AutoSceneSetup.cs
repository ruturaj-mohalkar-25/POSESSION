using UnityEditor;
using UnityEngine;

namespace Possession.Editor
{
    [InitializeOnLoad]
    public static class AutoSceneSetup
    {
        static AutoSceneSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!System.IO.File.Exists("Assets/Scenes/Level_01.unity"))
                {
                    Debug.Log("No Level_01 found. Building base scene...");
                    SceneBuilder.BuildLevel01();
                    Debug.Log("Level_01 created automatically. Hit Play to test.");
                }
            };
        }
    }
}
