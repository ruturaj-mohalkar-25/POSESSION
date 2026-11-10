using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(PossessionController))]
    public sealed class PossessionCharge : MonoBehaviour
    {
        [SerializeField] private int maxCharges = 3;
        [SerializeField] private int startingCharges = 1;

        private static readonly Color MoteColor = new Color(0.55f, 1f, 0.85f);

        private PossessionController possession;
        private Transform[] motes;              // one tiny ghost per carried charge
        private Renderer[][] moteParts;
        private Material moteBody;
        private Material moteEyes;
        private Camera cam;
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
            moteBody = RuntimeMaterials.Lit(MoteColor, MoteColor * 0.7f);
            moteEyes = RuntimeMaterials.Lit(new Color(0.06f, 0.06f, 0.08f));

            motes = new Transform[maxCharges];
            moteParts = new Renderer[maxCharges][];
            for (int i = 0; i < maxCharges; i++)
            {
                motes[i] = BuildMiniGhost(i);
                moteParts[i] = motes[i].GetComponentsInChildren<Renderer>(true);
                SetVisible(i, false);
            }
        }

        // Tiny ghost (head, droplet tail, two eyes) the size of the old charge dot; eyes face -z
        private Transform BuildMiniGhost(int index)
        {
            var root = new GameObject("Carried charge " + index) { layer = 2 }.transform;
            root.SetParent(transform, false);
            root.localScale = Vector3.one * 0.55f;
            MotePart(root, new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.5f, 0.46f), moteBody);
            MotePart(root, new Vector3(0f, -0.2f, 0f), new Vector3(0.32f, 0.3f, 0.32f), moteBody);
            MotePart(root, new Vector3(-0.08f, 0.1f, -0.21f), new Vector3(0.08f, 0.11f, 0.04f), moteEyes);
            MotePart(root, new Vector3(0.08f, 0.1f, -0.21f), new Vector3(0.08f, 0.11f, 0.04f), moteEyes);
            return root;
        }

        private static void MotePart(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.layer = 2;
            var col = go.GetComponent<Collider>();
            col.enabled = false;
            Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void SetVisible(int index, bool visible)
        {
            foreach (var r in moteParts[index])
                if (r != null) r.enabled = visible;
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

            if (cam == null) cam = Camera.main;
            for (int i = 0; i < maxCharges; i++)
            {
                if (motes[i] == null) continue;
                bool show = (flying && i == Count) || (!flying && spiritVisible && i < Count);
                SetVisible(i, show);
                if (!show) continue;
                if (cam != null) motes[i].rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

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
            if (moteBody != null) Destroy(moteBody);
            if (moteEyes != null) Destroy(moteEyes);
        }
    }
}
