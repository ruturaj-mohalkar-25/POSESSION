using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(PossessionController))]
    public sealed class PossessionCharge : MonoBehaviour
    {
        [SerializeField] private int maxCharges = 3;
        [SerializeField] private int startingCharges = 1;

        private PossessionController possession;
        private Transform[] motes;
        private Renderer[] moteRenderers;
        private Transform flightTarget;
        private Vector3 flightStart;
        private float spentAt = -10f;

        public int Count { get; private set; }
        public bool HasCharge => Count > 0;
        public int MaxCharges => maxCharges;
        public int SuccessfulPossessions { get; private set; }

        private void Awake() => possession = GetComponent<PossessionController>();

        private void OnEnable()
        {
            EventBus.Possessed += Possessed;
            EventBus.Ejected += Ejected;
            EventBus.SpiritDied += Clear;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= Possessed;
            EventBus.Ejected -= Ejected;
            EventBus.SpiritDied -= Clear;
        }

        private void Start()
        {
            Count = startingCharges;
            motes = new Transform[maxCharges];
            moteRenderers = new Renderer[maxCharges];
            for (int i = 0; i < maxCharges; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Carried charge " + i;
                orb.layer = 2;
                orb.transform.SetParent(transform, false);
                orb.transform.localScale = Vector3.one * 0.26f;
                var col = orb.GetComponent<Collider>();
                col.enabled = false;
                Destroy(col);
                var r = orb.GetComponent<Renderer>();
                r.material = new Material(Shader.Find("Sprites/Default"));
                r.material.color = new Color(0.55f, 1f, 0.85f);
                r.enabled = false;
                motes[i] = orb.transform;
                moteRenderers[i] = r;
            }
        }

        public bool TryCollect()
        {
            if (Count >= maxCharges || possession.CurrentState != PlayerState.Spirit
                || GetComponent<SpiritController>().CurrentHP <= 0
                || GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress)
                return false;
            Count++;
            return true;
        }

        private void Possessed(EnemyBase enemy, PlayerState state)
        {
            if (possession.PossessedEnemy != enemy) return;
            Count = Mathf.Max(0, Count - 1);
            SuccessfulPossessions++;
            spentAt = Time.time;
            flightStart = transform.position + Vector3.up * 1.5f;
            flightTarget = enemy.transform;
        }

        private void Ejected(EnemyBase enemy, bool forced)
        {
            if (possession.CurrentState == PlayerState.Spirit) flightTarget = null;
        }

        public void Clear()
        {
            Count = 0;
            flightTarget = null;
        }

        private void Update()
        {
            var host = possession.PossessedEnemy;
            if (host != null && (host.CurrentHP <= 0 || !host.gameObject.activeInHierarchy)
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress))
                possession.TryEject(false);

            if (motes == null) return;
            bool flying = flightTarget != null && Time.time - spentAt < 0.3f;
            bool spiritVisible = possession.CurrentState == PlayerState.Spirit;

            for (int i = 0; i < maxCharges; i++)
            {
                if (moteRenderers[i] == null) continue;
                bool show = (flying && i == Count) || (!flying && spiritVisible && i < Count);
                moteRenderers[i].enabled = show;
                if (!show) continue;

                if (flying && i == Count)
                {
                    motes[i].position = Vector3.Lerp(flightStart,
                        flightTarget.position + Vector3.up * 1.3f,
                        (Time.time - spentAt) / 0.3f);
                }
                else
                {
                    float angle = Time.time * 2f + i * (2f * Mathf.PI / Count);
                    motes[i].localPosition = new Vector3(
                        Mathf.Cos(angle) * 0.7f, 1.2f, Mathf.Sin(angle) * 0.7f);
                }
            }
        }

        private void OnDestroy()
        {
            if (moteRenderers == null) return;
            foreach (var r in moteRenderers)
                if (r != null) Destroy(r.sharedMaterial);
        }
    }
}
