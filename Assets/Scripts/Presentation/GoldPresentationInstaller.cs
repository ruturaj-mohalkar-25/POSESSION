using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public static class GoldPresentationInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }

        private static void Loaded(Scene scene, LoadSceneMode mode) => Install(scene);

        public static void Install(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            // The tutorial owns its scene and teaching sequence, but borrows Level 1's door and floor.
            if (CampaignLevels.NumberFor(scene.name) == 0)
            {
                if (scene.name != "Tutorial") return;
                GoldTutorialPresentation.Apply(roots);
                SkinDoors(roots);
                return;
            }
            if (roots.Any(r => r.GetComponent<GoldRoomPresentation>() != null)) return;
            var host = new GameObject("Gold_RoomPresentation");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<GoldRoomPresentation>().Apply(scene);
            foreach (var player in roots.SelectMany(r => r.GetComponentsInChildren<PossessionController>(true)))
                if (player.GetComponent<PlayerIdentityView>() == null)
                    player.gameObject.AddComponent<PlayerIdentityView>();
            SkinDoors(roots);
            if(!scene.GetRootGameObjects().Any(r=>r.GetComponent<GoldContactShadows>()!=null))
                host.AddComponent<GoldContactShadows>().Build(scene);
            GoldRoomDecor.Install(scene);
        }

        private static void SkinDoors(GameObject[] roots)
        {
            foreach (var door in roots.SelectMany(r => r.GetComponentsInChildren<SlidingDoorView>(true)))
                if (door.GetComponent<GoldDoorPresentation>() == null)
                    door.gameObject.AddComponent<GoldDoorPresentation>().Build();
        }
    }
}
