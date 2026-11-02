using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(PossessionController))]
    public sealed class PossessionCharge : MonoBehaviour
    {
        private PossessionController possession;
        private Renderer mote;
        private Transform flightTarget;
        private Vector3 flightStart;
        private float spentAt = -10f;
        public bool HasCharge { get; private set; }
        public int Count => HasCharge ? 1 : 0;
        public int SuccessfulPossessions { get; private set; }
        private void Awake() => possession = GetComponent<PossessionController>();
        private void OnEnable() { EventBus.Possessed += Possessed; EventBus.Ejected += Ejected; EventBus.SpiritDied += Clear; }
        private void OnDisable() { EventBus.Possessed -= Possessed; EventBus.Ejected -= Ejected; EventBus.SpiritDied -= Clear; }
        private void Start()
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name = "Carried charge"; orb.layer = 2;
            orb.transform.SetParent(transform, false); orb.transform.localScale = Vector3.one * 0.26f;
            var collider = orb.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            mote = orb.GetComponent<Renderer>();
            mote.material = new Material(Shader.Find("Sprites/Default")); mote.material.color = new Color(0.55f, 1f, 0.85f);
            mote.enabled = false;
        }
        public bool TryCollect()
        {
            if (HasCharge || possession.CurrentState != PlayerState.Spirit || GetComponent<SpiritController>().CurrentHP <= 0
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            HasCharge = true; return true;
        }
        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (possession.PossessedEnemy != enemy) return;
            HasCharge = false; SuccessfulPossessions++;
            spentAt = Time.time; flightStart = transform.position + Vector3.up * 1.5f; flightTarget = enemy.transform;
        }
        private void Ejected(EnemyBase enemy, bool forced)
        { if (possession.CurrentState == PlayerState.Spirit) Clear(); }
        public void Clear() { HasCharge = false; flightTarget = null; }
        private void Update()
        {
            var host = possession.PossessedEnemy;
            if (host != null && (host.CurrentHP <= 0 || !host.gameObject.activeInHierarchy)
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress))
                possession.TryEject(false);
            if (mote == null) return;
            bool flying = flightTarget != null && Time.time - spentAt < 0.3f;
            mote.enabled = flying || HasCharge && possession.CurrentState == PlayerState.Spirit;
            if (flying) mote.transform.position = Vector3.Lerp(flightStart, flightTarget.position + Vector3.up * 1.3f, (Time.time - spentAt) / 0.3f);
            else mote.transform.localPosition = new Vector3(Mathf.Cos(Time.time * 2f) * 0.7f, 1.2f, Mathf.Sin(Time.time * 2f) * 0.7f);
        }
        private void OnDestroy() { if (mote != null) Destroy(mote.sharedMaterial); }
    }
}
