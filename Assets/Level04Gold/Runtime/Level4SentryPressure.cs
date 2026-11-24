using UnityEngine;

namespace Possession.Level04Gold
{
    [RequireComponent(typeof(GuardEnemy))]
    public sealed class Level4SentryPressure : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Renderer warningLamp;
        [SerializeField, Min(.1f)] private float warningSeconds = .9f;
        [SerializeField, Min(.1f)] private float shotCooldown = 2.4f;
        [SerializeField, Min(1f)] private float shotRange = 8f;
        private GuardEnemy enemy;
        private PossessionController player;
        private EnemyBase trackedHost;
        private float fireAt, nextShot;
        private MaterialPropertyBlock lampBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool IsWindingUp { get; private set; }
        public bool TargetInSight { get; private set; }

        private void Awake()
        {
            enemy = GetComponent<GuardEnemy>();
            lampBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (player == null) player = FindFirstObjectByType<PossessionController>();
            var host = player != null ? player.PossessedEnemy : null;
            bool playing = Time.timeScale > 0f
                && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress);
            TargetInSight = playing && enemy != null && enemy.isActiveAndEnabled && enemy.CurrentHP > 0
                && !enemy.IsPossessed && host != null && host != enemy && host.IsPossessed
                && host.CurrentHP > 0 && host.gameObject.activeInHierarchy
                && Mathf.Abs(host.transform.position.y - transform.position.y) <= 2f
                && (host.transform.position - transform.position).sqrMagnitude <= shotRange * shotRange
                && enemy.CanSee(host.transform) && ClearMuzzleSight(host);
            if (!TargetInSight)
            {
                trackedHost = null;
                CancelWarning();
                return;
            }
            if (trackedHost != host)
            {
                trackedHost = host;
                CancelWarning();
            }
            if (Time.time < nextShot) return;
            if (!IsWindingUp)
            {
                IsWindingUp = true;
                fireAt = Time.time + warningSeconds;
                SetWarning(true);
            }
            if (Time.time < fireAt) return;
            Fire(host);
            nextShot = Time.time + shotCooldown;
            CancelWarning();
        }

        private bool ClearMuzzleSight(EnemyBase host)
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * .5f;
            Vector3 delta = host.transform.position - origin;
            foreach (var hit in Physics.RaycastAll(origin, delta, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(host.transform)) return false;
            return true;
        }

        private void Fire(EnemyBase host)
        {
            if (projectilePrefab == null || projectilePrefab.GetComponent<Projectile>() == null) return;
            Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * .5f;
            Vector3 direction = host.transform.position - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < .01f) return;
            var shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction.normalized));
            foreach (var collider in shot.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var body = shot.GetComponent<Rigidbody>();
            if (body != null) { body.useGravity = false; body.isKinematic = true; }
            shot.GetComponent<Projectile>().SetOwner(enemy.GetComponent<CharacterController>());
            var hostile = shot.GetComponent<EnemyShot>();
            if (hostile == null) hostile = shot.AddComponent<EnemyShot>();
            hostile.Init(1, shotRange);
        }

        private void CancelWarning()
        {
            IsWindingUp = false;
            fireAt = 0f;
            SetWarning(false);
        }

        private void SetWarning(bool warning)
        {
            if (warningLamp == null) return;
            if (!warning) { warningLamp.SetPropertyBlock(null); return; }
            var color = new Color(1f, .15f, .1f);
            warningLamp.GetPropertyBlock(lampBlock);
            lampBlock.SetColor(ColorId, color);
            lampBlock.SetColor(BaseColorId, color);
            warningLamp.SetPropertyBlock(lampBlock);
        }

        private void OnDisable()
        {
            TargetInSight = false;
            trackedHost = null;
            CancelWarning();
        }
    }
}
