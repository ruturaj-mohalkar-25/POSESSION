using UnityEngine;
namespace Possession
{
    public sealed class Level03Circuit : MonoBehaviour
    {
        [SerializeField] private AlarmWing wing;
        [SerializeField] private Target junction;
        [SerializeField] private BreakableWall panel;
        [SerializeField] private Renderer[] wires;
        [SerializeField] private float warningSeconds = 3f;
        private float suspicion, witnessUntil;
        private Level03Director level;
        public AlarmWing Wing => wing;
        public bool Latched { get; private set; }
        public bool Disabled => panel.CurrentHP <= 0;
        public bool Connected => !Disabled && !junction.IsHit;
        public float Suspicion => suspicion;
        private void OnEnable() => EventBus.Ejected += Ejected;
        private void OnDisable() => EventBus.Ejected -= Ejected;
        private void Ejected(EnemyBase enemy, bool forced)
        {
            var link = enemy != null ? enemy.GetComponent<Level03AlarmLink>() : null;
            if (link != null && link.wing == wing) witnessUntil = Time.time + 3.5f;
        }
        private void Update()
        {
            if (level == null) level = GetComponent<Level03Director>();
            if (Disabled) { Latched = false; suspicion = 0f; }
            else if (!Connected) suspicion = 0f;
            else if (!Latched && level != null && GameManager.Instance.CurrentState == LevelState.InProgress)
            {
                EnemyBase occupied = level.Possession.PossessedEnemy;
                var link = occupied != null ? occupied.GetComponent<Level03AlarmLink>() : null;
                Transform suspect = link != null && link.wing == wing ? occupied.transform
                    : level.Possession.CurrentState == PlayerState.Spirit && Time.time < witnessUntil ? level.Spirit.transform : null;
                bool seen = false;
                if (suspect != null)
                    foreach (var watcher in level.Hosts)
                        if (CanObserve(watcher, suspect)) { seen = true; break; }
                suspicion = seen ? suspicion + Time.deltaTime : 0f;
                if (suspicion >= warningSeconds) Latched = true;
            }
            Color color = Disabled ? new Color(.18f,.28f,.30f)
                : Latched ? new Color(1f,.16f,.14f)
                : !Connected ? new Color(.18f,.28f,.30f)
                : suspicion > 0f ? Color.Lerp(new Color(.9f,.65f,.1f),Color.red,(Mathf.Sin(Time.time*9f)+1f)*.5f)
                : wing == AlarmWing.West ? new Color(.3f,.65f,1f) : new Color(.8f,.4f,1f);
            foreach (var wire in wires) if (wire != null) wire.material.color = color;
        }
        public bool CanObserve(EnemyBase watcher, Transform suspect)
        {
            if (!Connected || watcher == null || suspect == null || watcher.transform == suspect
                || !watcher.gameObject.activeInHierarchy || watcher.CurrentHP <= 0 || level != null && level.Possession.PossessedEnemy == watcher) return false;
            var link = watcher.GetComponent<Level03AlarmLink>();
            if (link == null || link.wing != wing) return false;
            // Alarm observation uses the same visible cone/range as Spirit detection.
            return !watcher.IsPossessed && watcher.CanSee(suspect);
        }
        public void RestoreAlarm(bool latched) { Latched = latched && !Disabled; suspicion = 0f; witnessUntil = 0f; }
    }
}
