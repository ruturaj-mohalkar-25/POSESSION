using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Drops a pickup at a random reachable spot near the Spirit when it is running out:
    // a possession pickup at 0 charges, and a health pickup at low HP. Only when needed: if a ready pickup
    // of that kind is already in plain sight nearby, nothing is added. There is never more than one helper
    // of each kind; if the Spirit has moved away from it, it is moved next to the Spirit instead of a new one.
    public sealed class PickupHelper : MonoBehaviour
    {
        [SerializeField, Min(0)] private int lowCharges = 0;
        [SerializeField, Min(1)] private int lowHealth = 2;
        [SerializeField, Min(0f)] private float nearbyRadius = 12f;   // a ready pickup this close and in plain sight counts
        [SerializeField, Min(0f)] private float minSpawnDistance = 3f;
        [SerializeField, Min(0f)] private float maxSpawnDistance = 6f;
        [SerializeField, Min(0f)] private float enemyClearance = 3f;
        [SerializeField, Min(0f)] private float retryDelay = 3f;

        private SpiritController spirit;
        private PossessionController possession;
        private PossessionCharge charge;
        private PossessionChargePickup spawnedCharge;
        private HealthPickup spawnedHealth;
        private float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AddToScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AddToScene();

        private static void AddToScene()
        {
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            if (spirit == null || Object.FindFirstObjectByType<PickupHelper>() != null) return;
            var go = new GameObject("Pickup Helper");
            SceneManager.MoveGameObjectToScene(go, spirit.gameObject.scene);
            go.AddComponent<PickupHelper>().spirit = spirit;
        }

        private void Start()
        {
            if (spirit == null) spirit = FindFirstObjectByType<SpiritController>();
            if (spirit == null) { enabled = false; return; }
            possession = spirit.GetComponent<PossessionController>();
            charge = spirit.GetComponent<PossessionCharge>();
            nextCheck = Time.time + 1f; // let the level settle first
        }

        private void Update()
        {
            CleanUpTaken();
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.5f;

            if (spirit.CurrentHP <= 0 || possession != null && possession.CurrentState != PlayerState.Spirit
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress)
                return;

            Vector3 at = spirit.transform.position;

            if (charge != null && charge.Count <= lowCharges && !ReadyChargeInSight(at)
                && TryRandomSpot(at, out Vector3 coinSpot))
            {
                if (spawnedCharge == null)
                {
                    var go = new GameObject("Helper Soul Pickup");
                    SceneManager.MoveGameObjectToScene(go, spirit.gameObject.scene);
                    var trigger = go.AddComponent<SphereCollider>();
                    trigger.radius = 1.1f;
                    trigger.center = Vector3.up;
                    spawnedCharge = go.AddComponent<PossessionChargePickup>();
                    spawnedCharge.MakeOneUse();
                }
                spawnedCharge.transform.position = coinSpot; // new, or the existing helper following the Spirit
                nextCheck = Time.time + retryDelay;
            }

            if (spirit.CurrentHP <= lowHealth && !ReadyHealthInSight(at) && TryRandomSpot(at, out Vector3 healthSpot))
            {
                if (spawnedHealth == null)
                {
                    var go = new GameObject("Helper Health Pickup");
                    SceneManager.MoveGameObjectToScene(go, spirit.gameObject.scene);
                    spawnedHealth = go.AddComponent<HealthPickup>();
                    spawnedHealth.MakeOneUse();
                }
                spawnedHealth.transform.position = healthSpot;
                nextCheck = Time.time + retryDelay;
            }
        }

        // Once taken, a helper pickup is removed so the next one can appear when needed
        private void CleanUpTaken()
        {
            if (spawnedCharge != null && spawnedCharge.IsConsumed) { Destroy(spawnedCharge.gameObject); spawnedCharge = null; }
            if (spawnedHealth != null && spawnedHealth.IsConsumed) { Destroy(spawnedHealth.gameObject); spawnedHealth = null; }
        }

        private bool ReadyChargeInSight(Vector3 at)
        {
            foreach (var p in FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                if (p.IsReady && InSight(at, p.transform.position)) return true;
            return false;
        }

        private bool ReadyHealthInSight(Vector3 at)
        {
            foreach (var p in FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                if (p.IsReady && InSight(at, p.transform.position)) return true;
            return false;
        }

        // Close enough and nothing solid (wall, door, crate) between the Spirit and the pickup
        private bool InSight(Vector3 at, Vector3 pickup)
        {
            if (Vector3.Distance(at, pickup) > nearbyRadius) return false;
            Vector3 from = at + Vector3.up * 0.5f;
            Vector3 to = pickup + Vector3.up * 1f;
            foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(spirit.transform)) return false;
            return true;
        }

        // Random direction and distance; the spot must be reachable floor and not right next to an enemy
        private bool TryRandomSpot(Vector3 at, out Vector3 spot)
        {
            Physics.SyncTransforms();
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector3 dir = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Vector3.forward;
                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
                if (!StartRoomPickupPlacer.TryFloorSpot(spirit, at, dir, distance, out spot)) continue;
                if (NearEnemy(spot, enemies)) continue;
                return true;
            }
            spot = Vector3.zero;
            return false;
        }

        private bool NearEnemy(Vector3 spot, IReadOnlyList<EnemyBase> enemies)
        {
            foreach (var e in enemies)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                Vector3 d = e.transform.position - spot; d.y = 0f;
                if (d.magnitude < enemyClearance) return true;
            }
            return false;
        }
    }
}
