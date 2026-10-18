using UnityEngine;

namespace Possession
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 25f, -13f);
        [SerializeField] private float smoothSpeed = 5f;

        private Transform spiritTransform;

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
