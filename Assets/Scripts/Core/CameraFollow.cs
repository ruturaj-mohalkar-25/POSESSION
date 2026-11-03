using UnityEngine;

namespace Possession
{
    public class CameraFollow : MonoBehaviour
    {
        // One shared view keeps cones, labels, body timers and wall fading readable in
        // every level without taking ownership of the Level_02/Level_03 scene files.
        public static readonly Vector3 SharedOffset = new Vector3(0f, 22f, -9f);
        public static readonly Vector3 SharedEuler = new Vector3(63f, 0f, 0f);
        public const float SharedFieldOfView = 42f;

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 22f, -9f);
        [SerializeField] private float smoothSpeed = 5f;

        private Transform spiritTransform;

        private void Awake() => ApplySharedView();

        public void ApplySharedView()
        {
            offset = SharedOffset;
            transform.rotation = Quaternion.Euler(SharedEuler);
            var view = GetComponent<Camera>();
            if (view != null) view.fieldOfView = SharedFieldOfView;
        }

        private void Start()
        {
            var spirit = FindFirstObjectByType<SpiritController>();
            if (spirit != null)
            {
                spiritTransform = spirit.transform;
                if (target == null) target = spiritTransform;
            }
        }

        private void OnEnable()
        {
            EventBus.Possessed += OnPossessed;
            EventBus.Ejected += OnEjected;
        }

        private void OnDisable()
        {
            EventBus.Possessed -= OnPossessed;
            EventBus.Ejected -= OnEjected;
        }

        private void OnPossessed(EnemyBase enemy, PlayerState state)
        {
            target = enemy.transform;
        }

        private void OnEjected(EnemyBase enemy, bool forced)
        {
            target = spiritTransform;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }
    }
}
