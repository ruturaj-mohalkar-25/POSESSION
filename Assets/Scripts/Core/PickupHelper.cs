using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // A one-time safety net per level: the first time the Spirit runs out of possession charges (or is low
    // on HP) with no ready pickup of that kind in plain sight, one pickup is dropped at a reachable spot
    // near it. Hard cap: at most one helper of each kind per level, so running out still matters.
    // Until it is taken, the helper is moved next to the Spirit if the Spirit wanders away from it.
    public sealed class PickupHelper : MonoBehaviour
    {
        [SerializeField, Min(0)] private int lowCharges = 0;
        [SerializeField, Min(1)] private int lowHealth = 2;
        [SerializeField, Min(0)] private int maxChargeHelpersPerLevel = 1;
        [SerializeField, Min(0)] private int maxHealthHelpersPerLevel = 1;
        [SerializeField, Min(0f)] private float nearbyRadius = 12f;   // a ready pickup this close and in plain sight counts
        [SerializeField, Min(0f)] private float minSpawnDistance = 3f;
        [SerializeField, Min(0f)] private float maxSpawnDistance = 6f;
        [SerializeField, Min(0f)] private float enemyClearance = 3f;
        [SerializeField, Min(0f)] private float retryDelay = 3f;

        const float Clearance = 0.6f;
        const float FloorNormal = 0.7f; // hits this flat are floor/ground and never block line of sight

        private SpiritController spirit;
        private PossessionController possession;
        private PossessionCharge charge;
        private PossessionChargePickup spawnedCharge;
        private HealthPickup spawnedHealth;
        private int chargeHelpersSpawned;
        private int healthHelpersSpawned;
        private float nextCheck;
        private bool warnedNoSpot;

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

            // A live helper may follow the Spirit; a new one only while the per-level cap allows it
            bool canChargeHelp = spawnedCharge != null || chargeHelpersSpawned < maxChargeHelpersPerLevel;
            if (canChargeHelp && charge != null && charge.Count <= lowCharges && !ReadyChargeInSight(at)
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
                    chargeHelpersSpawned++;
                }
                spawnedCharge.transform.position = coinSpot;
                nextCheck = Time.time + retryDelay;
            }

            bool canHealthHelp = spawnedHealth != null || healthHelpersSpawned < maxHealthHelpersPerLevel;
            if (canHealthHelp && spirit.CurrentHP <= lowHealth && !ReadyHealthInSight(at)
                && TryRandomSpot(at, out Vector3 healthSpot))
            {
                if (spawnedHealth == null)
                {
                    var go = new GameObject("Helper Health Pickup");
                    SceneManager.MoveGameObjectToScene(go, spirit.gameObject.scene);
                    spawnedHealth = go.AddComponent<HealthPickup>();
                    spawnedHealth.MakeOneUse();
                    healthHelpersSpawned++;
                }
                spawnedHealth.transform.position = healthSpot;
                nextCheck = Time.time + retryDelay;
            }
        }

        // Once taken, a helper pickup is removed (the cap keeps another from replacing it)
        private void CleanUpTaken()
        {
            if (spawnedCharge != null && spawnedCharge.IsConsumed) { Destroy(spawnedCharge.gameObject); spawnedCharge = null; }
            if (spawnedHealth != null && spawnedHealth.IsConsumed) { Destroy(spawnedHealth.gameObject); spawnedHealth = null; }
        }

        private bool ReadyChargeInSight(Vector3 at)
        {
            foreach (var p in FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                if (p.IsReady && InSight(at, p.transform)) return true;
            return false;
        }

        private bool ReadyHealthInSight(Vector3 at)
        {
            foreach (var p in FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                if (p.IsReady && InSight(at, p.transform)) return true;
            return false;
        }

        // Close enough and nothing solid (wall, door, crate) between the Spirit and the pickup.
        // Floor and ground hits (upward-facing surfaces) are ignored, as are the Spirit and the pickup itself.
        private bool InSight(Vector3 at, Transform pickup)
        {
            if (Vector3.Distance(at, pickup.position) > nearbyRadius) return false;
            Vector3 from = at + Vector3.up * 0.5f;
            Vector3 to = pickup.position + Vector3.up * 1f;
            foreach (var hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y > FloorNormal) continue;
                if (hit.collider.transform.IsChildOf(spirit.transform) || hit.collider.transform.IsChildOf(pickup)) continue;
                return false;
            }
            return true;
        }

        // Random direction and distance; the spot must be reachable floor and not right next to an enemy.
        // If the room is crowded, it falls back to closer spots, then to spots that ignore enemy clearance,
        // so a needed pickup is never silently skipped.
        private bool TryRandomSpot(Vector3 at, out Vector3 spot)
        {
            Physics.SyncTransforms();
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector3 dir = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Vector3.forward;
                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
                if (!TryFloorSpot(at, dir, distance, out spot)) continue;
                if (NearEnemy(spot, enemies)) continue;
                return true;
            }
            // Fallback: a ring of closer spots, first away from enemies, then anywhere reachable
            float[] rings = { 2.5f, 1.8f };
            for (int pass = 0; pass < 2; pass++)
                foreach (float distance in rings)
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 dir = Quaternion.AngleAxis(i * 30f, Vector3.up) * Vector3.forward;
                        if (!TryFloorSpot(at, dir, distance, out spot)) continue;
                        if (pass == 0 && NearEnemy(spot, enemies)) continue;
                        return true;
                    }
            if (!warnedNoSpot)
            {
                warnedNoSpot = true;
                Debug.LogWarning($"PickupHelper: no clear floor spot near the Spirit at {at}; will keep retrying.");
            }
            spot = Vector3.zero;
            return false;
        }

        // A floor spot in plain sight of the Spirit (no wall in between), clear of walls and props
        private bool TryFloorSpot(Vector3 start, Vector3 dir, float distance, out Vector3 spot)
        {
            spot = Vector3.zero;
            Transform ignore = spirit.transform;
            Vector3 eye = start + Vector3.up * 0.5f;
            if (Physics.Raycast(eye, dir, out RaycastHit block, distance + Clearance, ~0, QueryTriggerInteraction.Ignore)
                && !block.collider.transform.IsChildOf(ignore))
                return false;

            Vector3 above = start + dir * distance + Vector3.up * 2f;
            if (!Physics.Raycast(above, Vector3.down, out RaycastHit floor, 6f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (floor.normal.y < FloorNormal || floor.point.y > start.y) return false; // not floor, or on top of a prop

            Vector3 body = floor.point + Vector3.up * 1f;
            foreach (var hit in Physics.OverlapSphere(body, Clearance, ~0, QueryTriggerInteraction.Ignore))
                if (hit != floor.collider && !hit.transform.IsChildOf(ignore)) return false;

            spot = new Vector3(floor.point.x, floor.point.y + 0.1f, floor.point.z);
            return true;
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
