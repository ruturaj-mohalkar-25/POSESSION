using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession
{
    // Possession charges: each possession spends one, and the Spirit gains more from PossessionChargePickup.
    // Defaults keep Level_02's rules (start empty, carry one). Any other level that contains a pickup
    // gets this component at runtime with the shared rules: start with one charge, charges stack.
    [RequireComponent(typeof(PossessionController))]
    public sealed class PossessionCharge : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startCharges;
        [SerializeField, Min(0)] private int maxCharges = 1; // 0 = no cap
        private PossessionController possession;
        private Renderer mote;
        private Transform flightTarget;
        private Vector3 flightStart;
        private float spentAt = -10f;
        private int count;
        public bool HasCharge => count > 0;
        public int Count => count;
        public int MaxCharges => maxCharges;
        public int SuccessfulPossessions { get; private set; }
        private void Awake() { possession = GetComponent<PossessionController>(); count = startCharges; }
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
            Announce(); // lets the HUD show the starting count
        }
        // Used when the component is added at runtime; also resets the current count
        public void Configure(int start, int max)
        {
            startCharges = Mathf.Max(0, start); maxCharges = Mathf.Max(0, max); count = startCharges;
        }
        public bool TryCollect()
        {
            if (maxCharges > 0 && count >= maxCharges || possession.CurrentState != PlayerState.Spirit || GetComponent<SpiritController>().CurrentHP <= 0
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return false;
            count++; EventBus.ChargeCollected?.Invoke(); Announce(); return true;
        }
        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (possession.PossessedEnemy != enemy) return;
            count = Mathf.Max(0, count - 1); SuccessfulPossessions++;
            EventBus.ChargeSpent?.Invoke(); Announce();
            spentAt = Time.time; flightStart = transform.position + Vector3.up * 1.5f; flightTarget = enemy.transform;
        }
        // Leaving a body keeps the charges still carried; only the flight effect is reset
        private void Ejected(EnemyBase enemy, bool forced)
        { if (possession.CurrentState == PlayerState.Spirit) flightTarget = null; }
        public void Clear() { count = 0; flightTarget = null; Announce(); }
        private void Announce() => EventBus.ChargesChanged?.Invoke(count, maxCharges);
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

        // ---- Shared rules for every other level ----

        public const int SharedStartCharges = 1;
        public const int SharedMaxCharges = 0; // charges stack
        // Levels that always allow unlimited possessions, even if collectibles are placed in them
        private static readonly string[] UnlimitedLevels = { "Level_01" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install(SceneManager.GetActiveScene());
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        // A level opts in by containing at least one pickup, so levels without collectibles keep free possession
        private static void Install(Scene scene)
        {
            if (System.Array.IndexOf(UnlimitedLevels, scene.name) >= 0) return;
            if (FindFirstObjectByType<PossessionChargePickup>() == null) return;
            var possession = FindFirstObjectByType<PossessionController>();
            if (possession == null || possession.GetComponent<PossessionCharge>() != null) return;
            possession.gameObject.AddComponent<PossessionCharge>().Configure(SharedStartCharges, SharedMaxCharges);
        }
    }
}
