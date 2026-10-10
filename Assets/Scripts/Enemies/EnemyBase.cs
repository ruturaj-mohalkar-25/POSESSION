using UnityEngine;

namespace Possession
{
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Base")]
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected int maxHP = 3;
        [SerializeField] protected Transform[] waypoints;

        protected int currentHP;
        protected bool isPossessed;
        protected bool isHostile;
        protected float hostileTimer;

        public bool CanBePossessed => !isPossessed && !isHostile && currentHP > 0;
        public int CurrentHP => currentHP;

        protected virtual void Awake()
        {
            currentHP = maxHP;
        }

        protected virtual void Update()
        {
            if (isPossessed)
            {
                HandlePossessedMovement();
                return;
            }

            if (isHostile)
            {
                hostileTimer -= Time.deltaTime;
                if (hostileTimer <= 0f)
                    isHostile = false;

                HandleHostileBehavior();
                return;
            }

            HandlePatrol();
        }

        protected virtual void HandlePatrol()
        {
            // Override in subclass: move between waypoints
        }

        protected virtual void HandlePossessedMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;

            if (dir.magnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(dir);

            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

            if (Input.GetMouseButtonDown(0))
            {
                var ability = GetComponent<Ability>();
                if (ability != null)
                    ability.TryExecute();
            }
        }

        protected virtual void HandleHostileBehavior()
        {
            // Override in subclass: chase or attack spirit
        }

        public virtual void OnPossessed()
        {
            isPossessed = true;
        }

        public virtual void OnEjected()
        {
            isPossessed = false;
        }

        public virtual void OnForcedEject(float hostileDuration)
        {
            isPossessed = false;
            isHostile = true;
            hostileTimer = hostileDuration;
        }

        public virtual void TakeDamage(int amount)
        {
            currentHP -= amount;
            if (currentHP <= 0)
                Die();
        }

        protected virtual void Die()
        {
            gameObject.SetActive(false);
        }
    }
}
