using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Warehouse
{
    // A loaded scene may predate externally saved builder changes. Install the same opt-in setup on Play.
    public static class WarehouseSceneSafety
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) Install(SceneManager.GetSceneAt(i));
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);
        public static void Install(Scene scene)
        {
            if (!scene.isLoaded || scene.name != "Level_03_Warehouse") return;
            Physics.SyncTransforms();
            var roots = scene.GetRootGameObjects();
            foreach (var spirit in roots.SelectMany(r => r.GetComponentsInChildren<SpiritController>(true)))
                if (spirit.GetComponent<WarehouseEjectShield>() == null) spirit.gameObject.AddComponent<WarehouseEjectShield>();
            var floors = roots.SelectMany(r => r.GetComponentsInChildren<BoxCollider>(true))
                .Where(c => c.name.EndsWith("_Floor") && c.name != "Hall_Floor").ToArray();
            foreach (var enemy in roots.SelectMany(r => r.GetComponentsInChildren<EnemyBase>(true)))
            {
                if (enemy.HasHomeBounds) continue;
                Vector3 at = enemy.transform.position;
                var floor = floors.FirstOrDefault(c => at.x >= c.bounds.min.x && at.x <= c.bounds.max.x
                    && at.z >= c.bounds.min.z && at.z <= c.bounds.max.z);
                var area = floor != null ? floor.bounds : new Bounds(at, new Vector3(8f, 10f, 7f));
                area.center = new Vector3(area.center.x, 3f, area.center.z);
                area.size = new Vector3(area.size.x, 10f, area.size.z);
                enemy.ConfigureHomeBounds(area);
            }
        }
    }
}
