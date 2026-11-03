using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Possession
{
    public sealed class Level03Director : MonoBehaviour
    {
        public const float FloorHeight = 7.2f;
        public const float LayoutUnit = 2.2f;
        [SerializeField] private SpiritController spirit;
        [SerializeField] private Target mainTarget, relay, exitTarget, serviceTarget;
        [SerializeField] private Level03SecurityGate stairA, bridge, extractionWest, extractionSouth, maintenance, service;
        [SerializeField] private Level03Circuit west, east;
        private float nextContact;
        private BreakableWall serviceWall;
        private Level03Checkpoint[] shelters;
        private static Snapshot checkpoint;
        private static bool resume;
        public SpiritController Spirit => spirit;
        public PossessionController Possession => spirit.GetComponent<PossessionController>();
        public PossessionCharge Charge => spirit.GetComponent<PossessionCharge>();
        public EnemyBase[] Hosts { get; private set; }
        public Level03Circuit West => west;
        public Level03Circuit East => east;
        public bool HasCheckpoint => checkpoint != null;
        public string CheckpointName => checkpoint?.name;
        public Level03Checkpoint NearbyCheckpoint { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { checkpoint = null; resume = false; }
        private void Awake()
        {
            var roots=gameObject.scene.GetRootGameObjects();
            Hosts=roots.SelectMany(r=>r.GetComponentsInChildren<EnemyBase>()).ToArray();
            serviceWall=roots.SelectMany(r=>r.GetComponentsInChildren<BreakableWall>(true)).First(w=>w.name=="B1");
            shelters=roots.SelectMany(r=>r.GetComponentsInChildren<Level03Checkpoint>()).ToArray();
        }
        private IEnumerator Start()
        {
            nextContact = Time.time + 12f;
            var hud = FindFirstObjectByType<HUDManager>();
            if (hud != null && hud.transform.Find("ObjectiveBar") is Transform bar) bar.gameObject.SetActive(false);
            gameObject.AddComponent<Level03StatusHUD>().Initialize(this);
            // Route this scene's native end-screen buttons through its complete reset semantics.
            if (hud != null)
                foreach (var button in hud.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    string name=button.name.ToLowerInvariant();
                    if (!name.Contains("restart") && !name.Contains("menu")) continue;
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++) button.onClick.SetPersistentListenerState(i,UnityEngine.Events.UnityEventCallState.Off);
                    button.onClick.RemoveAllListeners();
                    if(name.Contains("restart"))button.onClick.AddListener(FullRestart);else button.onClick.AddListener(MainMenu);
                }
            yield return null;
            if (resume && checkpoint != null) RestoreCheckpoint();
            resume = false;
            ApplyGates();
        }
        private void Update()
        {
            if (GetComponent<Level03RouteMap>()?.IsOpen == true) return;
            NearbyCheckpoint = Possession.CurrentState == PlayerState.Spirit ? shelters.FirstOrDefault(c => Vector3.Distance(c.transform.position + Vector3.up*1.15f,spirit.transform.position)<2.2f) : null;
            if (Input.GetKeyDown(KeyCode.F) && NearbyCheckpoint != null) SaveCheckpoint(NearbyCheckpoint);
            if (Input.GetKeyDown(KeyCode.C) && HasCheckpoint) RetryCheckpoint();
            else if (Input.GetKeyDown(KeyCode.R)) FullRestart();
        }
        private void LateUpdate() => ApplyGates();
        public void ApplyGates()
        {
            stairA.Apply((mainTarget.IsHit || west.Disabled) && !west.Latched);
            bridge.Apply(west.Disabled && !west.Latched);
            bool extracted = (exitTarget.IsHit || east.Disabled) && !east.Latched;
            extractionWest.Apply(extracted); extractionSouth.Apply(extracted);
            maintenance.Apply(relay.IsHit); service.Apply(serviceTarget.IsHit);
        }
        public string NavigationHint
        {
            get
            {
                var player=Possession.PossessedEnemy!=null?Possession.PossessedEnemy.transform:spirit.transform;
                if(player.position.y>5.5f)
                    return east.Latched?"EAST LOCKDOWN · SE target, then PE panel"
                        : extractionWest.IsOpen?"EXIT OPEN · Head to green extraction"
                        : "Find RECORDS · Shoot T3 for extraction";
                return west.Latched ? "MAIN A LOCKED · Use gold SERVICE B"
                    : mainTarget.IsHit || west.Disabled ? "MAIN A OPEN · Take the blue stairs"
                    : serviceWall.CurrentHP<=0 ? "SERVICE B OPEN · Take the gold stairs"
                    : "CHOOSE A ROUTE · Blue MAIN A / Gold SERVICE B";
            }
        }
        public bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (other.GetComponentInParent<SpiritController>() == spirit) return Possession.CurrentState == PlayerState.Spirit;
            return Possession.PossessedEnemy != null && other.GetComponentInParent<EnemyBase>() == Possession.PossessedEnemy;
        }
        public bool TryContactDamage()
        {
            if (Time.timeScale <= 0f || Time.time < nextContact || spirit.CurrentHP <= 0 || Possession.CurrentState != PlayerState.Spirit) return false;
            nextContact = Time.time + 5f; spirit.TakeDamage(1); return true;
        }
        public bool SaveCheckpoint(Level03Checkpoint point)
        {
            if (point == null || Possession.CurrentState != PlayerState.Spirit || spirit.CurrentHP <= 0
                || GameManager.Instance.CurrentState != LevelState.InProgress || !point.source.IsReady
                || Vector3.Distance(point.transform.position+Vector3.up*1.15f,spirit.transform.position)>2.2f
                || Hosts.Any(h => h.gameObject.activeInHierarchy && Vector3.Distance(h.transform.position,spirit.transform.position)<3.5f)) return false;
            var saved = new Snapshot { name = point.label, spawn = point.transform.position+Vector3.up*1.15f, westAlarm = west.Latched, eastAlarm = east.Latched };
            foreach (var t in FindObjectsByType<Target>(FindObjectsSortMode.None)) saved.targets[t.name] = t.IsHit;
            foreach (var w in Walls()) saved.walls[w.name] = w.CurrentHP;
            foreach (var p in FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None)) saved.pickups[p.name] = p.CaptureState();
            foreach (var h in Hosts)
            {
                Vector3 pos = h.gameObject.activeInHierarchy ? h.transform.position : GameObject.Find("SpawnPoint_"+h.name).transform.position;
                saved.hosts[h.name] = new Pose(pos,h.transform.rotation);
            }
            checkpoint = saved; return true;
        }
        public void RetryCheckpoint() { resume = checkpoint != null; LevelFlow.Load("Level_03"); }
        public void FullRestart() { checkpoint = null; resume = false; LevelFlow.Load("Level_03"); }
        public void MainMenu() { checkpoint = null; resume = false; LevelFlow.Load("MainMenu"); }
        private void RestoreCheckpoint()
        {
            foreach (var t in FindObjectsByType<Target>(FindObjectsSortMode.None))
                if (checkpoint.targets.TryGetValue(t.name,out bool hit) && hit) t.OnHit();
            foreach (var w in Walls())
                if (checkpoint.walls.TryGetValue(w.name,out int hp) && hp<=0) w.TakeDamage(2);
            foreach (var p in FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                if (checkpoint.pickups.TryGetValue(p.name,out var state)) p.RestoreState(state);
            foreach (var h in Hosts)
                if (checkpoint.hosts.TryGetValue(h.name,out var pose))
                { var cc=h.GetComponent<CharacterController>();cc.enabled=false;h.transform.SetPositionAndRotation(pose.position,pose.rotation);cc.enabled=true; }
            west.RestoreAlarm(checkpoint.westAlarm); east.RestoreAlarm(checkpoint.eastAlarm);
            Vector3 delta = checkpoint.spawn - spirit.transform.position;
            spirit.RestoreAt(checkpoint.spawn); Charge.Clear();
            if (Camera.main != null) Camera.main.transform.position += delta;
            Physics.SyncTransforms();
        }
        private BreakableWall[] Walls() => gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BreakableWall>(true)).ToArray();
        private sealed class Snapshot
        {
            public string name;
            public Vector3 spawn;
            public bool westAlarm,eastAlarm;
            public readonly Dictionary<string,bool> targets = new();
            public readonly Dictionary<string,int> walls = new();
            public readonly Dictionary<string,PossessionChargePickup.State> pickups = new();
            public readonly Dictionary<string,Pose> hosts = new();
        }
    }
}
