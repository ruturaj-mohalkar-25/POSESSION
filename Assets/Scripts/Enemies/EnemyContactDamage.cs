using UnityEngine;

namespace Possession
{
    // Hostile behaviour after a forced eject, using the body's own ability:
    // a Guard shoots visible bullets at the spirit, a Heavy winds up and smashes when close.
    // Bodies with neither ability fall back to a touch hit.
    // Kept separate from EnemyBase so the Guard/Heavy scripts stay untouched.
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyContactDamage : MonoBehaviour
    {
        private enum Attack { Touch, Shoot, Smash }

        [SerializeField] private int damage = 1;
        [SerializeField] private float firstAttackDelay = 0.75f; // grace period to run after ejecting

        [Header("Guard: shoot")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float shootRange = 4.5f; // also how far the bullet travels
        [SerializeField] private float shootInterval = 1.2f;

        [Header("Heavy: smash")]
        [SerializeField] private float smashRange = 3f;
        [SerializeField] private float smashWindup = 0.5f;
        [SerializeField] private float smashCooldown = 1.5f;
        [SerializeField] private Material dustMaterial;

        [Header("Touch")]
        [SerializeField] private float touchRange = 1.8f;
        [SerializeField] private float touchCooldown = 1f;

        [Header("Feedback")]
        [SerializeField] private Color warnColor = new Color(0.95f, 0.15f, 0.15f);
        [SerializeField] private float blinkTime = 0.15f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private EnemyBase enemy;
        private Collider ownCollider;
        private SpiritController spirit;
        private Collider spiritCollider;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private Attack attack;
        private bool hostile;
        private float nextAttackTime;
        private float smashAt;       // > 0 while winding up
        private float blinkUntil;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            ownCollider = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();

            if (GetComponent<ShootAbility>() != null && projectilePrefab != null) attack = Attack.Shoot;
            else if (GetComponent<SmashAbility>() != null) attack = Attack.Smash;
            else attack = Attack.Touch;
        }

        private void OnEnable()
        {
            EventBus.Ejected += OnEjected;
            EventBus.Possessed += OnPossessed;
        }

        private void OnDisable()
        {
            EventBus.Ejected -= OnEjected;
            EventBus.Possessed -= OnPossessed;
            SetTint(null);
        }

        private void OnEjected(EnemyBase ejected, bool forced)
        {
            if (ejected != enemy || !forced) return;
            hostile = true;
            smashAt = 0f;
            nextAttackTime = Time.time + firstAttackDelay;
        }

        private void OnPossessed(EnemyBase possessed, PlayerState state)
        {
            if (possessed != enemy) return;
            StopHostile();
        }

        private void Update()
        {
            if (blinkUntil > 0f && Time.time >= blinkUntil && smashAt <= 0f)
            {
                blinkUntil = 0f;
                SetTint(null);
            }

            if (!hostile) return;

            // EnemyBase makes the enemy possessable again once its hostile timer ends
            if (enemy.CanBePossessed || enemy.CurrentHP <= 0)
            {
                StopHostile();
                return;
            }

            if (!FindSpirit()) return;

            // Spirit is hidden while possessing another enemy
            if (spiritCollider != null && !spiritCollider.enabled) return;

            float distance = FlatDistanceToSpirit();

            if (smashAt > 0f)
            {
                if (Time.time >= smashAt) FinishSmash(distance);
                return;
            }

            if (Time.time < nextAttackTime) return;

            switch (attack)
            {
                case Attack.Shoot:
                    if (distance <= shootRange && CanSeeSpirit()) Shoot();
                    break;
                case Attack.Smash:
                    if (distance <= smashRange) StartSmash();
                    break;
                default:
                    if (distance <= touchRange)
                    {
                        spirit.TakeDamage(damage);
                        nextAttackTime = Time.time + touchCooldown;
                        Blink();
                    }
                    break;
            }
        }

        private void Shoot()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.5f;
            Vector3 dir = spirit.transform.position - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            var shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir.normalized));
            var projectile = shot.GetComponent<Projectile>();
            if (projectile != null && ownCollider != null) projectile.SetOwner(ownCollider);
            shot.AddComponent<EnemyShot>().Init(damage, shootRange);

            nextAttackTime = Time.time + shootInterval;
            Blink();
        }

        // Red while winding up so the player can step away before it lands
        private void StartSmash()
        {
            smashAt = Time.time + smashWindup;
            SetTint(warnColor);
        }

        private void FinishSmash(float distance)
        {
            smashAt = 0f;
            SetTint(null);
            nextAttackTime = Time.time + smashCooldown;

            Vector3 toSpirit = spirit.transform.position - transform.position;
            toSpirit.y = 0f;
            Vector3 impact = transform.position + toSpirit.normalized * 1.5f;
            impact.y = 0.3f;
            SimpleFX.Burst(impact, dustMaterial, 1f, 3.2f, 0.35f);

            if (distance <= smashRange + 0.3f) spirit.TakeDamage(damage);
        }

        private void StopHostile()
        {
            hostile = false;
            smashAt = 0f;
            blinkUntil = 0f;
            SetTint(null);
        }

        private bool FindSpirit()
        {
            if (spirit != null) return true;
            spirit = FindFirstObjectByType<SpiritController>();
            if (spirit == null) return false;
            spiritCollider = spirit.GetComponent<Collider>();
            return true;
        }

        private float FlatDistanceToSpirit()
        {
            Vector3 offset = spirit.transform.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private bool CanSeeSpirit()
        {
            Vector3 from = firePoint != null ? firePoint.position : transform.position;
            if (!Physics.Linecast(from, spirit.transform.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<SpiritController>() != null || hit.collider == ownCollider;
        }

        private void Blink()
        {
            blinkUntil = Time.time + blinkTime;
            SetTint(warnColor);
        }

        // Tints every renderer on the body; null restores the normal materials
        private void SetTint(Color? color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (color.HasValue)
                {
                    r.GetPropertyBlock(block);
                    block.SetColor(ColorId, color.Value);
                    r.SetPropertyBlock(block);
                }
                else
                {
                    r.SetPropertyBlock(null);
                }
            }
        }
    }
}
