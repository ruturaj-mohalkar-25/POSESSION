using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Possession.Editor
{
    public static class WebPreviewSetup
    {
        [MenuItem("Possession/Prepare Combined Web Preview")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before preparing the preview.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ConfigureCollectibles("Level_01", new[] { new Vector3(-3f,.1f,9f), new Vector3(3f,.1f,27f) });
            ConfigureCollectibles("Level_03", new[] { new Vector3(-1.5f,.1f,10.5f), new Vector3(-8f,.1f,24f),
                new Vector3(6.5f,.1f,36f), new Vector3(10f,.1f,20f) });
            ConfigureCollectibles("Level_02", null);
            ConfigureCollectibles(TutorialScene.Name, null);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true)
            };
            foreach (string name in LevelFlow.CampaignScenes)
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (!File.Exists(path)) throw new FileNotFoundException(path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            TutorialBuildRegistration.Ensure();
            HUDSetup.SetupMainMenu();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureCollectibles(string sceneName, Vector3[] positions)
        {
            string path = "Assets/Scenes/" + sceneName + ".unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var spirit = roots.SelectMany(r => r.GetComponentsInChildren<SpiritController>(true)).Single();
            var charge = spirit.GetComponent<PossessionCharge>() ?? spirit.gameObject.AddComponent<PossessionCharge>();
            var settings = new SerializedObject(charge);
            settings.FindProperty("startCharges").intValue = PossessionCharge.SharedStartCharges;
            settings.FindProperty("maxCharges").intValue = PossessionCharge.SharedMaxCharges;
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (positions != null)
            {
                var group = roots.FirstOrDefault(r => r.name == "SharedChargePickups")
                    ?? new GameObject("SharedChargePickups");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoulCoinPrefabBuilder.PrefabPath);
                if (prefab == null) throw new FileNotFoundException(SoulCoinPrefabBuilder.PrefabPath);
                for (int i = 0; i < positions.Length; i++)
                {
                    string name = "SoulCoin_" + (i + 1).ToString("00");
                    var existing = group.transform.Find(name);
                    var coin = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    coin.name = name;
                    coin.transform.SetParent(group.transform, true);
                    coin.transform.position = positions[i];
                    ConfigureRecovery(coin.GetComponent<PossessionChargePickup>());
                }
            }
            // Practice must remain recoverable when a new player spends a charge or misses a shot.
            if (sceneName == TutorialScene.Name)
                foreach (var pickup in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PossessionChargePickup>(true)))
                    ConfigureRecovery(pickup);
            if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PossessionChargePickup>(true)).Count() < 2)
                throw new System.InvalidOperationException(sceneName + " needs reachable Soul Coins.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureRecovery(PossessionChargePickup pickup)
        {
            var settings = new SerializedObject(pickup);
            settings.FindProperty("reforms").boolValue = true;
            settings.FindProperty("cooldown").floatValue = 8f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
