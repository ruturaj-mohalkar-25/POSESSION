using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Installs the enemy readability cues at runtime so shared scenes, prefabs and AI
    // files stay untouched. Mirrors HeavySmashIndicatorInstaller.
    public static class EnemyReadabilityInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var enemy in root.GetComponentsInChildren<EnemyBase>(true))
                    if (enemy.GetComponent<EnemyReadability>() == null)
                        enemy.gameObject.AddComponent<EnemyReadability>();

                foreach (var heavy in root.GetComponentsInChildren<HeavyEnemy>(true))
                    if (heavy.GetComponent<HeavyFeel>() == null)
                        heavy.gameObject.AddComponent<HeavyFeel>();
            }

            if (Object.FindFirstObjectByType<EnemyReadabilityController>() == null)
            {
                var controller = new GameObject("EnemyReadabilityController");
                controller.AddComponent<EnemyReadabilityController>();
            }
        }
    }

    // Each frame: the nearest possessable enemy the Spirit can reach and see gets the
    // green possess cue; every chasing or hostile enemy gets the red cue; the rest are
    // cleared. The green pick mirrors PossessionController so the cue only appears where
    // a possess would actually succeed.
    public sealed class EnemyReadabilityController : MonoBehaviour
    {
        [SerializeField] private float possessRange = 7f;

        private EnemyReadability[] cues;
        private Transform spirit;
        private PossessionController possession;

        private void Start() => Rescan();

        private void Rescan()
        {
            cues = FindObjectsByType<EnemyReadability>(FindObjectsSortMode.None);
            possession = FindFirstObjectByType<PossessionController>();
            var spiritController = FindFirstObjectByType<SpiritController>();
            spirit = spiritController != null ? spiritController.transform : null;
        }

        private void LateUpdate()
        {
            if (cues == null || cues.Length == 0)
            {
                Rescan();
                return;
            }

            bool asSpirit = spirit != null && spirit.gameObject.activeInHierarchy
                && (possession == null || possession.CurrentState == PlayerState.Spirit);
            EnemyReadability green = asSpirit ? PickPossessTarget() : null;

            foreach (var cue in cues)
            {
                if (cue == null) continue;
                var enemy = cue.GetComponent<EnemyBase>();
                if (enemy == null)
                {
                    cue.SetMode(EnemyReadability.Mode.None);
                    continue;
                }

                if (cue == green)
                    cue.SetMode(EnemyReadability.Mode.Possessable);
                else if (enemy.IsChasing || enemy.IsHostile)
                    cue.SetMode(EnemyReadability.Mode.Hostile);
                else
                    cue.SetMode(EnemyReadability.Mode.None);
            }
        }

        private EnemyReadability PickPossessTarget()
        {
            EnemyReadability best = null;
            float bestDist = possessRange;
            foreach (var cue in cues)
            {
                if (cue == null) continue;
                var enemy = cue.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBePossessed) continue;

                float dist = Vector3.Distance(spirit.position, enemy.transform.position);
                if (dist > bestDist || !CanSee(enemy)) continue;
                bestDist = dist;
                best = cue;
            }
            return best;
        }

        // Same line-of-sight test the possession pick uses: blocked only by something
        // that is neither the target enemy nor part of the Spirit.
        private bool CanSee(EnemyBase enemy)
        {
            if (!Physics.Linecast(spirit.position, enemy.transform.position, out RaycastHit hit,
                    ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<EnemyBase>() == enemy
                || hit.collider.transform.IsChildOf(spirit);
        }
    }
}
