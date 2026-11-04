using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    public sealed class Level02Director : MonoBehaviour
    {
        [SerializeField] private Door northDoor, southDoor;
        [SerializeField] private BreakableWall northWall, southWall;
        [SerializeField] private SpiritController spirit;
        [SerializeField] private Vector3 courtyard;
        private static bool resumeCourtyard;
        private float nextContactHit;
        public bool CheckpointReached { get; private set; }
        public SpiritController Spirit => spirit;
        public PossessionController Possession => spirit.GetComponent<PossessionController>();
        public PossessionCharge Charge => spirit.GetComponent<PossessionCharge>();
        public bool NorthReady => northDoor.IsOpen && northWall.CurrentHP <= 0;
        public bool SouthReady => southDoor.IsOpen && southWall.CurrentHP <= 0;
        public bool CanExit => NorthReady || SouthReady;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => resumeCourtyard = false;
        private void Start()
        {
            HUDManager hud = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root.GetComponentInChildren<HUDManager>(true) is HUDManager found) { hud = found; break; }
            if (hud != null && hud.transform.Find("ObjectiveBar") is Transform objectives)
                objectives.gameObject.SetActive(false);
            if (resumeCourtyard)
            {
                Vector3 delta = courtyard - spirit.transform.position;
                spirit.RestoreAt(courtyard); CheckpointReached = true;
                if (Camera.main != null) Camera.main.transform.position += delta;
            }
            resumeCourtyard = false;
            gameObject.AddComponent<Level02ChargeHUD>().Initialize(this);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C) && CheckpointReached) RetryCourtyard();
            else if (Input.GetKeyDown(KeyCode.R)) FullRestart();
        }
        public bool TryContactDamage()
        {
            if (Time.time < nextContactHit || spirit.CurrentHP <= 0 || Possession.CurrentState != PlayerState.Spirit) return false;
            nextContactHit = Time.time + 3f;
            spirit.TakeDamage(1);
            return true;
        }
        public void MarkCheckpoint() => CheckpointReached = true;
        public void RetryCourtyard() { resumeCourtyard = CheckpointReached; LevelFlow.Load("Level_02"); }
        public void FullRestart() { resumeCourtyard = false; LevelFlow.Load("Level_02"); }
        public void MainMenu() { resumeCourtyard = false; LevelFlow.Load("MainMenu"); }
        public bool IsPlayer(Collider other)
        {
            if (other.GetComponentInParent<SpiritController>() == spirit) return Possession.CurrentState == PlayerState.Spirit;
            var enemy = other.GetComponentInParent<EnemyBase>();
            return enemy != null && Possession.PossessedEnemy == enemy;
        }
    }
}
